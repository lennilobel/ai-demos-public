using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql.AzureSql
{
	public class AzureSqlMoviesAssistant : MoviesAssistantBase
	{
		public AzureSqlMoviesAssistant(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Run a vector search in our database (Azure SQL Database via Embeddings API using a text embedding model)
			var results = await this.RunVectorSearch(question);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(string question)
		{
			var started = DateTime.Now;

			var results = new List<JObject>();

			var counter = 0;
			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "AskQuestion",
				storedProcedureParameters:
				[
					("@Question", question),
				],
				getResult: rdr =>
				{
					counter++;
					if (DemoConfig.Instance.ShowInternalOperations && counter == 1)
					{
						ConsoleOutput.WriteHeading("Azure SQL Database Vector Search Result", ConsoleColor.Green);
					}

					var resultJson = rdr["MovieJson"].ToString();
					var result = JsonConvert.DeserializeObject<JObject>(resultJson);
					results.Add(result);

					if (DemoConfig.Instance.ShowInternalOperations)
					{
						ConsoleOutput.WriteLine($"{++counter}. {result["Title"]} (similarity: {rdr["SimilarityScore"]})", ConsoleColor.Green);
						ConsoleOutput.WriteLine(JsonConvert.SerializeObject(result));
					}
				},
				silent: true
			);

			base._elapsedRunVectorSearch = DateTime.Now.Subtract(started);

			return results.ToArray();
		}

	}
}
