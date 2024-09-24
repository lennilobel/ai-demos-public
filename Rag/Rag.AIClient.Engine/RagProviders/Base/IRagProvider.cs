using Rag.AIClient.Engine.Config;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public interface IRagProvider
	{
		string ProviderName { get; }
		string DatabaseName { get; }
		AppConfig.SqlConfig SqlConfig { get; }
		AppConfig.CosmosDbConfig CosmosDbConfig { get; }
		AppConfig.MongoDbConfig MongoDbConfig { get; }
		string SqlConnectionString { get; }

		string GetDataFilePath(string filename);
		string GetDataFileLocalPath(string filename);

		IDataPopulator GetDataPopulator();
		IDataVectorizer GetDataVectorizer();
		IAIAssistant GetAIAssistant();
	}
}
