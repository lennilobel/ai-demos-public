using Azure.AI.OpenAI;
using MongoDB.Bson;
using MongoDB.Driver;
using Rag.MoviesClient.EmbeddingModels;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataVectorizer : DataVectorizerBase
	{
		protected override async Task VectorizeMovies(int[] movieIds)
		{
			Debugger.Break();

			const int BatchSize = 100;

			var itemCount = 0;
			var database = Shared.MongoClient.GetDatabase(RagProviderFactory.GetDatabaseName());
			var collection = database.GetCollection<BsonDocument>(Shared.AppConfig.MongoDb.CollectionName);

			while (true)
			{
				var batchStarted = DateTime.Now;

				// Retrieve the next batch of documents
				ConsoleOutput.Write("Retrieving documents... ", ConsoleColor.Green);

				var documents = (await collection
					.Find(Builders<BsonDocument>.Filter.Empty)
					.Skip(itemCount)
					.Limit(BatchSize)
					.ToListAsync())
						.ToArray();

				ConsoleOutput.WriteLine(documents.Length.ToString(), ConsoleColor.Green);
				itemCount += documents.Length;

				if (documents.Length > 0)
				{
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

			// Strip any previous vectors from each document
			foreach (var document in documents)
			{
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

		private async Task SaveVectors(IMongoCollection<BsonDocument> collection, BsonDocument[] documents, IReadOnlyList<EmbeddingItem> embeddings)
		{
			ConsoleOutput.Write("Saving vectors... ", ConsoleColor.Green);

			var bulkOperations = new List<WriteModel<BsonDocument>>();

			// Set the vectors property of each document from the generated embeddings
			for (var i = 0; i < documents.Length; i++)
			{
				var document = documents[i];
				var embeddingsArray = embeddings[i].Embedding.ToArray();
				var vectors = new BsonArray(embeddingsArray);
				document["vectors"] = vectors;

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
