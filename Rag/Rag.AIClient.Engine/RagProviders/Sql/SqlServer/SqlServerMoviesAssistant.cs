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
	public class SqlServerMoviesAssistant : MoviesAssistantBase
    {
		public SqlServerMoviesAssistant(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task<JObject[]> GetDatabaseResults(string question)
		{
			// Generate vectors from a natural language query (Embeddings API using a text embedding model)
			var vectors = await base.VectorizeQuestion(question);

			// Run a vector search in our database (SQL Server similarity query)
			var results = await this.RunVectorSearch(vectors);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(float[] vectors)
        {
            var started = DateTime.Now;

            base.ConsoleWriteWaitingFor("Running vector search");

            var vectorsTable = new DataTable();
            vectorsTable.Columns.Add("VectorValueId", typeof(int));
            vectorsTable.Columns.Add("VectorValue", typeof(float));

            for (var i = 0; i < vectors.Length; i++)
            {
                vectorsTable.Rows.Add(i + 1, vectors[i]);
            }

            var results = new List<JObject>();

			var counter = 0;
			await SqlDataAccess.RunStoredProcedure(
                storedProcedureName: "RunVectorSearch",
                storedProcedureParameters: 
				[
					("@Vectors", vectorsTable)
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
