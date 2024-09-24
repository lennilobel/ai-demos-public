using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.RagProviders.Sql.SqlServer
{
    public class SqlServerRagProvider : RagProviderBase
    {
        public override string ProviderName => "SQL Server";

        public override string DatabaseName => SqlConfig.DatabaseName + GetDatabaseNameSuffix();

        public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.SqlServer;

		public override string EntityTitleFieldName => "Title";
		
        public override IDataPopulator GetDataPopulator() => new SqlServerDataPopulator(this);

        public override IDataVectorizer GetDataVectorizer() => new SqlServerDataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new SqlServerMoviesAssistant(this);
    }

}
