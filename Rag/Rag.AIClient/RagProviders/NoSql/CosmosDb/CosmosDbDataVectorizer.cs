using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Rag.AIClient.EmbeddingModels;
using Rag.AIClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbDataVectorizer : DataVectorizerBase
	{
		public static int _errorCount;
		public static double _ruCost;

		protected override async Task VectorizeEntities(int[] ids)
		{
			Debugger.Break();

			_errorCount = 0;
			_ruCost = 0;
			
            var itemCount = 0;
            var database = Shared.CosmosClient.GetDatabase(RagProviderFactory.GetDatabaseName());
            var container = database.GetContainer(RagProviderFactory.GetCosmosDbConfig().ContainerName);

            // Raise the throughput on the container
            await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000));

            // Query documents in the container (process results in batches)
            var sql = $"SELECT * FROM c{(ids == null ? null : $" WHERE c.id = IN({string.Join(',', ids)})")}";
            var iterator = container.GetItemQueryIterator<JObject>(
                queryText: sql,
                requestOptions: new QueryRequestOptions { MaxItemCount = 100 });

            while (iterator.HasMoreResults)
            {
                var batchStarted = DateTime.Now;

                // Retrieve the next batch of documents
                ConsoleOutput.Write("Retrieving documents... ", ConsoleColor.Green);
                var documents = (await iterator.ReadNextAsync()).ToArray();
                ConsoleOutput.WriteLine(documents.Length.ToString(), ConsoleColor.Green);
                itemCount += documents.Length;

                // Generate text embeddings (vectors) for the batch of documents
                var embeddings = await this.GenerateEmbeddings(documents);

                // Update the documents back to the container with generated text embeddings (vectors)
                await this.SaveVectors(container, documents, embeddings);

                var batchElapsed = DateTime.Now.Subtract(batchStarted);

                ConsoleOutput.WriteLine($"Processed documents {itemCount - documents.Length + 1} - {itemCount} in {batchElapsed}", ConsoleColor.Cyan);
            }

            // Lower the throughput on the container
            await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));

            ConsoleOutput.WriteLine($"Generated and embedded vectors for {itemCount} document(s) with {_errorCount} error(s) ({_ruCost} RUs)", ConsoleColor.Yellow);
        }

        private async Task<IReadOnlyList<EmbeddingItem>> GenerateEmbeddings(JObject[] documents)
        {
            ConsoleOutput.Write("Generating embeddings... ", ConsoleColor.Green);

            // Strip meaningless properties and any previous vectors from each document
            foreach (var document in documents)
            {
                document.Remove("_rid");
                document.Remove("_self");
                document.Remove("_etag");
                document.Remove("_attachments");
                document.Remove("_ts");
                document.Remove("ttl");
                document.Remove("vectors");
            }

            // Generate embeddings based on the JSON string content of each document
            var embeddingsOptions = new EmbeddingsOptions(
                deploymentName: EmbeddingModelFactory.GetDeploymentName(),
                input: documents.Select(d => d.ToString())
            );

            var openAIEmbeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
            var embeddings = openAIEmbeddings.Value.Data;

            ConsoleOutput.WriteLine(embeddings.Count, ConsoleColor.Green);

            return embeddings;
        }

        private async Task SaveVectors(Container container, JObject[] documents, IReadOnlyList<EmbeddingItem> embeddings)
        {
            ConsoleOutput.Write("Saving vectors... ", ConsoleColor.Green);

            // Set the vectors property of each document from the generated embeddings
            for (var i = 0; i < documents.Length; i++)
            {
                var embeddingsArray = embeddings[i].Embedding.ToArray();
                var vectors = JArray.FromObject(embeddingsArray);
                documents[i]["vectors"] = vectors;
            }

            // Use bulk execution to update the documents back to the container
            var tasks = new List<Task>(documents.Length);
            foreach (JObject document in documents)
            {
                var task = container.ReplaceItemAsync(document, document["id"].ToString(), new PartitionKey(RagProviderFactory.GetCosmosDbConfig().PartitionKeyValue));
                tasks.Add(task
                    .ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            _ruCost += t.Result.RequestCharge;
                        }
                        else
                        {
                            ConsoleOutput.WriteErrorLine($"Error replacing document id='{document["id"]}'\n{t.Exception.Message}");
                            _errorCount++;
                        }
                    }));
            }

            await Task.WhenAll(tasks);

            ConsoleOutput.WriteLine(documents.Length, ConsoleColor.Green);
        }

    }
}