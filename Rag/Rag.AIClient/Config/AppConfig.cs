using Rag.AIClient.EmbeddingModels;
using Rag.AIClient.RagProviders;

namespace Rag.AIClient.Config
{
	// This class is mapped to appsettings.json
	public class AppConfig
    {
        public RagProviderType RagProvider { get; set; }
		public EmbeddingModelType EmbeddingModel { get; set; }

		public SqlConfig SqlServer { get; set; }
        public SqlConfig AzureSql { get; set; }
		public SqlConfig AzureSqlEap { get; set; }
		public class SqlConfig
		{
			public string ServerName { get; set; }
			public string DatabaseName { get; set; }
			public string Username { get; set; }
			public string Password { get; set; }
			public bool TrustServerCertificate { get; set; }
			public string JsonInitialDataFilename { get; set; }
			public string JsonUpdateDataFilename { get; set; }
		}

		public CosmosDbConfig CosmosDb { get; set; }
		public class CosmosDbConfig
		{
			public string Endpoint { get; set; }
			public string MasterKey { get; set; }
			public string DatabaseName { get; set; }
			public string ContainerName { get; set; }
			public string PartitionKeyValue { get; set; }
			public string JsonInitialDataFilename { get; set; }
			public string JsonUpdateDataFilename { get; set; }
		}

		public MongoDbConfig MongoDb { get; set; }
		public class MongoDbConfig
		{
			public string ConnectionString { get; set; }
			public string DatabaseName { get; set; }
			public string CollectionName{ get; set; }
			public string JsonInitialDataFilename { get; set; }
			public string JsonUpdateDataFilename { get; set; }
		}

		public OpenAIConfig OpenAI { get; set; }
		public class OpenAIConfig
		{
			public string Endpoint { get; set; }
			public string ApiKey { get; set; }
			public EmbeddingDeploymentNamesConfig EmbeddingDeploymentNames { get; set; }
			public class EmbeddingDeploymentNamesConfig
			{
				public string Default { get; set; }
				public string TextEmbedding3Large { get; set; }
				public string TextEmbedding3Small { get; set; }
				public string TextEmbeddingAda002 { get; set; }
			}
			public string CompletionsDeploymentName { get; set; }
			public string DalleDeploymentName { get; set; }
		}
	
	}
}
