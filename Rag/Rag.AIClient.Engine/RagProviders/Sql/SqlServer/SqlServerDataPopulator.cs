using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql
{
	public class SqlServerDataPopulator : DataPopulatorBase
	{
		public SqlServerDataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			ConsoleOutput.WriteHeading("Load Data", ConsoleColor.Yellow);

			ConsoleOutput.WriteLine("Deleting all data", ConsoleColor.Yellow);
			await SqlDataAccess.RunStoredProcedure("DeleteAllData");

			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.SqlConfig.JsonInitialDataFilename);
			await this.LoadDataFromJsonFile(filename);

			var elapsed = DateTime.Now.Subtract(started);
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Data loaded in {elapsed}", ConsoleColor.Yellow);
		}

		private async Task LoadDataFromJsonFile(string filename)
		{
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Loading data from {filename}", ConsoleColor.Yellow);

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "LoadMovies",
				storedProcedureParameters:
				[
					("@Filename", filename)
				]
			);
		}

		public override async Task UpdateData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			ConsoleOutput.WriteHeading("Update Data", ConsoleColor.Yellow);

			// Load additional data into the database
			var remoteFilename = base.RagProvider.GetDataFilePath(base.RagProvider.SqlConfig.JsonUpdateDataFilename);
			await this.LoadDataFromJsonFile(remoteFilename);

			// Vectorize the new data
			var localFilename = base.RagProvider.GetDataFileLocalPath(base.RagProvider.SqlConfig.JsonUpdateDataFilename);
			var documents = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(localFilename));
			var movieIds = documents.Select(d => ((JObject)d)["id"].Value<int>()).ToArray();

			var vectorizer = base.RagProvider.GetDataVectorizer();
			await vectorizer.VectorizeData(movieIds);

			var elapsed = DateTime.Now.Subtract(started);
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Data updated in {elapsed}", ConsoleColor.Cyan);
		}

		public override async Task ResetData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Reset Data", ConsoleColor.Yellow);

			await SqlDataAccess.RunStoredProcedure("DeleteStarWarsTrilogy");
		}

	}
}
