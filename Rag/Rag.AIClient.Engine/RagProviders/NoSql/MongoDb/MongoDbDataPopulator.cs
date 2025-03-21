using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataPopulator : DataPopulatorBase
	{
		public MongoDbDataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Load Data", ConsoleHelper.UserColor);

			this.DropDatabase();
			var collection = await this.CreateCollection();
			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.MongoDbConfig.JsonInitialDataFilename);

			await this.CreateDocuments(filename, collection);
		}

		private void DropDatabase()
		{
			var databaseName = base.RagProvider.DatabaseName;
			ConsoleHelper.WriteLine($"Deleting database '{databaseName}' (if exists)");
			Shared.MongoClient.DropDatabase(databaseName);
		}

		private async Task<IMongoCollection<BsonDocument>> CreateCollection()
		{
			var collectionName = base.RagProvider.MongoDbConfig.CollectionName;

			var database = Shared.MongoClient.GetDatabase(base.RagProvider.DatabaseName);
			await database.CreateCollectionAsync(collectionName);

			var createVectorIndexCommand = new BsonDocument
			{
				{ "createIndexes", collectionName },
				{ "indexes", new BsonArray
					{
						new BsonDocument
						{
							{ "name", "VectorSearchIndex" },
							{ "key", new BsonDocument { { "vector", "cosmosSearch" } } },	// property path to generated vector array
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

			ConsoleHelper.WriteLine($"Created '{collectionName}' collection");
			return collection;
		}

		private async Task CreateDocuments(string jsonFilename, IMongoCollection<BsonDocument> collection)
		{
			ConsoleHelper.WriteLine($"Creating documents from {jsonFilename}");

			var started = DateTime.Now;

			var json = await File.ReadAllTextAsync(jsonFilename);

			var documents = BsonSerializer.Deserialize<IEnumerable<BsonDocument>>(json);

			foreach (var document in documents)
			{
				document["_id"] = document["id"];
			}

			await collection.InsertManyAsync(documents);

			ConsoleHelper.WriteLine($"Created {documents.Count()} document(s) in {DateTime.Now.Subtract(started)}");
		}

		public override async Task UpdateData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Update Data", ConsoleHelper.UserColor);

			var databaseName = base.RagProvider.DatabaseName;
			var collectionName = base.RagProvider.MongoDbConfig.CollectionName;

			var database = Shared.MongoClient.GetDatabase(databaseName);
			var collection = database.GetCollection<BsonDocument>(collectionName);
			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.MongoDbConfig.JsonUpdateDataFilename);

			await this.CreateDocuments(filename, collection);
		}

		public override async Task ResetData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Reset Data", ConsoleHelper.UserColor);

			var databaseName = base.RagProvider.DatabaseName;
			var collectionName = base.RagProvider.MongoDbConfig.CollectionName;

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

			ConsoleHelper.WriteLine($"Deleted {result.DeletedCount} movie(s)");
		}

	}
}
