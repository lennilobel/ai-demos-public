using Microsoft.Data.SqlClient;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.EmbeddingModels;
using System;
using System.IO;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class RagProviderBase : IRagProvider
	{
		public abstract string ProviderName { get; }

		public abstract string DatabaseName { get; }

		public virtual AppConfig.SqlConfig SqlConfig => throw new NotSupportedException($"No SQL configuration is available for RAG provider '{this.ProviderName}'");

		public virtual AppConfig.CosmosDbConfig CosmosDbConfig => throw new NotSupportedException($"No Cosmos DB configuration is available for RAG provider '{this.ProviderName}'");

		public virtual AppConfig.MongoDbConfig MongoDbConfig => throw new NotSupportedException($"No MongoDB configuration is available for RAG provider '{this.ProviderName}'");

		public string SqlConnectionString
		{
			get
			{
				var config = this.SqlConfig;

				var csb = new SqlConnectionStringBuilder
				{
					DataSource = config.ServerName,
					InitialCatalog = this.DatabaseName,
					UserID = config.Username,
					Password = config.Password,
					TrustServerCertificate = config.TrustServerCertificate
				};

				return csb.ConnectionString;
			}
		}

		public abstract string EntityTitleFieldName { get; }

		public virtual string GetDataFilePath(string filename) => this.GetDataFileLocalPath(filename);

		public virtual string GetDataFileLocalPath(string filename) => new FileInfo($@"Data\{filename}").FullName;

		public abstract IDataPopulator GetDataPopulator();

		public abstract IDataVectorizer GetDataVectorizer();

		public abstract IAIAssistant GetAIAssistant();

		protected string GetDatabaseNameSuffix()
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

			throw new NotSupportedException($"No database name suffix is implemented for embedding model type {EmbeddingModelFactory.EmbeddingModelType}");
		}

	}
}
