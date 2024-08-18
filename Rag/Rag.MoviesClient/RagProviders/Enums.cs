namespace Rag.MoviesClient.RagProviders
{
    public enum RagProviderType
    {
        AzureSql,   // Azure SQL Database
        SqlServer,  // SQL Server (on-prem)
        CosmosDb,   // Azure Cosmos DB for NoSQL
        MongoDb,    // Azure Cosmos DB for MongoDB vCore
    }
}
