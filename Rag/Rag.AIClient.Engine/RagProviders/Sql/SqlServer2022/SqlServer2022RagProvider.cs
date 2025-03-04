using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;

namespace Rag.AIClient.Engine.RagProviders.Sql.SqlServer
{
    public class SqlServer2022RagProvider : RagProviderBase
    {
        public override string ProviderName => "SQL Server 2022";

        public override string DatabaseName => SqlConfig.DatabaseName + GetDatabaseNameSuffix();

        public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.SqlServer2022;

		public override string EntityTitleFieldName => "Title";
		
        public override IDataPopulator GetDataPopulator() => new SqlServer2022DataPopulator(this);

        public override IDataVectorizer GetDataVectorizer() => new SqlServer2022DataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new SqlServer2022MoviesAssistant(this);
    }

}
