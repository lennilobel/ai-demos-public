using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.RagProviders.Sql.AzureSql
{
    public class AzureSqlRagProvider : RagProviderBase
    {
        public override string ProviderName => "Azure SQL Database";

        public override string DatabaseName => SqlConfig.DatabaseName + GetDatabaseNameSuffix();

        public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.AzureSql;

		public override string EntityTitleFieldName => "Title";
		
        public override string GetDataFilePath(string filename) => filename;

        public override IDataPopulator GetDataPopulator() => new AzureSqlDataPopulator(this);

        public override IDataVectorizer GetDataVectorizer() => new AzureSqlDataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new AzureSqlMoviesAssistant(this);
    }
}
