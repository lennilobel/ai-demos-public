using Rag.AIClient.Engine.Config;

namespace Rag.AIClient.Engine.RagProviders.Sql.AzureSql
{
    public class AzureSqlEapRagProvider : AzureSqlRagProvider
    {
        public override string ProviderName => "Azure SQL Database (EAP)";

        public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.AzureSqlEap;
    }
}
