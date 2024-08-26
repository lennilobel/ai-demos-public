using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.EmbeddingModels;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql.SqlServer
{
	public class SqlServerDataVectorizer : DataVectorizerBase
	{
		protected override async Task VectorizeMovies(int[] movieIds)
		{
			Debugger.Break();

			var moviesJson = default(string);
			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "GetMoviesJson",
				storedProcedureParameters:
				[
					("@MovieIdsCsv", movieIds == null ? null : string.Join(',', movieIds))
				],
				getResult: rdr => moviesJson = rdr.GetString(0)
			);

			var moviesArray = JsonConvert.DeserializeObject<JObject[]>(moviesJson);
			ConsoleOutput.WriteLine($"Vectorizing {moviesArray.Length} movie(s)", ConsoleColor.Yellow);

			const int BatchSize = 100;
			for (var i = 0; i < moviesArray.Length; i += BatchSize)
			{
				var batchStarted = DateTime.Now;

				// Get next batch of movie documents
				var documents = moviesArray.Skip(i).Take(BatchSize).ToArray();

				// Generate text embeddings (vectors) for the batch of documents
				var embeddings = await this.GenerateEmbeddings(documents);

				// Update the database with generated text embeddings (vectors) for the batch of documents
				await this.SaveVectors(documents, embeddings);

				var batchElapsed = DateTime.Now.Subtract(batchStarted);

				ConsoleOutput.WriteLine($"Processed rows {i + 1} - {i + documents.Length} in {batchElapsed}", ConsoleColor.Cyan);
			}
		}

		private async Task<IReadOnlyList<EmbeddingItem>> GenerateEmbeddings(JObject[] documents)
        {
            ConsoleOutput.Write($"Generating embeddings... ", ConsoleColor.Green);

            // Generate embeddings based on the textual content of each document
            var embeddingsOptions = new EmbeddingsOptions(
                deploymentName: EmbeddingModelFactory.GetDeploymentName(),
                input: documents.Select(d => d.ToString()).ToArray()
			);

            var openAIEmbeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
            var embeddings = openAIEmbeddings.Value.Data;

			ConsoleOutput.WriteLine(embeddings.Count, ConsoleColor.Green);
			
            return embeddings;
        }

        private async Task SaveVectors(JObject[] documents, IReadOnlyList<EmbeddingItem> embeddings)
        {
			ConsoleOutput.WriteLine("Saving vectors", ConsoleColor.Green);

			var movieVectors = new DataTable();

            movieVectors.Columns.Add("MovieId", typeof(int));
            movieVectors.Columns.Add("VectorValueId", typeof(int));
            movieVectors.Columns.Add("VectorValue", typeof(float));

            for (var i = 0; i < documents.Length; i++)
            {
                var movieId = documents[i]["MovieId"].Value<int>();
                var vectors = embeddings[i].Embedding;

                var vectorValueId = 1;
                foreach (var vectorValue in vectors.ToArray())
                {
                    movieVectors.Rows.Add(new object[]
                    {
                        movieId,
                        vectorValueId++,
                        vectorValue
                    });
                }
            }

			await SqlDataAccess.RunStoredProcedure(
                storedProcedureName: "CreateMovieVectors",
                storedProcedureParameters:
				[
					("@MovieVectors", movieVectors)
				]
			);
		}

	}
}
