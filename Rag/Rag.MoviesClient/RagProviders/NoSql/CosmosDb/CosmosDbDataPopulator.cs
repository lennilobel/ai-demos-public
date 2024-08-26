using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbDataPopulator : IDataPopulator
    {
        public async Task LoadData()
        {
            Debugger.Break();

			ConsoleOutput.WriteHeading("Load Data", ConsoleColor.Yellow);
			
            var database = await this.DropAndCreateDatabase();
            var container = await this.CreateContainer(database);
            await this.CreateDocuments(@"Data\movies.json", container);
            await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));
        }

        private async Task<Database> DropAndCreateDatabase()
        {
            var databaseName = RagProviderFactory.GetDatabaseName();

            try
            {
                await Shared.CosmosClient.GetDatabase(databaseName).DeleteAsync();
				ConsoleOutput.WriteLine($"Deleted existing '{databaseName}' database");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

            await Shared.CosmosClient.CreateDatabaseAsync(databaseName);
            var database = Shared.CosmosClient.GetDatabase(databaseName);

            ConsoleOutput.WriteLine($"Created '{databaseName}' database");

            return database;
        }

        private async Task<Container> CreateContainer(Database database)
        {
            var containerName = Shared.AppConfig.CosmosDb.ContainerName;

            var containerProperties = new ContainerProperties
            {
                Id = containerName,
                PartitionKeyPath = "/type",
                VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
                    new Collection<Embedding>(
                    [
                        new Embedding
                        {
                            Path = "/vectors",                          // property path to generated vector array
                            DataType = VectorDataType.Float32,          // highest precision values
                            DistanceFunction = DistanceFunction.Cosine, // calculates the similarity between two vector arrays
                            Dimensions = 1536                           // vector array size (must match the embeddings model; start with 256, increase for greater accuracy)
                        }
                    ])
                ),
                IndexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths =
                    {
                        new IncludedPath { Path = "/*" }
                    },
                    ExcludedPaths =
                    {
                        new ExcludedPath { Path = "/_etag/?" },
                        new ExcludedPath { Path = "/vectors/*" },	// not strictly necessary; paths defined in the vector embedding policy are excluded automatically
					},
                    VectorIndexes =
                    [
                        new VectorIndexPath
                        {
                            Path = "/vectors",              // property path to generated vector array
                            Type = VectorIndexType.DiskANN  // use DiskANN (Disk-based Approximate Near Neighbor) algorithm
                        }
                    ]
                }
            };

            var containerThroughput = ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000);

            await database.CreateContainerAsync(containerProperties, containerThroughput);
            var container = database.GetContainer(containerName);

            ConsoleOutput.WriteLine($"Created '{containerName}' container");

            return container;
        }

		private async Task CreateDocuments(string jsonFilename, Container container)
        {
			ConsoleOutput.WriteLine($"Creating documents from {jsonFilename}");

			var started = DateTime.Now;
			
            var json = await File.ReadAllTextAsync(jsonFilename);
            var documents = JsonConvert.DeserializeObject<JArray>(json);
            var count = documents.Count;

            var cost = 0D;
            var errors = 0;

            var tasks = new List<Task>(count);
            foreach (JObject document in documents)
            {
                var task = container.CreateItemAsync(document, new PartitionKey("movie"));
                tasks.Add(task
                    .ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            cost += t.Result.RequestCharge;
                        }
                        else
                        {
							ConsoleOutput.WriteErrorLine($"Error creating document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}");
                            errors++;
                        }
                    }));
            }
            await Task.WhenAll(tasks);

			ConsoleOutput.WriteLine($"Created {count - errors} document(s) with {errors} error(s): {cost:0.##} RUs in {DateTime.Now.Subtract(started)}");
        }

		public async Task UpdateData()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Update Data", ConsoleColor.Yellow);

			var container = Shared.CosmosClient.GetContainer(
				RagProviderFactory.GetDatabaseName(),
				Shared.AppConfig.CosmosDb.ContainerName);

			await this.CreateDocuments(@"Data\movies-sw.json", container);      // Let the Azure Function wake to consume the change feed and vectorize the new documents
		}

		public async Task ResetData()
        {
            Debugger.Break();

			ConsoleOutput.WriteHeading("Reset Data", ConsoleColor.Yellow);
			
            var container = Shared.CosmosClient.GetContainer(
                RagProviderFactory.GetDatabaseName(),
                Shared.AppConfig.CosmosDb.ContainerName
            );

            var iterator = container.GetItemQueryIterator<string>(
                "SELECT VALUE c.id FROM c WHERE c.title IN ('Star Wars', 'The Empire Strikes Back', 'Return of the Jedi')");

            var ids = (await iterator.ReadNextAsync()).ToArray();
            foreach (var id in ids)
            {
                await container.DeleteItemAsync<object>(id, new PartitionKey("movie"));
				ConsoleOutput.WriteLine($"Deleted movie ID {id}");
            }
        }

    }
}
