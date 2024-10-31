using Azure.AI.OpenAI;
using MongoDB.Bson;
using MongoDB.Driver;
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

		protected override async Task VectorizeEntities(int[] movieIds)
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

				ConsoleOutput.WriteLine(documents.Length.ToString(), ConsoleColor.Green);
				itemCount += documents.Length;

				if (documents.Length > 0)
				{
					foreach (var document in documents)
					{
						var title = document.GetValue(base.RagProvider.EntityTitleFieldName).AsString;
						var id = document.GetValue("_id").ToString();
						ConsoleOutput.WriteLine($"{++counter,5}: Vectorizing entity - {title} (ID {id})", ConsoleColor.DarkCyan);
					}

					// Generate text embeddings (vectors) for the batch of documents
					var embeddings = await this.GenerateEmbeddings(documents);

					// Update the documents back to the container with generated text embeddings (vectors)
					await this.SaveVectors(collection, documents, embeddings);

					var batchElapsed = DateTime.Now.Subtract(batchStarted);

					ConsoleOutput.WriteLine($"Processed documents {itemCount - documents.Length + 1} - {itemCount} in {batchElapsed}", ConsoleColor.Cyan);
				}

				if (documents.Length < BatchSize)
				{
					break;
				}
			}

			ConsoleOutput.WriteLine($"Generated and embedded vectors for {itemCount} document(s)", ConsoleColor.Yellow);
		}

		private async Task<IReadOnlyList<EmbeddingItem>> GenerateEmbeddings(BsonDocument[] documents)
		{
			ConsoleOutput.Write("Generating embeddings... ", ConsoleColor.Green);

			// Strip any previous vector from each document
			foreach (var document in documents)
			{
				document.Remove("vector");
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

		private async Task SaveVectors(IMongoCollection<BsonDocument> collection, BsonDocument[] documents, IReadOnlyList<EmbeddingItem> embeddings)
		{
			ConsoleOutput.Write("Saving vectors... ", ConsoleColor.Green);

			var bulkOperations = new List<WriteModel<BsonDocument>>();

			// Set the vector property of each document from the generated embeddings
			for (var i = 0; i < documents.Length; i++)
			{
				var document = documents[i];
				var embeddingsArray = embeddings[i].Embedding.ToArray();
				var vector = new BsonArray(embeddingsArray);
				document["vector"] = vector;

				var idFilter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
				var replaceOne = new ReplaceOneModel<BsonDocument>(idFilter, document);
				bulkOperations.Add(replaceOne);
			}

			// Use bulk write to update the documents back to the container
			await collection.BulkWriteAsync(bulkOperations);

			ConsoleOutput.WriteLine(documents.Length, ConsoleColor.Green);
		}

	}
}
