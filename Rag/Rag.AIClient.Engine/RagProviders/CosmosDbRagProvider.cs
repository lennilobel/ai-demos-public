using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb;

namespace Rag.AIClient.Engine.RagProviders
{
	public class CosmosDbRagProvider : RagProviderBase
	{
		public override string ProviderName => "Azure Cosmos DB for NoSQL";

		public override AppConfig.CosmosDbConfig CosmosDbConfig => Shared.AppConfig.CosmosDb;

		public override string DatabaseName => this.CosmosDbConfig.DatabaseName + base.GetDatabaseNameSuffix();

		public override IDataPopulator GetDataPopulator() => new CosmosDbDataPopulator(this);

		public override IDataVectorizer GetDataVectorizer() => new CosmosDbDataVectorizer(this);

		public override IAIAssistant GetAIAssistant() => new CosmosDbMoviesAssistant(this);
	}
}
