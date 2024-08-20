# RAG Solution

This is a provider-based RAG solution that leverages Azure OpenAI and supports the following back-end database platforms:
- SQL Server 2022
- Azure SQL Database
- Azure Cosmos DB for NoSQL
- Azure Cosmos DB for MongoDB vCore

## Movies Database Scenario

The scenario is a transactional database of movies, with a RAG pattern that leverages Azure OpenAI to return natural language responses from natural language queries for movie recommendations.

The solution demonstrates:
- Loading the movies database with initial data
- Vectorizing the initial data load
- Vectorizing incremental data changes
- Vectorizing natural language questions for movie recommendations
- Running a vector search in the database to retrieve similarity results
- Delivering a natural language response from the similarity results
- Generating a movies poster image based on the similarity results

Common functionality is implemented in a set of shared base classes, while individual providers implement the functionality that is unique to each database platform:

**SQL Server 2022**

- SQL Server loads the initial data by shredding a JSON file from a local path into relational tables.
- The client application calls OpenAI to vectorize movies in the database and natural language questions posed by users.
- Movie vectors are efficiently stored and indexed in a traditional columnstore index table related to the Movie table.
- Vector searching is performed using a simple cosine distance algorithm.
- The client application is responsible for coordinating vectorization with new movies as they are added/updated.

**Azure SQL Database**

- Azure SQL Database loads the initial data by shredding a JSON file from Azure Blob Storage into relational tables.
- Azure SQL Database calls OpenAI (via sp_invoke_external_rest_endpoint) to vectorize movies in the database and natural language questions posed by users.
- Movie vectors are efficiently stored and indexed in a native vector data type column in the Movie table (TBD).
- Vector searching is performed using the native T-SQL vector distance function (TBD).
- The client application is responsible for coordinating vectorization with new movies as they are added/updated.

**Azure Cosmos DB for NoSQL**

- The client application uses bulk execution to populate a container of movie documents from a local JSON file.
- The client application calls OpenAI to vectorize movies in the container and natural language questions posed by users.
- Movie vectors are stored in a vector array property in each movie document, and efficiently indexed using the DiskANN (Disk-based Approximate Nearest Neighbor) algorithm.
- Vector searching is performed using the native VectorDistance function in a SQL query.
- An Azure Function monitors the container's change feed and calls OpenAI to vectorize movies as they are added/updated.

**Azure Cosmos DB for MongoDB vCore**

- The Azure Cosmos DB for MongoDB vCore provider is not yet implemented.

## Environment Setup

To setup your environment, first configure Azure OpenAI as explained in the next section below. Then follow the instructions in the remaining sections for each of the database platforms you wish to use.

### Azure OpenAI

Regardless of which database platform you're using, you'll need to configure an Azure OpenAI resource with three model deployments.

**Configure Azure OpenAI**
- Use the Azure portal to create a new Azure OpenAI resource.
- Use Azure OpenAI Studio to create three model deployments:
  - text-embeddings-3-large
  - gpt-4o
  - dall-e-3

**Update the application configuration file**
- Open the **AIDemos** solution in Visual Studio.
- Expand the **Rag** folder.
- Expand the **Rag.MoviesClient** project.
- Open the **appsettings.json** file.
- In the **OpenAI** section, supply the endpoint and API key of your Azure resource, as well as the names you assigned to the three model deployments:
  ```json
  "OpenAI": {
    "Endpoint": "[ENDPOINT]",
    "ApiKey": "[API-KEY]",
    "EmbeddingsDeploymentName": "[NAME-OF-YOUR-EMBEDDINGS-MODEL]",
    "CompletionsDeploymentName": "[NAME-OF-YOUR-COMPLETIONS-MODEL]",
    "DalleDeploymentName": "[NAME-OF-YOUR-DALL-E-MODEL]"
  }
  ```

### SQL Server 2022

To use the RAG solution with SQL Server 2022, you'll need to first initialize the sample database and update the application configuration file accordingly.

**Initialize the sample database**
- Use SSMS to create a new database named **RagDemo**.
- Open the **AIDemos** solution in Visual Studio.
- Expand the **Rag** folder.
- Expand the **Rag.MoviesDatabase.SqlServer** project.
- Open the **ProjectToSqlServer.scmp** schema compare file.
- In the target dropdown, choose **Select Target...**.
- Connect to the new **RagDemo** database.
- Click **Compare**.
- Click **Update**.

**Update the application configuration file**
- Expand the **Rag.MoviesClient** project.
- Open the **appsettings.json** file.
- In the **SqlServer** section, supply the server name, username, and password for connecting to the database:
  ```json
    "SqlServer": {
      "ServerName": "[SERVER-NAME]",
      "DatabaseName": "RagDemo",
      "Username": "[USERNAME]",
      "Password": "[PASSWORD]",
      "TrustServerCertificate": true
    }
    ```

### Azure SQL Database

To use the RAG solution with Azure SQL Database, you'll need to first save the JSON source files to Azure Blob Storage, initialize the sample database, and update the application configuration file accordingly.

**Save JSON files to Azure Blob Storage**
- Under **Rag\Rag.MoviesClient\Data**, copy the two **.json** files to an Azure Blob Storage container.
  - `movies.json`
  - `movies-sw.json`
- Expand the **BlobStorage** folder in the **Rag.MoviesDatabase.AzureSql** project.
- Edit **BlobStorageCredential.sql** to supply a database server master key password, and the secret (SAS token) to access the Azure Blob Storage container.
  ```tsql
  CREATE MASTER KEY ENCRYPTION BY PASSWORD = '[PASSWORD]'
  GO
  
  CREATE DATABASE SCOPED CREDENTIAL BlobStorageCredential
    WITH IDENTITY = 'SHARED ACCESS SIGNATURE',
    SECRET = '[SAS-TOKEN]'   -- SAS token for Blob, Object, Read access
  GO
  ```
- Edit **BlobStorageDataSource.sql** to supply the storage account name and container name.
  ```tsql
  CREATE EXTERNAL DATA SOURCE BlobStorageContainer
  WITH (
    TYPE = BLOB_STORAGE,
    LOCATION = 'https://[STORAGE-ACCOUNT-NAME].blob.core.windows.net/[CONTAINER-NAME]',
    CREDENTIAL = BlobStorageCredential
  )
  ```

**Initialize the sample database**
- Use the Azure portal to create a new database named **RagDemo**.
- Open the **AIDemos** solution in Visual Studio.
- Expand the **Rag** folder.
- Expand the **Rag.MoviesDatabase.AzureSql** project.
- Open the **ProjectToAzureSql.scmp** schema compare file.
- In the target dropdown, choose **Select Target...**.
- Connect to the new **RagDemo** database.
- Click **Compare**.
- Click **Update**.

**Update the application configuration file**
- Expand the **Rag.MoviesClient** project.
- Open the **appsettings.json** file.
- In the **AzureSql** section, supply the server name, username and password for connecting to the database:
  ```json
  "AzureSql": {
    "ServerName": "[SERVER-NAME].database.windows.net",
    "DatabaseName": "RagDemo",
    "Username": "[USERNAME]",
    "Password": "[PASSWORD]"
  }
  ```

### Azure Cosmos DB for NoSQL

To use the RAG solution with Azure Cosmos DB for NoSQL, you'll need to first update the application configuration file for your existing Cosmos DB account.

**Update the application configuration file**
- Expand the **Rag.MoviesClient** project.
- Open the **appsettings.json** file.
- In the **CosmosDb** section, supply the endpoint and master key for connecting to your Azure Cosmos DB for NoSQL account:
  ```json
  "CosmosDb": {
    "Endpoint": "[ENDPOINT]",
    "MasterKey": "[MASTER-KEY]",
    "DatabaseName": "rag-demo",
    "ContainerName": "movies"
  }
  ```

**Update the Azure Function configuration file**
- Expand the **Rag.MoviesFunctions.CosmosDb** project.
- Open the **localsettings.json** file.
- In the **Values** section, supply the connection string to your Azure Cosmos DB for NoSQL account, the endpoint and API key for your Azure OpenAI resource, and the name your embeddings model deployed to Azure OpenAI:
  ```json
  {
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "CosmosDbConnectionString": "[CONNECTION-STRING]",
        "OpenAIEndpoint": "[ENDPOINT]",
        "OpenAIKey": "[API-KEY]",
        "OpenAIDeploymentName": "[NAME-OF-YOUR-EMBEDDINGS-MODEL]"
    }
  }
  ```

## Running the Demo Solution

### Start the Movies Client

- Run the `Rag.MoviesClient` project.
  - All the primary functionality is contained in this one console application.

- Select the RAG provider for your database platform.
  - Type the "change edition" command `CE [provider]`, replacing [provider] with:
    - `SqlServer` (SQL Server 2022)
    - `AzureSql` (Azure SQL Database)
    - `CosmosDb` (Azure Cosmos DB for NoSQL)
    - `MongoDb` (Azure Cosmos DB for MongoDB vCore)
  - The provider is set to `SqlServer` by default, but you can select any provider as the default to any provider by changing the `RagProvider` property in `appsettings.json`.
  
### Load the Initial Data

- Type the "load data" command `LD`.
  - This will delete all existing data in the database, and populate the database with all the movies in `movies.json`.

- Test out searching before vectorization.
  - Type the "movies assistant" command `MA`.
  - Try asking any question, and observe that the response explains that there are no results.

### Vectorize the Initial Data

- Type the "reset data" command `RD`.
  - This removes the three original Star Wars trilogy movies from the database.
  - You will re-add them and vectorize them in a later step.
  
- Type the "vectorize data" command `VD`.
  - This will vectorize all the movies (except for the three deleted Star Wars movies).
  - The process typically takes approximately 15-25 minutes.

### Get Movie Recommendations

- Type the "movies assistant" command `MA`.
  - Press the `A` key to trigger the first automatic question about sci-fi movies.
  - Press `A` again to ask the next question about Star Wars in particular.
  - Press `A` once more to ask specifically about the original Star Wars trilogy.
- Observe how the last response explains that those movies are not in the database.
- Press `Esc` to exit the movies assistant and return to the menu.

### Incrementally Add and Vectorize New Data

- If you're using Azure Cosmos DB for NoSQL
  - Start the `Rag.MoviesFunction.CosmosDb` project.
  - This runs an Azure Function (in your local environment) that listens on the container's change feed for new/updated documents to be (re-)vectorized.

- Type the "update data" command `UD`.
  - This loads the three original Star Wars trilogy movies from `movies-sw.json` into the database.
  - If you're using Azure Cosmos DB for NoSQL, observe that the Azure Function wakes up and automatically vectorizes the new movie documents.
  - Providers for the other database platforms explicitly vectorize the new movies after loading them from the JSON file.

- Type the "movies assistant" command `MA`.
  - Press the `A` key to trigger the first automatic question about sci-fi movies.
  - Press `A` again to ask the next question about Star Wars in particular.
  - Press `A` once more to ask specifically about the original Star Wars trilogy.
- Observe how the last response provides recommendations for the trilogy.

### Experiment with Different Prompts

- Press `Esc` to exit the movies assistant and return to the menu.
- Type the "update configuration" command `UC`.
- Change the **ShowInternalOperations** property to **true**.
  - This causes the movies assistant to display the prompts and other behind-the-scenes information, as it processes each question.
- Experiment with different values for these other properties to change the behavior of the movies assistant:
  - **Demeanor** - sets the language tone of the AI responses
  - **IncludeDetails** - specifies details (for example, "genre, language, run-time") to be included with each recommendation (in addition to title, year, and overview).
  - **NoEmojis** - if true, prompts the assistant not to include emoji characters in the response (which do not render properly in console applications).
  - **NoMarkdown** - if true, prompts the assistant not to format the response with markdown characters (which do not get processed for rendering in console applications).
  - **GeneratePosterImage** - if true, generates a poster image based on the movie recommendations provided for each question.
