namespace Rag.AIClient.Engine.RagProviders
{
    public enum RagProviderType
    {
        SqlServer,      // SQL Server 2022
		AzureSql,       // Azure SQL Database
		AzureSqlEap,    // Azure SQL Database EAP
		CosmosDb,       // Azure Cosmos DB for NoSQL
        MongoDb,        // Azure Cosmos DB for MongoDB vCore
        External,       // An external provider based on any of the above provider types
    }

}
