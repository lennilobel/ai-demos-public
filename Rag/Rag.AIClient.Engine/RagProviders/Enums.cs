namespace Rag.AIClient.Engine.RagProviders
{
    public enum RagProviderType
    {
        SqlServer2022,      // SQL Server 2022
		AzureSql,           // Azure SQL Database
		AzureSqlPreview,    // Azure SQL Database Preview
		CosmosDb,           // Azure Cosmos DB for NoSQL
        MongoDb,            // Azure Cosmos DB for MongoDB vCore
        External,           // An external provider based on any of the above provider types
    }

}
