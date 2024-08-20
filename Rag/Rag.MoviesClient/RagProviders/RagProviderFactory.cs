using Microsoft.Data.SqlClient;
using Rag.MoviesClient.Config;
using Rag.MoviesClient.RagProviders.Base;
using Rag.MoviesClient.RagProviders.NoSql.CosmosDb;
using Rag.MoviesClient.RagProviders.NoSql.MongoDb;
using Rag.MoviesClient.RagProviders.Sql;
using Rag.MoviesClient.RagProviders.Sql.AzureSql;
using Rag.MoviesClient.RagProviders.Sql.SqlServer;
using System;
using System.IO;

namespace Rag.MoviesClient.RagProviders
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
				case RagProviderType.AzureSql:
					return "Azure SQL Database";

				case RagProviderType.SqlServer:
					return "SQL Server";

				case RagProviderType.CosmosDb:
					return "Azure Cosmos DB for NoSQL";

				case RagProviderType.MongoDb:
					return "Azure Cosmos DB for MongoDB vCore";
			}

			throw new NotSupportedException($"No data vectorizer is implemented for RAG provider type {RagProviderType}");
		}

		public static IDataPopulator GetDataPopulator()
		{
			switch (RagProviderType)
			{
				case RagProviderType.AzureSql:
				case RagProviderType.SqlServer:
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
				case RagProviderType.AzureSql:
					return new AzureSqlDataVectorizer();

				case RagProviderType.SqlServer:
					return new SqlServerDataVectorizer();

				case RagProviderType.CosmosDb:
					return new CosmosDbDataVectorizer();

				case RagProviderType.MongoDb:
					return new MongoDbDataVectorizer();
			}

			throw new NotSupportedException($"No data vectorizer is implemented for RAG provider type {RagProviderType}");
		}

		public static IMoviesAssistant GetMoviesAssistant()
		{
			switch (RagProviderType)
			{
				case RagProviderType.AzureSql:
					return new AzureSqlMoviesAssistant();

				case RagProviderType.SqlServer:
					return new SqlServerMoviesAssistant();

				case RagProviderType.CosmosDb:
					return new CosmosDbMoviesAssistant();

				case RagProviderType.MongoDb:
					return new MongoDbMoviesAssistant();
			}

			throw new NotSupportedException($"No movies assistant is implemented for RAG provider type {RagProviderType}");
		}

		public static string GetSqlConnectionString()
		{
			switch (RagProviderType)
			{
				case RagProviderType.AzureSql:
					return BuildConnectionString(Shared.AppConfig.AzureSql);

				case RagProviderType.SqlServer:
					return BuildConnectionString(Shared.AppConfig.SqlServer);
			}

			throw new NotSupportedException($"No SQL connection string is available for RAG provider type {RagProviderType}");
		}

		private static string BuildConnectionString(AppConfig.SqlConfig config) =>
			new SqlConnectionStringBuilder
			{
				DataSource = config.ServerName,
				InitialCatalog = config.DatabaseName,
				UserID = config.Username,
				Password = config.Password,
				TrustServerCertificate = config.TrustServerCertificate
			}.ConnectionString;

		public static string GetDataFilePath(string filename)
		{
			switch (RagProviderType)
			{
				case RagProviderType.AzureSql:
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
