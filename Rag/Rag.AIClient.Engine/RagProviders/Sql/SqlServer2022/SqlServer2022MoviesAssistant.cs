using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql.SqlServer
{
	public class SqlServer2022MoviesAssistant : MoviesAssistantBase
    {
		public SqlServer2022MoviesAssistant(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Generate vector from a natural language query (Embeddings API using a text embedding model)
			var vector = await base.VectorizeQuestion(question);

			// Run a vector search in our database (SQL Server similarity query)
			var results = await this.RunVectorSearch(vector);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(float[] vector)
        {
            var started = DateTime.Now;

            var vectorTable = new DataTable();
            vectorTable.Columns.Add("VectorValueId", typeof(int));
            vectorTable.Columns.Add("VectorValue", typeof(float));

            for (var i = 0; i < vector.Length; i++)
            {
                vectorTable.Rows.Add(i + 1, vector[i]);
            }

            var results = new List<JObject>();

			var counter = 0;
			await SqlDataAccess.RunStoredProcedure(
                storedProcedureName: "RunVectorSearch",
                storedProcedureParameters: 
				[
					("@Vector", vectorTable)
				],
                getResult: rdr =>
				{
					counter++;
					if (DemoConfig.Instance.ShowInternalOperations && counter == 1)
					{
						ConsoleOutput.WriteHeading("SQL Server Vector Search Result", ConsoleColor.Green);
					}

					var resultJson = rdr["MovieJson"].ToString();
					var result = JsonConvert.DeserializeObject<JObject>(resultJson);
					results.Add(result);

					if (DemoConfig.Instance.ShowInternalOperations)
					{
						ConsoleOutput.WriteLine($"{++counter}. {result["Title"]}", ConsoleColor.Green);
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
