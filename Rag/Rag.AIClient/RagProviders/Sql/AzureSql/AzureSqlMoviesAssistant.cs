using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Config;
using Rag.AIClient.EmbeddingModels;
using Rag.AIClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Sql.AzureSql
{
	public class AzureSqlMoviesAssistant : MoviesAssistantBase
	{
		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Run a vector search in our database (Azure SQL Database via Embeddings API using a text embedding model)
			var results = await this.RunVectorSearch(question);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(string question)
		{
			var started = DateTime.Now;

			base.ConsoleWriteWaitingFor("Running vector search");

			var results = new List<JObject>();

			var counter = 0;
			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "AskQuestion",
				storedProcedureParameters:
				[
					("@Question", question),
					("@OpenAIEndpoint", Shared.AppConfig.OpenAI.Endpoint),
					("@OpenAIApiKey", Shared.AppConfig.OpenAI.ApiKey),
					("@OpenAIDeploymentName", EmbeddingModelFactory.GetDeploymentName()),
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