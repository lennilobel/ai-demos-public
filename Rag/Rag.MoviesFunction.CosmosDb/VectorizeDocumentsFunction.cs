using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using System.Linq;
using OpenAI.Embeddings;

namespace FunctionApp1
{
	public class VectorizeDocumentsFunction
	{
		private const string DatabaseName = "rag-demo-3l";
		private const string ContainerName = "movies";

		private static readonly List<string> _processedIds = [];
		private static readonly object _threadLock = new();

		private readonly ILogger _logger;

		private static CosmosClient CosmosClient { get; }

		private static AzureOpenAIClient OpenAIClient { get; }

		static VectorizeDocumentsFunction()
		{
			var cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
			CosmosClient = new CosmosClient(cosmosDbConnectionString, new CosmosClientOptions { AllowBulkExecution = true });

			var openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
			var openAIApiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
			OpenAIClient = new AzureOpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIApiKey));
		}

		public VectorizeDocumentsFunction(ILoggerFactory loggerFactory)
		{
			this._logger = loggerFactory.CreateLogger<VectorizeDocumentsFunction>();
		}

		[Function("EmbedVectors")]
		public async Task EmbedVectors(
			[CosmosDBTrigger(
				databaseName: DatabaseName,
				containerName: ContainerName,
				Connection = "CosmosDbConnectionString",
				LeaseContainerName = "lease",
				CreateLeaseContainerIfNotExists = true
			)]
			IReadOnlyList<JsonElement> documentElements)
		{
			if (documentElements == null || documentElements.Count == 0)
			{
				return;
			}

			try
			{
				await this.ProcessChanges(documentElements);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "An error occurred processing changed documents");
			}
		}

		private async Task ProcessChanges(IReadOnlyList<JsonElement> documentElements)
		{
			var documents = this.GetChangedDocuments(documentElements);

			if (documents.Length == 0)
			{
				return;
			}

			this._logger.LogInformation($"Change detected in {documents.Length} document(s)");

			var embeddings = await this.GenerateEmbeddings(documents);

			await this.UpdateDocuments(documents, embeddings);
		}

		private JObject[] GetChangedDocuments(IReadOnlyList<JsonElement> documentElements)
		{
			var changedDocuments = new List<JObject>();
			foreach (var documentElement in documentElements)
			{
				var document = JsonConvert.DeserializeObject<JObject>(documentElement.GetRawText());
				var id = document["id"].ToString();

				lock (_threadLock)
				{
					if (!_processedIds.Contains(id))
					{
						_logger.LogInformation($"Change detected in document ID {id} ({document["title"]})");
						_processedIds.Add(id);
						changedDocuments.Add(document);
					}
					else
					{
						_processedIds.Remove(id);
					}
				}
			}

			return changedDocuments.ToArray();
		}

		private async Task<OpenAIEmbedding[]> GenerateEmbeddings(JObject[] documents)
		{
			this._logger.LogInformation($"Generating vector embeddings for {documents.Length} document(s)");

			// Strip meaningless properties and any previous vector from each document
			foreach (var document in documents)
			{
				document.Remove("_rid");
				document.Remove("_self");
				document.Remove("_etag");
				document.Remove("_attachments");
				document.Remove("_lsn");
				document.Remove("_ts");
				document.Remove("ttl");
				document.Remove("vector");
			}

			var input = documents.Select(d => d.ToString()).ToArray();
			var embeddingClient = OpenAIClient.GetEmbeddingClient(Environment.GetEnvironmentVariable("OpenAIModelDeploymentName"));
			var embeddings = (await embeddingClient.GenerateEmbeddingsAsync(input)).Value.ToArray();

			this._logger.LogInformation($"Generated vector embeddings for {documents.Length} document(s)");

			return embeddings;
		}

		private async Task UpdateDocuments(JObject[] documents, OpenAIEmbedding[] embeddings)
		{
			this._logger.LogInformation($"Updating {documents.Length} document(s)");

			var container = CosmosClient.GetDatabase(DatabaseName).GetContainer(ContainerName);

			for (var i = 0; i < documents.Length; i++)
			{
				var vector = JArray.FromObject(embeddings[i].ToFloats().ToArray());
				documents[i]["vector"] = vector;
			}

			var tasks = new List<Task>(documents.Length);
			foreach (JObject document in documents)
			{
				var task = container.ReplaceItemAsync(document, document["id"].ToString(), new PartitionKey("movie"));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status != TaskStatus.RanToCompletion)
						{
							this._logger.LogError($"Error replacing document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}");
						}
					}));
			}

			await Task.WhenAll(tasks);

			this._logger.LogInformation($"Updated {documents.Length} document(s)");
		}

	}
}
