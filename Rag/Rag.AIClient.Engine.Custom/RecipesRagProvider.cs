using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb;

namespace Rag.AIClient.Engine.Custom
{
    public class RecipesRagProvider : CosmosDbRagProvider
	{
		public override string ProviderName => "Recipes Custom Provider";

		public override AppConfig.CosmosDbConfig CosmosDbConfig => Shared.AppConfig.GetExternalRagProvider("Recipes").CosmosDb;

		public override string EntityTitleFieldName => "name";

		public override IAIAssistant GetAIAssistant() => new RecipesAssistant(this);

	}
}
