using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.RagProviders.NoSql.MongoDb
{
    public class MongoDbRagProvider : RagProviderBase
    {
        public override string ProviderName => "Azure Cosmos DB for MongoDB vCore";

        public override string DatabaseName => Shared.AppConfig.MongoDb.DatabaseName + GetDatabaseNameSuffix();

        public override AppConfig.MongoDbConfig MongoDbConfig => Shared.AppConfig.MongoDb;

        public override IDataPopulator GetDataPopulator() => new MongoDbDataPopulator(this);

        public override IDataVectorizer GetDataVectorizer() => new MongoDbDataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new MongoDbMoviesAssistant(this);
    }
}
