using Microsoft.Data.SqlClient;
using Rag.AIClient.Config;
using Rag.AIClient.EmbeddingModels;
using Rag.AIClient.RagProviders.Base;
using Rag.AIClient.RagProviders.NoSql.CosmosDb;
using Rag.AIClient.RagProviders.NoSql.MongoDb;
using Rag.AIClient.RagProviders.Sql;
using Rag.AIClient.RagProviders.Sql.AzureSql;
using Rag.AIClient.RagProviders.Sql.SqlServer;
using System;
using System.IO;

namespace Rag.AIClient.RagProviders
{
	public static class RagProviderFactory
	{
		public static RagProviderType RagProviderType { get; set; }

		static RagProviderFactory()
		{
			var args = Environment.GetCommandLineArgs();

			RagProviderType = args.Length > 1
				? (RagProviderType)Enum.Parse(typeof(RagProviderType), args[1], ignoreCase: true)
				: Shared.AppConfig.RagProvider;
		}

		public static string GetProviderName()
		{
			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
					return "SQL Server";

				case RagProviderType.AzureSql:
					return "Azure SQL Database";

				case RagProviderType.AzureSqlEap:
					return "Azure SQL Database (EAP)";

				case RagProviderType.CosmosDb:
					return "Azure Cosmos DB for NoSQL";

				case RagProviderType.MongoDb:
					return "Azure Cosmos DB for MongoDB vCore";
			}

			throw new NotSupportedException($"No provider name is implemented for RAG provider type {RagProviderType}");
		}

		public static string GetDatabaseName()
		{
			var suffix = GetDatabaseNameSuffix();

			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
					return Shared.AppConfig.SqlServer.DatabaseName + suffix;

				case RagProviderType.AzureSql:
					return Shared.AppConfig.AzureSql.DatabaseName + suffix;

				case RagProviderType.AzureSqlEap:
					return Shared.AppConfig.AzureSqlEap.DatabaseName + suffix;

				case RagProviderType.CosmosDb:
					return Shared.AppConfig.CosmosDb.DatabaseName + suffix;

				case RagProviderType.MongoDb:
					return Shared.AppConfig.MongoDb.DatabaseName + suffix;
			}

			throw new NotSupportedException($"No database name is implemented for RAG provider type {RagProviderType}");
		}

		private static string GetDatabaseNameSuffix()
		{
			switch (EmbeddingModelFactory.EmbeddingModelType)
			{
				case EmbeddingModelType.Default:
					return string.Empty;

				case EmbeddingModelType.TextEmbedding3Large:
					return "-3l";

				case EmbeddingModelType.TextEmbedding3Small:
					return "-3s";

				case EmbeddingModelType.TextEmbeddingAda002:
					return "-ada";
			}

			throw new NotSupportedException($"No database name suffix is implemented for RAG provider type {RagProviderType}");
		}

		public static IDataPopulator GetDataPopulator()
		{
			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
				case RagProviderType.AzureSql:
				case RagProviderType.AzureSqlEap:
					return new SqlDataPopulator();

				case RagProviderType.CosmosDb:
					return new CosmosDbDataPopulator();

				case RagProviderType.MongoDb:
					return new MongoDbDataPopulator();
			}

			throw new NotSupportedException($"No data populator is implemented for RAG provider type {RagProviderType}");
		}

		public static IDataVectorizer GetDataVectorizer()
		{
			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
					return new SqlServerDataVectorizer();

				case RagProviderType.AzureSql:
				case RagProviderType.AzureSqlEap:
					return new AzureSqlDataVectorizer();

				case RagProviderType.CosmosDb:
					return new CosmosDbDataVectorizer();

				case RagProviderType.MongoDb:
					return new MongoDbDataVectorizer();
			}

			throw new NotSupportedException($"No data vectorizer is implemented for RAG provider type {RagProviderType}");
		}

		public static IAIAssistant GetAIAssistant()
		{
			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
					return new SqlServerMoviesAssistant();

				case RagProviderType.AzureSql:
				case RagProviderType.AzureSqlEap:
					return new AzureSqlMoviesAssistant();

				case RagProviderType.CosmosDb:
					return new CosmosDbMoviesAssistant();

				case RagProviderType.MongoDb:
					return new MongoDbMoviesAssistant();
			}

			throw new NotSupportedException($"No AI assistant is implemented for RAG provider type {RagProviderType}");
		}

		public static string GetSqlConnectionString()
		{
			var config = GetSqlConfig();

			var csb = new SqlConnectionStringBuilder
			{
				DataSource = config.ServerName,
				InitialCatalog = GetDatabaseName(),
				UserID = config.Username,
				Password = config.Password,
				TrustServerCertificate = config.TrustServerCertificate
			};
			
			return csb.ConnectionString;
		}

		public static AppConfig.SqlConfig GetSqlConfig()
		{
			switch (RagProviderType)
			{
				case RagProviderType.SqlServer:
					return Shared.AppConfig.SqlServer;

				case RagProviderType.AzureSql:
					return Shared.AppConfig.AzureSql;

				case RagProviderType.AzureSqlEap:
					return Shared.AppConfig.AzureSqlEap;
			}

			throw new NotSupportedException($"No SQL configuration is available for RAG provider type {RagProviderType}");
		}

		public static AppConfig.CosmosDbConfig GetCosmosDbConfig()
		{
			switch (RagProviderType)
			{
				case RagProviderType.CosmosDb:
					return Shared.AppConfig.CosmosDb;
			}

			throw new NotSupportedException($"No Cosmos DB configuration is available for RAG provider type {RagProviderType}");
		}

		public static string GetDataFilePath(string filename)
		{
			switch (RagProviderType)
			{
				case RagProviderType.AzureSql:
				case RagProviderType.AzureSqlEap:
					return filename;

				case RagProviderType.SqlServer:
				case RagProviderType.CosmosDb:
				case RagProviderType.MongoDb:
					return new FileInfo($@"Data\{filename}").FullName;
			}

			throw new NotSupportedException($"No data file path is implemented for RAG provider type {RagProviderType}");
		}

	}
}
