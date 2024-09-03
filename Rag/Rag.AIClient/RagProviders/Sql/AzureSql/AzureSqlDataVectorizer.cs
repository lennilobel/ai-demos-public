 using Rag.AIClient.EmbeddingModels;
using Rag.AIClient.RagProviders.Base;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Sql.AzureSql
{
	public class AzureSqlDataVectorizer : DataVectorizerBase
	{
		protected override async Task VectorizeEntities(int[] movieIds)
		{
			Debugger.Break();

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "VectorizeMovies",
				storedProcedureParameters:
				[
					("@MovieIdsCsv", movieIds == null ? null : string.Join(',', movieIds)),
					("@OpenAIEndpoint", Shared.AppConfig.OpenAI.Endpoint),
					("@OpenAIApiKey", Shared.AppConfig.OpenAI.ApiKey),
					("@OpenAIDeploymentName", EmbeddingModelFactory.GetDeploymentName()),
				]
			);
		}

	}
}
