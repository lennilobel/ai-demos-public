using Rag.MoviesClient.RagProviders;

namespace Rag.MoviesClient
{
    public class AppConfig
    {
		public RagProviderType RagProvider { get; set; }

		public SqlServerConfig SqlServer{ get; set; }
		public class SqlServerConfig
		{
			public string ConnectionString { get; set; }
		}

		public AzureSqlConfig AzureSql { get; set; }
		public class AzureSqlConfig
		{
			public string ConnectionString { get; set; }
		}

		public CosmosDbConfig CosmosDb { get; set; }
		public class CosmosDbConfig
		{
			public string Endpoint { get; set; }
			public string MasterKey { get; set; }
			public string DatabaseName { get; set; }
			public string ContainerName { get; set; }
		}
		
		public OpenAIConfig OpenAI { get; set; }
		public class OpenAIConfig
        {
            public string Endpoint { get; set; }
            public string ApiKey { get; set; }
            public string EmbeddingsDeploymentName { get; set; }
            public string CompletionsDeploymentName { get; set; }
            public string DalleDeploymentName { get; set; }
        }

    }
}
