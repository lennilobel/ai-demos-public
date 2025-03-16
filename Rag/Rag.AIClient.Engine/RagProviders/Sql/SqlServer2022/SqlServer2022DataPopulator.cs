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
	public class SqlServer2022DataPopulator : DataPopulatorBase
	{
		public SqlServer2022DataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			Debugger.Break();

			var started = DateTime.Now;

			ConsoleHelper.WriteHeading("Load Data", ConsoleHelper.UserColor);

			ConsoleHelper.WriteLine("Deleting all data", ConsoleHelper.UserColor);
			await SqlDataAccess.RunStoredProcedure("DeleteAllData");

			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.SqlConfig.JsonInitialDataFilename);
			await this.LoadDataFromJsonFile(filename);

			var elapsed = DateTime.Now.Subtract(started);
			ConsoleHelper.WriteLine();
			ConsoleHelper.WriteLine($"Data loaded in {elapsed}", ConsoleHelper.UserColor);
		}

		private async Task LoadDataFromJsonFile(string filename)
		{
			ConsoleHelper.WriteLine();
			ConsoleHelper.WriteLine($"Loading data from {filename}", ConsoleHelper.UserColor);

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

			ConsoleHelper.WriteHeading("Update Data", ConsoleHelper.UserColor);

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
			ConsoleHelper.WriteLine();
			ConsoleHelper.WriteLine($"Data updated in {elapsed}", ConsoleHelper.InfoColor);
		}

		public override async Task ResetData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Reset Data", ConsoleHelper.UserColor);

			await SqlDataAccess.RunStoredProcedure("DeleteStarWarsTrilogy");
		}

	}
}
