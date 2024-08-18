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

| Task                          | SQL Server (on-prem)                                                 | Azure SQL Database                                                       | Cosmos DB NoSQL API                                                      | Cosmos DB MongoDB vCore                                                  |
|--------------------------------|----------------------------------------------------------------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------|---------------------------------------------------------------------------|
| Load initial data              | Server loads from JSON file on local disk<br>Server stores data in tables | Server loads from JSON file in Azure Blob Storage<br>Server stores data in tables | Client loads from JSON file on local disk (bulk insert)<br>Server stores data in documents | Client loads from JSON file on local disk (bulk insert)<br>Server stores data in documents |
| Vectorize initial data load    | Client retrieves data from server<br>Client calls OpenAI to vectorize data | Server calls OpenAI to vectorize data<br>Server stores vectors in tables    | Client retrieves data from server<br>Client calls OpenAI to vectorize data | Client retrieves data from server<br>Client calls OpenAI to vectorize data |
| Vectorize new/updated data     | Client calls OpenAI to vectorize new/updated data                     | Client sends new/updated data to server<br>Server calls OpenAI to vectorize data | Azure Function captures new/updated documents in Cosmos DB<br>Server stores vectors | Client calls OpenAI to vectorize new/updated data                         |
| Delete data                    | Server deletes data from tables<br>Server deletes vectors              | Server deletes data with vectors                                         | Server deletes data with vectors                                         | Server deletes data with vectors                                          |
| AI assistant                   | Client calls OpenAI to vectorize question<br>Server retrieves vectors  | Server calls OpenAI to vectorize question<br>Server retrieves vectors       | Client calls OpenAI to vectorize question<br>Server retrieves vectors      | Client calls OpenAI to vectorize question<br>Server retrieves vectors      |
