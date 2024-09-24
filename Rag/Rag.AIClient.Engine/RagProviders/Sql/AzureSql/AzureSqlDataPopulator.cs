using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql
{
	public class AzureSqlDataPopulator : SqlServerDataPopulator
	{
		public AzureSqlDataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			await base.InitializeData();

			ConsoleOutput.WriteHeading("Load Configuration", ConsoleColor.Yellow);

			ConsoleOutput.WriteLine("Loading configuration", ConsoleColor.Yellow);
			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "LoadConfig",
				storedProcedureParameters:
				[
					("@OpenAIEndpoint", Shared.AppConfig.OpenAI.Endpoint),
					("@OpenAIApiKey", Shared.AppConfig.OpenAI.ApiKey),
					("@OpenAIDeploymentName", EmbeddingModelFactory.GetDeploymentName()),
				]
			);
		}

	}
}
