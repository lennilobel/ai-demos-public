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

| Unnamed: 0                  | SQL Server (on-prem)                               | Azure SQL Database                                    | Cosmos DB NoSQL API                                                | Cosmos DB MongoDB vCore                                 |
|:----------------------------|:---------------------------------------------------|:------------------------------------------------------|:-------------------------------------------------------------------|:--------------------------------------------------------|
| Load initial data           | Server loads from JSON file on local disk.         | Server loads from JSON file in Azure Blob Storage.    | Client loads from JSON file on local disk (bulk execution).        | Client loads from JSON file on local disk (bulk write), |
|                             | Server shreds JSON to normalized tables.           | Server shreds JSON to normalized tables.              | Client saves individual JSON documents to server (bulk execution). | Client saves individual JSON documents to server,       |
| Vectorize initial data load | Client retrieves data from server.                 | Server calls OpenAI to vectorize data.                | Client retrieves data from server.                                 | Client retrieves data from server.                      |
|                             | Client calls OpenAI to vectorize data.             | Server stores vectors in native data type.            | Client calls OpenAI to vectorize data (bulk batching).             | Client calls OpenAI to vectorize data (bulk batching).  |
|                             | Client updates vectors to server.                  | (19 min)                                              | Client updates vectors to server (bulk execution).                 | Client updates vectors to server (bulk write).          |
|                             | Server stores vectors in columnstore index.        |                                                       |                                                                    |                                                         |
| Vectorize new/updated data  | Client calls OpenAI to vectorize new/updated data. | Client sends new/updated data to server.              | Azure Function captures new/updated documents via change feed.     | Client calls OpenAI to vectorize new/updated data.      |
|                             | Client saves new/updated data to server.           | Server calls OpenAI to vectorize new/updated data.    | Azure Function calls OpenAI to vectorize new/updated data.         | Client saves new/updated data to server.                |
|                             | Client saves new/updated vectors to server.        | Server updates vectors with native data type.         | Azure Function updates vectors to server.                          | Client saves new/updated vectors to server.             |
| Delete data                 | Server deletes data from tables.                   | Server deletes data with vectors.                     | Server deletes data with vectors.                                  | Server deletes data with vectors.                       |
|                             | Server deletes vectors from columnstore.           |                                                       |                                                                    |                                                         |
| AI assistant                | Client calls OpenAI to vectorize question.         | Server calls OpenAI to vectorize question.            | Client calls OpenAI to vectorize question.                         | Client calls OpenAI to vectorize question.              |
|                             | Server runs classic vector query.                  | Server run native vector query  (new T-SQL function). | Server runs native vector query (VectorDistance function).         | Server runs native vector query ($search stage).        |
