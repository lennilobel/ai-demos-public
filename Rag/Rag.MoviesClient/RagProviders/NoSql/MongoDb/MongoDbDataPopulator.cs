using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataPopulator : IDataPopulator
	{
		public async Task LoadData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Load Data", ConsoleColor.Yellow);

			this.DropDatabase();
			var collection = await this.CreateCollection();
			await this.CreateDocuments(@"Data\movies.json", collection);
		}

		private void DropDatabase()
		{
			var databaseName = RagProviderFactory.GetDatabaseName();
			ConsoleOutput.WriteLine($"Deleting database '{databaseName}' (if exists)");
			Shared.MongoClient.DropDatabase(databaseName);
		}

		private async Task<IMongoCollection<BsonDocument>> CreateCollection()
		{
			var collectionName = Shared.AppConfig.MongoDb.CollectionName;

			var database = Shared.MongoClient.GetDatabase(RagProviderFactory.GetDatabaseName());
			await database.CreateCollectionAsync(collectionName);

			var createVectorIndexCommand = new BsonDocument
			{
				{ "createIndexes", collectionName },
				{ "indexes", new BsonArray
					{
						new BsonDocument
						{
							{ "name", "VectorSearchIndex" },
							{ "key", new BsonDocument { { "vectors", "cosmosSearch" } } },	// property path to generated vector array
							{ "cosmosSearchOptions", new BsonDocument
								{
									{ "kind", "vector-ivf" },	// use IVF (Inverted File) algorithm (max 2000 dimensions for ivfflat index)
									{ "numLists", 1 },			// number of clusters that the IVF index uses to group the vector data
									{ "similarity", "COS" },	// calculates the similarity between two vector arrays
									{ "dimensions", 1536 }		// vector array size (must match the embeddings model; start with 256, increase for greater accuracy)
								}
							}
						}
					}
				}
			};

			await database.RunCommandAsync<BsonDocument>(createVectorIndexCommand);

			var collection = database.GetCollection<BsonDocument>(collectionName);

			ConsoleOutput.WriteLine($"Created '{collectionName}' collection");
			return collection;
		}

		private async Task CreateDocuments(string jsonFilename, IMongoCollection<BsonDocument> collection)
		{
			ConsoleOutput.WriteLine($"Creating documents from {jsonFilename}");

			var started = DateTime.Now;

			var json = await File.ReadAllTextAsync(jsonFilename);

			var documents = BsonSerializer.Deserialize<IEnumerable<BsonDocument>>(json);

			foreach (var document in documents)
			{
				document["_id"] = document["id"];
			}

			await collection.InsertManyAsync(documents);

			ConsoleOutput.WriteLine($"Created {documents.Count()} document(s) in {DateTime.Now.Subtract(started)}");
		}

		public async Task UpdateData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Update Data", ConsoleColor.Yellow);

			var databaseName = RagProviderFactory.GetDatabaseName();
			var collectionName = Shared.AppConfig.MongoDb.CollectionName;

			var database = Shared.MongoClient.GetDatabase(databaseName);
			var collection = database.GetCollection<BsonDocument>(collectionName);

			await this.CreateDocuments(@"Data\movies-sw.json", collection);
		}

		public async Task ResetData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Reset Data", ConsoleColor.Yellow);

			var databaseName = RagProviderFactory.GetDatabaseName();
			var collectionName = Shared.AppConfig.MongoDb.CollectionName;

			var database = Shared.MongoClient.GetDatabase(databaseName);
			var collection = database.GetCollection<BsonDocument>(collectionName);

			var moviesToDelete = new[]
			{
				"Star Wars",
				"The Empire Strikes Back",
				"Return of the Jedi"
			};

			var filter = Builders<BsonDocument>.Filter.In("title", moviesToDelete);

			var result = await collection.DeleteManyAsync(filter);

			ConsoleOutput.WriteLine($"Deleted {result.DeletedCount} movie(s)");
		}

	}
}
