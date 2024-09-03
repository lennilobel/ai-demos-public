using Azure.AI.OpenAI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Sql.AzureSql
{
	public class SimpleVectorizeDemo
	{
		private class Movie
		{
			public string Title { get; set; }
			public float[] Vectors { get; set; }
		}

		private readonly Movie[] _movies =
			[
				new Movie { Title = "Return of the Jedi" },
				new Movie { Title = "The Godfather" },
				new Movie { Title = "Animal House" },
				new Movie { Title = "The Two Towers" },
			];

		private readonly string[] _queries =
			[
				// Movie phrases
				"May the force be with you",
				"I'm gonna make him an offer he can't refuse",
				"Toga party",
				"One ring to rule them all",
				null,
				// Movie characters
				"Luke Skywalker",
				"Don Corleone",
				"James Blutarsky",
				"Gandalf",
				null,
				// Movie actors
				"Mark Hamill",
				"Al Pacino",
				"John Belushi",
				"Elijah Wood",
				null,
				// Movie location references
				"Tatooine",
				"Sicily",
				"Faber College",
				"Mordor",
				null,
				// Movie genres
				"Science fiction",
				"Crime",
				"Comedy",
				"Fantasy/Adventure",
			];

		public async Task RunDemo()
		{
			Debugger.Break();

			ConsoleOutput.WriteHeading("Simple Vectorize Demo", ConsoleColor.Yellow);

			foreach (var movie in this._movies)
			{
				movie.Vectors = await this.VectorizeText(movie.Title);
			}

			foreach (var query in this._queries)
			{
				if (query == null)
				{
					Console.WriteLine();
					continue;
				}

				var queryVectors = await this.VectorizeText(query);
				var movie = this.RunVectorSearch(queryVectors);

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write($"{query,-50}");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write("matches ");
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(movie.Title);
				Console.ResetColor();
			}
		}

		private async Task<float[]> VectorizeText(string text)
		{
			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Shared.AppConfig.OpenAI.EmbeddingDeploymentNames.TextEmbedding3Large,
				input: [text]
			);

			var openAIEmbeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddings = openAIEmbeddings.Value.Data;
			var vectors = embeddings[0].Embedding.ToArray();

			return vectors;
		}

		private Movie RunVectorSearch(float[] queryVectors)
		{
			var result = this._movies
				.Select(m => new
				{
					Movie = m,
					CosineDistance = queryVectors
						.Zip(m.Vectors, (qv, mv) => qv * mv)
						.Sum()
				})
				.OrderByDescending(r => r.CosineDistance)
				.Select(r => r.Movie)
				.FirstOrDefault();

			return result;
		}

	}
}
