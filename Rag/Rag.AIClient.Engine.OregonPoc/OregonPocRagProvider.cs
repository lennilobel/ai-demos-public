using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.OregonPoc
{
	public class OregonPocRagProvider : CosmosDbRagProvider
	{
		public override string ProviderName => "Oregon POC";

		public override AppConfig.CosmosDbConfig CosmosDbConfig => Shared.AppConfig.GetExternalRagProvider("OregonPoc").CosmosDb;

		public override IAIAssistant GetAIAssistant() => new OregonPocDpmsAssistant(this);

	}
}
