using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.Sql.AzureSql;

namespace Rag.AIClient.Engine.RagProviders
{
	public class AzureSqlRagProvider : SqlRagProviderBase
	{
        public override string ProviderName => "Azure SQL Database";

		public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.AzureSql;

		public override string GetDataFilePath(string filename) => filename;
		
		public override IDataVectorizer GetDataVectorizer() => new AzureSqlDataVectorizer(this);

        public override IAIAssistant GetAIAssistant() => new AzureSqlMoviesAssistant(this);
    }
}
