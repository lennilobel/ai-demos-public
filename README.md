# AI Demos

Welcome to my repository of AI demos!

## Hiker

A series of basic AI demos for both OpenAI and Semantic Kernel

## RAG

A provider-based RAG solution supporting:
- SQL Server (on-premises)
- Azure SQL Database
- Azure Cosmos DB for NoSQL
- Azure Cosmos DB for MongoDB vCore

|                                      | SQL Server (on-prem)                                                                                                        | Azure SQL Database                                                                                                      | Cosmos DB NoSQL API                                                                                                           | Cosmos DB MongoDB vCore                                                                                                         |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------|
| Load initial data                    | Server loads from JSON file on local disk.<br>Server parses and inserts data into tables.<br>Server calls OpenAI to vectorize data. | Server loads from JSON file in Azure Blob Storage.<br>Server parses and inserts data into tables.<br>Server calls OpenAI to vectorize data. | Client loads from JSON file on local disk (bulk load).<br>Server calls OpenAI to vectorize data.<br>Vectors are added to documents. | Client loads from JSON file on local disk (bulk load).<br>Server calls OpenAI to vectorize data.<br>Vectors are added to documents. |
| Vectorize initial data load          | Client retrieves data from server.<br>Client calls OpenAI to vectorize data.<br>Vectors are added to documents.              | Server calls OpenAI to vectorize data.<br>Server saves vectors into a vector index table.                                 | Client retrieves data from server.<br>Client calls OpenAI to vectorize data.<br>Vectors are added to documents.              | Client retrieves data from server.<br>Client calls OpenAI to vectorize data.<br>Vectors are added to documents.                |
| Vectorize new/updated data           | Client calls OpenAI to vectorize new/updated data.<br>Vectors are added to tables.                                           | Client sends new/updated data to server.<br>Server calls OpenAI to vectorize new/updated data.<br>Vectors are added to tables. | Azure Function captures new/updated documents (Cosmos DB trigger).<br>Function calls OpenAI to vectorize data.<br>Vectors are added to documents. | Client calls OpenAI to vectorize new/updated data.<br>Vectors are added to documents.                                           |
| Delete data                          | Server deletes data from tables.<br>Server deletes corresponding vectors from vector index table.                           | Server deletes data with vectors.                                                                                         | Server deletes data with vectors.                                                                                             | Server deletes data with vectors.                                                                                               |
| AI assistant                         | Client calls OpenAI to vectorize question.<br>Server retrieves matching data from vector index.<br>Server returns results.  | Server calls OpenAI to vectorize question.<br>Server retrieves matching data from vector index.<br>Server returns results. | Client calls OpenAI to vectorize question.<br>Server retrieves matching data from vector index.<br>Server returns results.  | Client calls OpenAI to vectorize question.<br>Server retrieves matching data from vector index.<br>Server returns results.      |
