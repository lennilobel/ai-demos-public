using MongoDB.Bson;
using MongoDB.Driver;
using OpenAI.Embeddings;
using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataVectorizer : DataVectorizerBase
	{
		public MongoDbDataVectorizer(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task VectorizeEntities(int[] ids)
		{
			Debugger.Break();

			const int BatchSize = 100;

			var itemCount = 0;
			var database = Shared.MongoClient.GetDatabase(base.RagProvider.DatabaseName);
			var collection = database.GetCollection<BsonDocument>(base.RagProvider.MongoDbConfig.CollectionName);

			var counter = 0;

			while (true)
			{
				var batchStarted = DateTime.Now;

				var documents = (await collection
					.Find(Builders<BsonDocument>.Filter.Empty)
					.Sort(Builders<BsonDocument>.Sort.Ascending(base.RagProvider.EntityTitleFieldName))
					.Skip(itemCount)
					.Limit(BatchSize)
					.ToListAsync())
						.ToArray();

				ConsoleHelper.WriteLine(documents.Length.ToString(), ConsoleHelper.SystemColor);
				itemCount += documents.Length;

				if (documents.Length > 0)
				{
					foreach (var document in documents)
					{
						var title = document.GetValue(base.RagProvider.EntityTitleFieldName).AsString;
						var id = document.GetValue("_id").ToString();
						ConsoleHelper.WriteLine($"{++counter,5}: Vectorizing entity - {title} (ID {id})", ConsoleHelper.InfoDimColor);
					}

					// Generate text embeddings (vectors) for the batch of documents
					var embeddings = await this.GenerateEmbeddings(documents);

					// Update the documents back to the container with generated text embeddings (vectors)
					await this.SaveVectors(collection, documents, embeddings);

					var batchElapsed = DateTime.Now.Subtract(batchStarted);

					ConsoleHelper.WriteLine($"Processed documents {itemCount - documents.Length + 1} - {itemCount} in {batchElapsed}", ConsoleHelper.InfoColor);
				}

				if (documents.Length < BatchSize)
				{
					break;
				}
			}

			ConsoleHelper.WriteLine($"Generated and embedded vectors for {itemCount} document(s)", ConsoleHelper.UserColor);
		}

		private async Task<OpenAIEmbedding[]> GenerateEmbeddings(BsonDocument[] documents)
		{
			ConsoleHelper.Write("Generating embeddings... ", ConsoleHelper.SystemColor);

			// Strip any previous vector from each document
			foreach (var document in documents)
			{
				document.Remove("vector");
			}

			// Generate embeddings based on the JSON string content of each document
			var input = documents.Select(d => d.ToString()).ToArray();
			var embeddingClient = Shared.AzureOpenAIClient.GetEmbeddingClient(EmbeddingModelFactory.GetDeploymentName());
			var embeddings = (await embeddingClient.GenerateEmbeddingsAsync(input)).Value.ToArray();

			ConsoleHelper.WriteLine(embeddings.Length, ConsoleHelper.SystemColor);

			return embeddings;
		}

		private async Task SaveVectors(IMongoCollection<BsonDocument> collection, BsonDocument[] documents, OpenAIEmbedding[] embeddings)
		{
			ConsoleHelper.Write("Saving vectors... ", ConsoleHelper.SystemColor);

			var bulkOperations = new List<WriteModel<BsonDocument>>();

			// Set the vector property of each document from the generated embeddings
			for (var i = 0; i < documents.Length; i++)
			{
				var document = documents[i];
				var vector = new BsonArray(embeddings[i].ToFloats().ToArray());
				document["vector"] = vector;

				var idFilter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
				var replaceOne = new ReplaceOneModel<BsonDocument>(idFilter, document);
				bulkOperations.Add(replaceOne);
			}

			// Use bulk write to update the documents back to the container
			await collection.BulkWriteAsync(bulkOperations);

			ConsoleHelper.WriteLine(documents.Length, ConsoleHelper.SystemColor);
		}

	}
}
