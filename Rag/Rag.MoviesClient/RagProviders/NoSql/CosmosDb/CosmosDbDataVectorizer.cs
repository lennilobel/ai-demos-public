using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbDataVectorizer : DataVectorizerBase
	{
        private static class Context
        {
            public static int ItemCount;
            public static int ErrorCount;
            public static double RuCost;
        }

		protected override async Task VectorizeMovies(int? movieId)
		{
			Debugger.Break();

			Context.ItemCount = 0;
            Context.ErrorCount = 0;
            Context.RuCost = 0;

            var database = Shared.CosmosClient.GetDatabase(Shared.AppConfig.CosmosDb.DatabaseName);
            var container = database.GetContainer(Shared.AppConfig.CosmosDb.ContainerName);

            // Raise the throughput on the container
            await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000));

            // Query documents in the container (process results in batches)
            var sql = $"SELECT * FROM c{(movieId == null ? null : $" WHERE c.movieId = {movieId}")}";
			var iterator = container.GetItemQueryIterator<JObject>(
                queryText: "SELECT * FROM c",
                requestOptions: new QueryRequestOptions { MaxItemCount = 100 });

            while (iterator.HasMoreResults)
            {
                var batchStarted = DateTime.Now;

                // Step 1 - Retrieve the next batch of documents
                base.ConsoleWrite("Retrieving documents... ", ConsoleColor.Green);
                var documents = (await iterator.ReadNextAsync()).ToArray();
                base.ConsoleWriteLine(documents.Length.ToString(), ConsoleColor.Green);
                Context.ItemCount += documents.Length;

                // Step 2 - Generate text embeddings (vectors) for the batch of documents
                var embeddings = await this.GenerateEmbeddings(documents);

                // Step 3 = Update the documents back to the container with generated text embeddings (vectors)
                await this.SaveVectors(container, documents, embeddings);

                var batchElapsed = DateTime.Now.Subtract(batchStarted);

                base.ConsoleWriteLine($"Processed documents {Context.ItemCount - documents.Length + 1} - {Context.ItemCount} in {batchElapsed}", ConsoleColor.Cyan);
            }

            // Lower the throughput on the container
            await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));

            base.ConsoleWriteLine($"Generated and embedded vectors for {Context.ItemCount} document(s) with {Context.ErrorCount} error(s) ({Context.RuCost} RUs)", ConsoleColor.Yellow);
        }

        private async Task<IReadOnlyList<EmbeddingItem>> GenerateEmbeddings(JObject[] documents)
        {
            base.ConsoleWrite("Generating embeddings... ", ConsoleColor.Green);

            // Strip meaningless properties and any previous vectors from each document
            foreach (var document in documents)
            {
                document.Remove("_rid");
                document.Remove("_self");
                document.Remove("_etag");
                document.Remove("_attachments");
                document.Remove("_ts");
                document.Remove("ttl");
                document.Remove("vectors");
            }

            // Generate embeddings based on the JSON string content of each document
            var embeddingsOptions = new EmbeddingsOptions(
                deploymentName: Shared.AppConfig.OpenAI.EmbeddingsDeploymentName,
                input: documents.Select(d => d.ToString()));

            var openAIEmbeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
            var embeddings = openAIEmbeddings.Value.Data;

            base.ConsoleWriteLine(embeddings.Count, ConsoleColor.Green);

            return embeddings;
        }

        private async Task SaveVectors(Container container, JObject[] documents, IReadOnlyList<EmbeddingItem> embeddings)
        {
            base.ConsoleWrite("Saving vectors... ", ConsoleColor.Green);

            // Set the vectors property of each document from the generated embeddings
            for (var i = 0; i < documents.Length; i++)
            {
                var embeddingsArray = embeddings[i].Embedding.ToArray();
                var vectors = JArray.FromObject(embeddingsArray);
                documents[i]["vectors"] = vectors;
            }

            // Use bulk execution to update the documents back to the container
            var tasks = new List<Task>(documents.Length);
            foreach (JObject document in documents)
            {
                var task = container.ReplaceItemAsync(document, document["id"].ToString(), new PartitionKey("movie"));
                tasks.Add(task
                    .ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            Context.RuCost += t.Result.RequestCharge;
                        }
                        else
                        {
                            base.ConsoleWriteLine($"Error replacing document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}", ConsoleColor.Red);
                            Context.ErrorCount++;
                        }
                    }));
            }

            await Task.WhenAll(tasks);

            base.ConsoleWriteLine(documents.Length, ConsoleColor.Green);
        }

    }
}
