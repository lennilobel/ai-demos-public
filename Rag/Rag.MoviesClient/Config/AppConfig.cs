using Rag.MoviesClient.RagProviders;

namespace Rag.MoviesClient.Config
{
	// This class is mapped to appsettings.json
	public class AppConfig
    {
        public RagProviderType RagProvider { get; set; }

        public SqlConfig SqlServer { get; set; }
        public SqlConfig AzureSql { get; set; }
		public class SqlConfig
		{
			public string ServerName { get; set; }
			public string DatabaseName { get; set; }
			public string Username { get; set; }
			public string Password { get; set; }
			public bool TrustServerCertificate { get; set; }
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
