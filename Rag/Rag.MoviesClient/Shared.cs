using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;

namespace Rag.MoviesClient
{
	public static class Shared
    {
        public static AppConfig AppConfig { get; set; }
        public static string SqlServerConnectionString { get; set; }
		public static string AzureSqlConnectionString { get; set; }
		public static CosmosClient CosmosClient { get; set; }
		public static OpenAIClient OpenAIClient { get; set; }

        public static void Initialize()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            AppConfig = config.GetSection("AppConfig").Get<AppConfig>();

			var sqlServer = AppConfig.SqlServer;
			SqlServerConnectionString = sqlServer.ConnectionString;

			var azureSql = AppConfig.AzureSql;
			AzureSqlConnectionString = azureSql.ConnectionString;

			var cosmosDb = AppConfig.CosmosDb;
			CosmosClient = new CosmosClient(cosmosDb.Endpoint, cosmosDb.MasterKey, new CosmosClientOptions { AllowBulkExecution = true });

			var openAI = AppConfig.OpenAI;
            OpenAIClient = new OpenAIClient(new Uri(openAI.Endpoint), new AzureKeyCredential(openAI.ApiKey));
        }

    }
}
