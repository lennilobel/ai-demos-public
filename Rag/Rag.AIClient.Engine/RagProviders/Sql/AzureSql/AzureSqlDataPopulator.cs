using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql
{
	public class AzureSqlDataPopulator : SqlServer2022DataPopulator
	{
		public AzureSqlDataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			await base.InitializeData();

			ConsoleHelper.WriteHeading("Load Configuration", ConsoleHelper.UserColor);

			ConsoleHelper.WriteLine("Loading configuration", ConsoleHelper.UserColor);
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
