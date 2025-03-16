using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbDataPopulator : DataPopulatorBase
	{
		public CosmosDbDataPopulator(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public override async Task InitializeData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Load Data", ConsoleHelper.UserColor);

			var database = await this.DropAndCreateDatabase();
			var container = await this.CreateContainer(database);
			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.CosmosDbConfig.JsonInitialDataFilename);

			await this.CreateDocuments(filename, container);
			await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));
		}

		private async Task<Database> DropAndCreateDatabase()
		{
			var databaseName = base.RagProvider.DatabaseName;

			try
			{
				await Shared.CosmosClient.GetDatabase(databaseName).DeleteAsync();
				ConsoleHelper.WriteLine($"Deleted existing '{databaseName}' database");
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

			await Shared.CosmosClient.CreateDatabaseAsync(databaseName);
			var database = Shared.CosmosClient.GetDatabase(databaseName);

			ConsoleHelper.WriteLine($"Created '{databaseName}' database");

			return database;
		}

		private async Task<Container> CreateContainer(Database database)
		{
			var containerName = base.RagProvider.CosmosDbConfig.ContainerName;
			var partitionKeyPath = $"/{base.RagProvider.CosmosDbConfig.PartitionKey}";

			var containerProperties = new ContainerProperties
			{
				Id = containerName,
				PartitionKeyPath = partitionKeyPath,
				VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
					new Collection<Embedding>(
					[
						new Embedding
						{
							Path = "/vector",							// property path to vector array of floating point values
							DataType = VectorDataType.Float32,          // highest precision vector values
							DistanceFunction = DistanceFunction.Cosine, // calculates the cosine distance metric between two vector arrays
							Dimensions = 3072                           // vector array size (must match the actual dimensions stored in the property, or the vector index will be bypassed)
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
						new ExcludedPath { Path = "/vector/*" },	// not strictly necessary; paths defined in the vector embedding policy are excluded automatically
					},
					VectorIndexes =
					[
						new VectorIndexPath
						{
							Path = "/vector",				// property path to generated vector array
							Type = VectorIndexType.DiskANN  // use DiskANN (Disk-based Approximate Near Neighbor) index type
						}
					]
				}
			};

			var containerThroughput = ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000);

			await database.CreateContainerAsync(containerProperties, containerThroughput);
			var container = database.GetContainer(containerName);

			ConsoleHelper.WriteLine($"Created '{containerName}' container");

			return container;
		}

		private async Task CreateDocuments(string jsonFilename, Container container)
		{
			ConsoleHelper.WriteLine($"Creating documents from '{jsonFilename}'");

			var started = DateTime.Now;

			var json = await File.ReadAllTextAsync(jsonFilename);
			var documents = JsonConvert.DeserializeObject<JArray>(json);
			var count = documents.Count;

			var cost = 0D;
			var errors = 0;

			var tasks = new List<Task>(count);
			foreach (JObject document in documents)
			{
				var task = container.CreateItemAsync(document, new PartitionKey(document[base.RagProvider.CosmosDbConfig.PartitionKey].Value<string>()));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status == TaskStatus.RanToCompletion)
						{
							cost += t.Result.RequestCharge;
						}
						else
						{
							ConsoleHelper.WriteErrorLine($"Error creating document id='{document["id"]}'\n{t.Exception.Message}");
							errors++;
						}
					}));
			}
			await Task.WhenAll(tasks);

			ConsoleHelper.WriteLine($"Created {count - errors} document(s) with {errors} error(s): {cost:0.##} RUs in {DateTime.Now.Subtract(started)}");
		}

		public override async Task UpdateData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Update Data", ConsoleHelper.UserColor);

			var container = Shared.CosmosClient.GetContainer(
				base.RagProvider.DatabaseName,
				base.RagProvider.CosmosDbConfig.ContainerName);

			var filename = base.RagProvider.GetDataFilePath(base.RagProvider.CosmosDbConfig.JsonUpdateDataFilename);

			await this.CreateDocuments(filename, container);      // Let the Azure Function wake to consume the change feed and vectorize the new documents
		}

		public override async Task ResetData()
		{
			Debugger.Break();

			ConsoleHelper.WriteHeading("Reset Data", ConsoleHelper.UserColor);

			var container = Shared.CosmosClient.GetContainer(
				base.RagProvider.DatabaseName,
				base.RagProvider.CosmosDbConfig.ContainerName
			);

			var iterator = container.GetItemQueryIterator<string>(
				"SELECT VALUE c.id FROM c WHERE c.title IN ('Star Wars', 'The Empire Strikes Back', 'Return of the Jedi')");

			var ids = (await iterator.ReadNextAsync()).ToArray();
			foreach (var id in ids)
			{
				await container.DeleteItemAsync<object>(id, new PartitionKey("movie"));
				ConsoleHelper.WriteLine($"Deleted movie ID {id}");
			}
		}

	}
}
