using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.NoSql.MongoDb
{
	public class MongoDbMoviesAssistant : MoviesAssistantBase
	{
		public MongoDbMoviesAssistant(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Generate vectors from a natural language query (Embeddings API using a text embedding model)
			var vector = await base.VectorizeQuestion(question);

			// Run a vector search in our database (Mongo DB vCore API vector support)
			var results = await this.RunVectorSearch(vector);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(float[] vector)
		{
			var databaseName = base.RagProvider.DatabaseName;
			var collectionName = base.RagProvider.MongoDbConfig.CollectionName;

			var database = Shared.MongoClient.GetDatabase(databaseName);
			var collection = database.GetCollection<BsonDocument>(collectionName);

			var searchJObject = new JObject
			{
				{ "$search", new JObject
					{
						{ "cosmosSearch", new JObject
							{
								{ "vector", new JArray(vector) },
								{ "path", "vector" },  // Path to the vector field
								{ "k", 5 }
							}
						}
					}
				}
			};
 
			var projectJObject = new JObject
			{
				{ "$project", new JObject
					{
						{ "_id", 1},
						{ "title", 1},
						{ "budget", 1},
						{ "genres", 1},
						{ "original_language", 1},
						{ "original_title", 1},
						{ "overview", 1},
						{ "popularity", 1},
						{ "production_companies", 1},
						{ "release_date", 1},
						{ "revenue", 1},
						{ "runtime", 1},
						{ "spoken_languages", 1},
						{ "video", 1},
						{ "similarity_score", new JObject
							{
								{ "$meta", "searchScore" }
							}
						},
					}
				}
			};

			if (DemoConfig.Instance.ShowInternalOperations)
			{
				var showSearch = Newtonsoft.Json.JsonConvert.SerializeObject(searchJObject, Newtonsoft.Json.Formatting.Indented);
				var showProject = Newtonsoft.Json.JsonConvert.SerializeObject(projectJObject, Newtonsoft.Json.Formatting.Indented);
				var vectorArrayPattern = @"(""vector"":\s*\[)[^\]]*(\])";
				showSearch = Regex.Replace(showSearch, vectorArrayPattern, "$1 values... $2");

				ConsoleOutput.WriteHeading("Cosmos DB for MongoDB vCore Vector Search Query", ConsoleColor.Green);
				ConsoleOutput.WriteLine(showSearch, ConsoleColor.Green);
				ConsoleOutput.WriteLine(showProject, ConsoleColor.Green);
			}

			var search = BsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(searchJObject));
			var project = BsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(projectJObject));
			var pipeline = new[] { search,  project };

			var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);

			var results = new List<JObject>();

			await cursor.ForEachAsync(bsonDocument =>
			{
				var json = bsonDocument.ToJson();
				var document = JObject.Parse(json);
				results.Add(document);
			});

			return results.ToArray();
		}

	}
}
