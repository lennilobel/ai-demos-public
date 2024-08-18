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
		public static string GetProviderName()
		{
			switch (Shared.AppConfig.RagProvider)
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

			throw new NotSupportedException($"No data vectorizer is implemented for RAG provider type {Shared.AppConfig.RagProvider}");
		}

		public static IDataPopulator GetDataPopulator()
		{
			switch (Shared.AppConfig.RagProvider)
			{
				case RagProviderType.AzureSql:
				case RagProviderType.SqlServer:
					return new SqlDataPopulator();

				case RagProviderType.CosmosDb:
					return new CosmosDbDataPopulator();

				case RagProviderType.MongoDb:
					return new MongoDbDataPopulator();
			}

			throw new NotSupportedException($"No data populator is implemented for RAG provider type {Shared.AppConfig.RagProvider}");
		}

		public static IDataVectorizer GetDataVectorizer()
		{
			switch (Shared.AppConfig.RagProvider)
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

			throw new NotSupportedException($"No data vectorizer is implemented for RAG provider type {Shared.AppConfig.RagProvider}");
		}

		public static IMoviesAssistant GetMoviesAssistant()
		{
			switch (Shared.AppConfig.RagProvider)
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

			throw new NotSupportedException($"No movies assistant is implemented for RAG provider type {Shared.AppConfig.RagProvider}");
		}

		public static string GetSqlConnectionString()
		{
			switch (Shared.AppConfig.RagProvider)
			{
				case RagProviderType.AzureSql:
					return Shared.AppConfig.AzureSql.ConnectionString;

				case RagProviderType.SqlServer:
					return Shared.AppConfig.SqlServer.ConnectionString;

			}

			throw new NotSupportedException($"No SQL connection string is available for RAG provider type {Shared.AppConfig.RagProvider}");
		}

		public static string GetDataFilePath(string filename)
		{
			switch (Shared.AppConfig.RagProvider)
			{
				case RagProviderType.AzureSql:
					return filename;

				case RagProviderType.SqlServer:
				case RagProviderType.CosmosDb:
				case RagProviderType.MongoDb:
					return new FileInfo($@"Data\{filename}").FullName;
			}

			throw new NotSupportedException($"No data file path is implemented for RAG provider type {Shared.AppConfig.RagProvider}");
		}

	}
}
