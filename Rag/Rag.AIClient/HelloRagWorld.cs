using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Rag.AIClient.Engine;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rag.AIClient
{
	public class HelloRagWorld
	{
		public async Task RunDemo()
		{
			Debugger.Break();

			await TextEmbeddingsDemo();
			await CompletionsDemo();
		}

		#region Text embeddings demo

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


		private async Task TextEmbeddingsDemo()
		{
			ConsoleOutput.WriteHeading("Text Embeddings", ConsoleColor.Cyan);

			// Vectorize the movies
			foreach (var movie in this._movies)
			{
				movie.Vectors = await this.VectorizeText(movie.Title);
			}

			// Vectorize each query and run a vector search against the movies
			foreach (var query in this._queries)
			{
				if (query == null)
				{
					Console.WriteLine();
					continue;
				}

				var queryVectors = await this.VectorizeText(query);
				var movie = this.RunVectorSearch(queryVectors);

				Console.ForegroundColor = ConsoleColor.Yellow; Console.Write($"{query,-50}");
				Console.ForegroundColor = ConsoleColor.White; Console.Write("matches ");
				Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(movie.Title);
				Console.ResetColor();
			}
		}

		private async Task<float[]> VectorizeText(string text)
		{
			// Prepare an OpenAI client
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			var openAIClient = new OpenAIClient(
				new Uri(config["AppConfig:OpenAI:Endpoint"]),
				new AzureKeyCredential(config["AppConfig:OpenAI:ApiKey"])
			);

			// Create options for the OpenAI Embeddings API, specifying the deployment model and input text
			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: "lenni-text-embedding-3-small",
				input: [text]
			);

			// Call OpenAI API to get the embeddings for the input text
			var openAIEmbeddings = await openAIClient.GetEmbeddingsAsync(embeddingsOptions);

			// Extract the embedding data (the vector representation of the text) from the API response
			var embeddings = openAIEmbeddings.Value.Data;

			// Convert the first embedding from the response to an array of float values (the vector)
			var vectors = embeddings[0].Embedding.ToArray();

			// Return the vectorized representation of the input text
			return vectors;
		}

		private Movie RunVectorSearch(float[] queryVectors)
		{
			// Perform a vector search by comparing the query vector against each movie's vector
			var result = this._movies
				.Select(m => new
				{
					Movie = m,									// The current movie object
					CosineDistance = queryVectors
						.Zip(m.Vectors, (qv, mv) => qv * mv)	// Pair up the query vector with the movie's vector and compute the product
						.Sum()									// Sum the product to calculate a form of similarity (cosine similarity)
				})
				.OrderByDescending(r => r.CosineDistance)		// Sort movies by cosine similarity in descending order
				.Select(r => r.Movie)							// Select the movie with the highest similarity score
				.FirstOrDefault();								// Return the first (most similar) movie, or null if no movie is found

			// Return the movie that best matches the query vector
			return result;
		}

		#endregion

		#region Completions demo

		private async Task CompletionsDemo()
		{
			ConsoleOutput.WriteHeading("Completions", ConsoleColor.Cyan);

			// Load configuration settings from appsettings.json
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			// Create an OpenAI client using the endpoint and API key from configuration
			var openAIClient = new OpenAIClient(
				new Uri(config["AppConfig:OpenAI:Endpoint"]),
				new AzureKeyCredential(config["AppConfig:OpenAI:ApiKey"])
			);

			// Define options for the chat completions, including parameters like max tokens, temperature, and penalties
			var completionOptions = new ChatCompletionsOptions
			{
				MaxTokens = 1000,                   // Max number of tokens for the response; the more tokens you specify (spend), the more verbose the response
				Temperature = 1.0f,                 // Range is 0.0 to 2.0; controls "apparent creativity"; higher = more random, lower = more deterministic
				FrequencyPenalty = 0.0f,            // Range is -2.0 to 2.0; controls likelihood of repeating words; higher = less likely, lower = more likely
				PresencePenalty = 0.0f,				// Range is -2.0 to 2.0; controls likelihood of introducing new topics; higher = more likely, lower = less likely
				NucleusSamplingFactor = 0.95f,      // Range is 0.0 to 2.0; aka "Top P sampling"; temperature alternative; controls diversity of responses (1.0 is full random, lower values limit randomness)
				DeploymentName = "lenni-gpt-4o",	// The deployment name to specify which completions model to use
			};

			// Interact with the model by sending various prompts and displaying the answers
			await this.AskAndAnswer(openAIClient, completionOptions, "Provide a summary of these movies: Return of the Jedi, The Godfather, Animal House, The Two Towers.");
			await this.AskAndAnswer(openAIClient, completionOptions, "Only show the movie titles.");
			await this.AskAndAnswer(openAIClient, completionOptions, "Go back to showing full descriptions. Also, include additional movies that I might like.");
			await this.AskAndAnswer(openAIClient, completionOptions, "Go back to showing a summary of just the movies I asked about, with no additional recommendations, translated to Dutch.");
		}

		private async Task AskAndAnswer(OpenAIClient openAIClient, ChatCompletionsOptions completionOptions, string question)
		{
			// Display the question
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(question);

			// Add the question as a message to the completion options
			completionOptions.Messages.Add(new ChatRequestUserMessage(question));

			// Get the chat completion response from OpenAI based on the provided question and options
			var completions = await openAIClient.GetChatCompletionsAsync(completionOptions);

			// Extract the answer from the first choice in the response
			var answer = completions.Value.Choices[0].Message.Content;

			// Display the answer
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(answer);
			Console.WriteLine();
			Console.ResetColor();
		}

		#endregion
	}

}
