using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbMoviesAssistant : MoviesAssistantBase
	{
		protected virtual AppConfig.CosmosDbConfig CosmosDbConfig => base.RagProvider.CosmosDbConfig;

		public CosmosDbMoviesAssistant(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Generate vector from a natural language query (Embeddings API using a text embedding model)
			var vector = await base.VectorizeQuestion(question);

			// Run a vector search in our database (Cosmos DB NoSQL API vector support)
			var results = await this.RunVectorSearch(vector);

			return results;
		}

		protected async Task<JObject[]> RunVectorSearch(float[] vector)
		{
			var started = DateTime.Now;

			var database = Shared.CosmosClient.GetDatabase(base.RagProvider.DatabaseName);
			var container = database.GetContainer(this.CosmosDbConfig.ContainerName);

            var sql = this.GetVectorSearchSql();

			if (DemoConfig.Instance.ShowInternalOperations)
			{
				ConsoleOutput.WriteHeading("Cosmos DB for NoSQL Vector Search Query", ConsoleColor.Green);
				ConsoleOutput.WriteLine(sql, ConsoleColor.Green);
			}

			try
			{
				var query = new QueryDefinition(sql).WithParameter("@vector", vector);
				var iterator = container.GetItemQueryIterator<JObject>(query);
				var results = new List<JObject>();

				while (iterator.HasMoreResults)
				{
					var page = await iterator.ReadNextAsync();
					foreach (var result in page)
					{
						results.Add(result);
					}
				}

				if (DemoConfig.Instance.ShowInternalOperations)
				{
					ConsoleOutput.WriteHeading("Cosmos DB for NoSQL Vector Search Result", ConsoleColor.Green);

					var counter = 0;
					foreach (var result in results)
					{
						ConsoleOutput.WriteLine($"{++counter}. {result["title"]}", ConsoleColor.Green);
						ConsoleOutput.WriteLine(JsonConvert.SerializeObject(result), ConsoleColor.DarkGreen);
					}
				}

				base._elapsedRunVectorSearch = DateTime.Now.Subtract(started);

				return results.ToArray();
			}
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine("Error running vector search query");
				ConsoleOutput.WriteErrorLine(ex.Message);

				return null;
			}
		}

		// Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
		protected virtual string GetVectorSearchSql() =>
			@"
                SELECT TOP 5
                    c.id,
                    c.title,
                    c.budget,
                    c.genres,
                    c.original_language,
                    c.original_title,
                    c.overview,
                    c.popularity,
                    c.production_companies,
                    c.release_date,
                    c.revenue,
                    c.runtime,
                    c.spoken_languages,
                    c.video,
                    VectorDistance(c.vector, @vector) AS similarity_score
                FROM
                    c
                ORDER BY
                    VectorDistance(c.vector, @vector)
			";

	}
}
