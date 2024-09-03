using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Config;
using Rag.AIClient.RagProviders.Base;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Sql
{
	public class SqlDataPopulator : IDataPopulator
	{
		public async Task LoadData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			ConsoleOutput.WriteHeading("Load Data", ConsoleColor.Yellow);

			ConsoleOutput.WriteLine("Deleting all data", ConsoleColor.Yellow);
			await SqlDataAccess.RunStoredProcedure("DeleteAllData");

			await this.LoadDataFromJsonFile(RagProviderFactory.GetSqlConfig().JsonInitialDataFilename);

			var elapsed = DateTime.Now.Subtract(started);
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Data loaded in {elapsed}", ConsoleColor.Yellow);
		}

		private async Task LoadDataFromJsonFile(string filename)
		{
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Loading data from {filename}", ConsoleColor.Yellow);

			filename = RagProviderFactory.GetDataFilePath(filename);

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "LoadMovies",
				storedProcedureParameters:
				[
					("@Filename", filename)
				]
			);
		}

		public async Task UpdateData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			ConsoleOutput.WriteHeading("Update Data", ConsoleColor.Yellow);

			// Load additional movies into the database
			await this.LoadDataFromJsonFile(RagProviderFactory.GetSqlConfig().JsonUpdateDataFilename);

			// Vectorize the new movies
			var documents = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(@$"Data\{RagProviderFactory.GetSqlConfig().JsonUpdateDataFilename}"));
			var movieIds = documents.Select(d => ((JObject)d)["id"].Value<int>()).ToArray();

			var vectorizer = RagProviderFactory.GetDataVectorizer();
			await vectorizer.VectorizeData(movieIds);

			var elapsed = DateTime.Now.Subtract(started);
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Data updated in {elapsed}", ConsoleColor.Cyan);
		}

		public async Task ResetData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Reset Data", ConsoleColor.Yellow);

			await SqlDataAccess.RunStoredProcedure("DeleteStarWarsTrilogy");
		}

	}
}
