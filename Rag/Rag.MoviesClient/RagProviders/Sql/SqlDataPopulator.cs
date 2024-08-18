using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql
{
	public class SqlDataPopulator : RagProviderBase, IDataPopulator
	{
		public async Task LoadData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			base.ConsoleWriteHeading("Load Data", ConsoleColor.Yellow);

			base.ConsoleWriteLine("Deleting all data", ConsoleColor.Yellow);
			await SqlDataAccess.RunStoredProcedure("DeleteAllData");

			await this.LoadDataFromJsonFile("movies.json");

			var elapsed = DateTime.Now.Subtract(started);
			base.ConsoleWriteLine();
			base.ConsoleWriteLine($"Data loaded in {elapsed}", ConsoleColor.Yellow);
		}

		public async Task UpdateData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			base.ConsoleWriteHeading("Update Data", ConsoleColor.Yellow);

			// Load additional movies into the database
			await this.LoadDataFromJsonFile("movies-sw.json");

			// Vectorize the new movies
			var documents = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(@"Data\movies-sw.json"));
			var movieIds = documents.Select(d => ((JObject)d)["id"].Value<int>()).ToArray();

			var vectorizer = RagProviderFactory.GetDataVectorizer();
			foreach (var movieId in movieIds)
			{
				await vectorizer.VectorizeData(movieId);
			}

			var elapsed = DateTime.Now.Subtract(started);
			base.ConsoleWriteLine();
			base.ConsoleWriteLine($"Data updated in {elapsed}", ConsoleColor.Cyan);
		}

		private async Task LoadDataFromJsonFile(string filename)
		{
			base.ConsoleWriteLine();
			base.ConsoleWriteLine($"Loading data from {filename}", ConsoleColor.Yellow);

			filename = RagProviderFactory.GetDataFilePath(filename);

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "LoadMovies",
				storedProcedureParameters: [("@Filename", filename)]);
		}

		public async Task ResetData()
		{
			Debugger.Break();

			base.ConsoleWriteHeading("Reset Data", ConsoleColor.Yellow);

			await SqlDataAccess.RunStoredProcedure("DeleteStarWarsTrilogy");
		}

	}
}
