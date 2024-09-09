using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.Custom
{
	public class RecipesRagProvider : CosmosDbRagProvider
	{
		public override string ProviderName => "Recipes Custom Provider";

		public override AppConfig.CosmosDbConfig CosmosDbConfig => Shared.AppConfig.GetExternalRagProvider("Recipes").CosmosDb;

		public override IAIAssistant GetAIAssistant() => new RecipesAssistant(this);

	}
}
