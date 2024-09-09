using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.Custom
{
	public class ProductsRagProvider : CosmosDbRagProvider
	{
		public override string ProviderName => "Products Custom Provider";

		public override AppConfig.CosmosDbConfig CosmosDbConfig => Shared.AppConfig.GetExternalRagProvider("Products").CosmosDb;

		public override IAIAssistant GetAIAssistant() => new ProductsAssistant(this);

	}
}
