using Rag.AIClient.Engine.Config;

namespace Rag.AIClient.Engine.RagProviders.Sql.AzureSql
{
    public class AzureSqlPreviewRagProvider : AzureSqlRagProvider
    {
        public override string ProviderName => "Azure SQL Database (Preview)";

        public override AppConfig.SqlConfig SqlConfig => Shared.AppConfig.AzureSqlPreview;
    }
}
