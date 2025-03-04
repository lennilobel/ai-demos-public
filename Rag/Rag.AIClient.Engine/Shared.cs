using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Rag.AIClient.Engine.Config;
using System;

namespace Rag.AIClient.Engine
{
	public static class Shared
    {
        public static AppConfig AppConfig { get; set; }
		public static CosmosClient CosmosClient { get; set; }
		public static MongoClient MongoClient { get; set; }
		public static AzureOpenAIClient AzureOpenAIClient { get; set; }

		public static void Initialize()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			AppConfig = config.GetSection("AppConfig").Get<AppConfig>();

			CosmosClient = new CosmosClient(
				AppConfig.CosmosDb.Endpoint,
				AppConfig.CosmosDb.MasterKey,
				new CosmosClientOptions { AllowBulkExecution = true }
			);

			MongoClient = new MongoClient(
				AppConfig.MongoDb.ConnectionString
			);

			AzureOpenAIClient = new AzureOpenAIClient(
				new Uri(AppConfig.OpenAI.Endpoint),
				new AzureKeyCredential(AppConfig.OpenAI.ApiKey)
			);
		}

	}
}
