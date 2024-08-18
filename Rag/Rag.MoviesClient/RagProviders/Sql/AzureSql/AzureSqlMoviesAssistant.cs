using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql.AzureSql
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
				storedProcedureParameters: [("@Question", question)],
				getResult: rdr =>
				{
					counter++;
					if (base._showInternalOperations && counter == 1)
					{
						base.ConsoleWriteHeading("AZURE SQL DB VECTOR SEARCH RESULT", ConsoleColor.Green);
					}

					var resultJson = rdr["MovieJson"].ToString();
					var result = JsonConvert.DeserializeObject<JObject>(resultJson);
					results.Add(result);

					if (base._showInternalOperations)
					{
						base.ConsoleWriteLine($"{++counter}. {result["Title"]}", ConsoleColor.Green);
						base.ConsoleWriteLine(JsonConvert.SerializeObject(result));
					}
				},
				silent: true
			);

			base._elapsedRunVectorSearch = DateTime.Now.Subtract(started);

			return results.ToArray();
        }

    }
}
