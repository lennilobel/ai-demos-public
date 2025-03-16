using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Embeddings;
using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql.SqlServer
{
	public class SqlServer2022DataVectorizer : DataVectorizerBase
	{
		public SqlServer2022DataVectorizer(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task VectorizeEntities(int[] movieIds)
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
			ConsoleHelper.WriteLine($"Vectorizing {moviesArray.Length} movie(s)", ConsoleHelper.UserColor);

			const int BatchSize = 100;
			var itemCount = 0;

			for (var i = 0; i < moviesArray.Length; i += BatchSize)
			{
				var batchStarted = DateTime.Now;

				// Retrieve the next batch of documents
				var documents = moviesArray.Skip(i).Take(BatchSize).ToArray();
				foreach (var document in documents)
				{
					ConsoleHelper.WriteLine($"{++itemCount,5}: Vectorizing entity - {document[base.RagProvider.EntityTitleFieldName]} (ID {document["MovieId"]})", ConsoleHelper.InfoDimColor);
				}

				// Generate text embeddings (vectors) for the batch of documents
				var embeddings = await this.GenerateEmbeddings(documents);

				// Update the database with generated text embeddings (vectors) for the batch of documents
				await this.SaveVectors(documents, embeddings);

				var batchElapsed = DateTime.Now.Subtract(batchStarted);

				ConsoleHelper.WriteLine($"Processed rows {i + 1} - {i + documents.Length} in {batchElapsed}", ConsoleHelper.InfoColor);
			}

			ConsoleHelper.WriteLine($"Generated and embedded vectors for {itemCount} document(s)", ConsoleHelper.UserColor);
		}

		private async Task<OpenAIEmbedding[]> GenerateEmbeddings(JObject[] documents)
        {
            ConsoleHelper.Write($"Generating embeddings... ", ConsoleHelper.SystemColor);

			// Generate embeddings based on the textual content of each document
			var input = documents.Select(d => d.ToString()).ToArray();
			var embeddingClient = Shared.AzureOpenAIClient.GetEmbeddingClient(EmbeddingModelFactory.GetDeploymentName());
			var embeddings = (await embeddingClient.GenerateEmbeddingsAsync(input)).Value.ToArray();

			ConsoleHelper.WriteLine(embeddings.Length, ConsoleHelper.SystemColor);

			return embeddings;
		}

		private async Task SaveVectors(JObject[] documents, OpenAIEmbedding[] embeddings)
        {
			ConsoleHelper.WriteLine("Saving vectors", ConsoleHelper.SystemColor);

			var movieVectors = new DataTable();

            movieVectors.Columns.Add("MovieId", typeof(int));
            movieVectors.Columns.Add("VectorValueId", typeof(int));
            movieVectors.Columns.Add("VectorValue", typeof(float));

            for (var i = 0; i < documents.Length; i++)
            {
				var movieId = documents[i]["MovieId"].Value<int>();
				var vector = embeddings[i].ToFloats().ToArray();

                var vectorValueId = 1;
                foreach (var vectorValue in vector)
                {
                    movieVectors.Rows.Add(
					[
						movieId,
                        vectorValueId++,
                        vectorValue
                    ]);
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
