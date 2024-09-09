using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.Sql.SqlServer;

namespace Rag.AIClient.Engine.RagProviders
{
	public class SqlServerRagProvider : SqlRagProviderBase
    {
        public override string ProviderName => "SQL Server";

		public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.SqlServer;

		public override IDataVectorizer GetDataVectorizer() => new SqlServerDataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new SqlServerMoviesAssistant(this);
    }

}
