using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Base
{
	public abstract class MoviesAssistantBase : RagProviderBase, IMoviesAssistant
	{
		// UX behavior
		protected readonly bool _showInternalOperations = false;     // Display internal operations (completion messages, vector search)
		private readonly bool _streamOutput = true;                  // Stream output to simulate reading and writing
		private readonly bool _interactive = true;                   // Wait for the user to press Enter for each question

		// AI behavior
		private readonly string _includeDetails = "genre";           // Be specific about what movie info to be included in the response
		private readonly bool _noEmojis = false;                     // Don't include emojies in the response
		private readonly bool _noMarkdown = false;                   // Don't format markdown in the response
		private readonly bool _generatePosterImage = false;          // Generate a movie poster based on the response (DALL-E)
		private readonly string _demeanor = "upbeat and friendly";   // Set the language tone of the AI responses
		private readonly string _responseLanguage = "English";       // Translate the natural language response to any other language

		// Timings
		protected TimeSpan _elapsedVectorizeQuestion;
		protected TimeSpan _elapsedRunVectorSearch;
		private TimeSpan _elapsedGenerateAnswer;
		private TimeSpan _elapsedGeneratePoster;

		private int _currentQuestionIndex;

		// List your natural language movie questions here...
		private string[] Questions = [
			"Please recommend some good sci-fi movies.",
			"What about Star Wars?",
			"Actually, I'm looking for the original Star Wars trilogy.",
			"Do you know any good mobster movies?",
			"Do you know any movies produced by Pixar?",
			"Can you recommend movies in Italian?",
			"Actually, I meant just comedies in that language.",
			"Can you recommend movies made before the year 2000?",
			"I love horror flicks.",
		];

		public async Task RunMoviesAssistant()
		{
			Debugger.Break();

			this.SayHello();

			var completionsOptions = this.InitializeCompletionOptions();

			this.SetChatPrompt(completionsOptions);

			this._currentQuestionIndex = 0;
			while (true)
			{
				var question = this.GetQuestion();

				if (question == null)
				{
					break;
				}

				await this.ProcessQuestion(question, completionsOptions);
			}
		}

		private void SayHello()
		{
			Console.OutputEncoding = Encoding.UTF8;
			Console.Clear();

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(@"  __  __            _                  _            _     _              _   ");
			Console.WriteLine(@" |  \/  | _____   _(_) ___  ___       / \   ___ ___(_)___| |_ __ _ _ __ | |_ ");
			Console.WriteLine(@" | |\/| |/ _ \ \ / / |/ _ \/ __|     / _ \ / __/ __| / __| __/ _` | '_ \| __|");
			Console.WriteLine(@" | |  | | (_) \ V /| |  __/\__ \    / ___ \\__ \__ \ \__ \ || (_| | | | | |_ ");
			Console.WriteLine(@" |_|  |_|\___/ \_/ |_|\___||___/   /_/   \_\___/___/_|___/\__\__,_|_| |_|\__|");
			Console.WriteLine();
			Console.WriteLine(@$"   {RagProviderFactory.GetProviderName()} Edition");
			Console.WriteLine();
			Console.ResetColor();
		}

		private ChatCompletionsOptions InitializeCompletionOptions() =>
			new()
			{
				MaxTokens = 1000,                   // The more tokens you specify (spend), the more verbose the response
				Temperature = 1.0f,                 // Range is 0.0 to 2.0; controls "apparent creativity"; higher = more random, lower = more deterministic
				FrequencyPenalty = 0.0f,            // Range is -2.0 to 2.0; controls likelihood of repeating words; higher = less likely, lower = more likely
				PresencePenalty = 0.0f,             // Range is -2.0 to 2.0; controls likelihood of introducing new topics; higher = more likely, lower = less likely
				NucleusSamplingFactor = 0.95f,      // Range is 0.0 to 2.0; aka "Top P sampling"; temperature alternative
				DeploymentName =                    // GPT model
					Shared.AppConfig.OpenAI.CompletionsDeploymentName,
			};

		private void SetChatPrompt(ChatCompletionsOptions completionsOptions)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"You are a movies enthusiast who helps people discover films that they would enjoy watching.");
			sb.AppendLine($"Your demeanor is {_demeanor}.");
			sb.AppendLine($"Only include the following details of each movie recommendation: title, year, overview, {_includeDetails}.");

			if (this._noEmojis)
			{
				sb.AppendLine("Don't include emojis, because they won't render in my demo console application.");
			}

			if (this._noMarkdown)
			{
				sb.AppendLine("Don't include markdown syntax, because it won't render in my demo console application.");
			}

			if (this._responseLanguage != "English")
			{
				sb.AppendLine($"Translate your recommendations in {this._responseLanguage}; don't include the recommendations in English. ");
			}

			var prompt = sb.ToString();

			this.ConsoleWritePromptMessage(prompt);

			completionsOptions.Messages.Add(new ChatRequestSystemMessage(prompt));
		}

		private string GetQuestion()
		{
			base.ConsoleWriteHeading("USER QUESTION", ConsoleColor.Yellow);
			base.ConsoleWriteLine("[A] = Auto / [M] = Manual / [ESC] = Quit: ", suppressLineFeed: true);

			if (this._interactive)
			{
				var key = default(ConsoleKey);
				while (true)
				{
					key = Console.ReadKey(intercept: true).Key;
					if (key == ConsoleKey.A || key == ConsoleKey.M || key == ConsoleKey.Escape)
					{
						break;
					}
				}

				this.ConsoleClearLine();
				Console.SetCursorPosition(0, Console.GetCursorPosition().Top);

				if (key == ConsoleKey.Escape)
				{
					return null;
				}

				if (key == ConsoleKey.M)
				{
					while (true)
					{
						base.ConsoleWriteLine("> ", ConsoleColor.Yellow, suppressLineFeed: true);
						Console.ForegroundColor = ConsoleColor.Yellow;
						var question = Console.ReadLine();
						if (!string.IsNullOrWhiteSpace(question))
						{
							return question;
						}
					}
				}
			}

			if (this._currentQuestionIndex == Questions.Length)
			{
				this._currentQuestionIndex = 0;
			}

			var autoQuestion = this.Questions[_currentQuestionIndex++];

			this.ConsoleWriteStreamedLine($"> {autoQuestion} ", ConsoleColor.Yellow, streamChunkSize: 1, suppressLineFeed: true);
			Thread.Sleep(500);
			base.ConsoleWriteLine();

			return autoQuestion;
		}

		private async Task ProcessQuestion(string question, ChatCompletionsOptions completionsOptions)
		{
			// Get similarity results from the database using a vector search
			var results = await this.GetDatabaseResults(question);

			// Generate a natural language response (Completions API using a GPT model)
			var answer = await this.GenerateAnswer(question, results, completionsOptions);

			this.ConsoleWriteAssistantResponse(answer);

			if (this._generatePosterImage)
			{
				// Generate an image based on the results (DALL-E model)
				await this.GeneratePosterImage(results);
			}

			// Done
			base.ConsoleWriteLine();
			base.ConsoleWriteLine($"Vectorized question:     {this._elapsedVectorizeQuestion}");
			base.ConsoleWriteLine($"Ran vector search:       {this._elapsedRunVectorSearch}");
			base.ConsoleWriteLine($"Generated response:      {this._elapsedGenerateAnswer}");
			if (this._generatePosterImage)
			{
				base.ConsoleWriteLine($"Generated poster image:  {this._elapsedGeneratePoster}");
			}
		}

		protected async Task<float[]> VectorizeQuestion(string question)
		{
			var started = DateTime.Now;

			this.ConsoleWriteWaitingFor("Vectorizing question");

			var vectors = default(float[]);
			try
			{
				var embeddingsOptions = new EmbeddingsOptions(
					deploymentName: Shared.AppConfig.OpenAI.EmbeddingsDeploymentName,   // Text embeddings model
					input: new[] { question });                                         // Natural language query

				var embeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
				var embeddingItems = embeddings.Value.Data;
				vectors = embeddingItems[0].Embedding.ToArray();
			}
			catch (Exception ex)
			{
				base.ConsoleWriteLine("Error generating vector embeddings", ConsoleColor.Red);
				base.ConsoleWriteLine(ex.Message, ConsoleColor.Red);
			}

			this._elapsedVectorizeQuestion = DateTime.Now.Subtract(started);

			return vectors;
		}

		protected abstract Task<JObject[]> GetDatabaseResults(string question);

		private async Task<string> GenerateAnswer(string question, dynamic[] results, ChatCompletionsOptions completionsOptions)
		{
			if (results == null)
			{
				return null;
			}

			var started = DateTime.Now;

			this.ConsoleWriteWaitingFor("Generating response");

			var sb = new StringBuilder();
			sb.AppendLine($"The database returned the following recommendations after being asked '{question}'.");
			sb.AppendLine($"Generate a natural language response of these recommendations.");
			sb.AppendLine($"Limit your response to the information in the recommendations returned by the database.");

			foreach (var result in results)
			{
				sb.AppendLine(JsonConvert.SerializeObject(result));
				sb.AppendLine();
			}

			var userMessagePrompt = sb.ToString();

			this.ConsoleWritePromptMessage(userMessagePrompt);

			completionsOptions.Messages.Add(new ChatRequestUserMessage(userMessagePrompt));

			var completions = await Shared.OpenAIClient.GetChatCompletionsAsync(completionsOptions);    // GPT model
			var answer = completions.Value.Choices[0].Message.Content;                                  // Natural language answer

			this._elapsedGenerateAnswer = DateTime.Now.Subtract(started);

			return answer;
		}

		private async Task GeneratePosterImage(dynamic[] results)
		{
			var started = DateTime.Now;

			var sb = new StringBuilder();
			sb.AppendLine("I am planning a 'Movie Discussion Night' event, where we will get together and discuss each of these movies:");

			var counter = 0;
			foreach (var result in results)
			{
				sb.AppendLine($"{++counter}. {result.title}");
			}

			sb.AppendLine("Make a collage poster depicting one image based on each movie.");
			sb.AppendLine("Generate a title \"Movie Discussion Night\" in big letters at the top of the poster.");
			sb.AppendLine("Generate a subtitle that says \"Let's Discuss...\".");

			var imagePrompt = sb.ToString();

			this.ConsoleWritePromptMessage(imagePrompt);

			var response = default(Azure.Response<ImageGenerations>);
			try
			{
				response = await Shared.OpenAIClient.GetImageGenerationsAsync(
					new ImageGenerationOptions()
					{
						DeploymentName = Shared.AppConfig.OpenAI.DalleDeploymentName,
						Prompt = imagePrompt,
						Size = ImageSize.Size1024x1792,
						Quality = ImageGenerationQuality.Standard,
					});
			}
			catch (Exception ex)
			{
				base.ConsoleWriteLine("Error generating poster image", ConsoleColor.Red);
				base.ConsoleWriteLine(ex.Message, ConsoleColor.Red);
			}

			this._elapsedGeneratePoster = DateTime.Now.Subtract(started);

			if (response == null)
			{
				return;
			}

			var generatedImage = response.Value.Data[0];
			if (!string.IsNullOrEmpty(generatedImage.RevisedPrompt) && this._showInternalOperations)
			{
				this.ConsoleWritePromptMessage($"Input prompt revised to:\n{generatedImage.RevisedPrompt}");
			}

			this.ConsoleWriteAssistantResponse($"Generated image is ready at:\n{generatedImage.Url.AbsoluteUri}");
			this.OpenBrowser(generatedImage.Url.AbsoluteUri);
		}

		private void OpenBrowser(string url)
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				};
				Process.Start(psi);
			}
			catch (Exception ex)
			{
				base.ConsoleWriteLine($"Error opening browser: {ex.Message}", ConsoleColor.Red);
			}
		}

		#region Console write helpers

		private void ConsoleWritePromptMessage(string text)
		{
			if (!this._showInternalOperations)
			{
				return;
			}

			this.ConsoleWriteHeading("PROMPT MESSAGE", ConsoleColor.Magenta);
			base.ConsoleWriteLine(text, ConsoleColor.Magenta);
		}

		private void ConsoleWriteAssistantResponse(string text)
		{
			if (!this._showInternalOperations)
			{
				this.ConsoleClearLine();
			}

			this.ConsoleWriteHeading("ASSISTANT RESPONSE", ConsoleColor.Cyan);
			this.ConsoleWriteStreamedLine(text, ConsoleColor.Cyan, streamChunkSize: 10);
		}

		protected void ConsoleWriteWaitingFor(string text)
		{
			if (this._showInternalOperations)
			{
				return;
			}

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write($"   {text}...");
			Console.ResetColor();
		}

		private void ConsoleWriteStreamedLine(string text, ConsoleColor color = ConsoleColor.Gray, int? streamChunkSize = null, bool suppressLineFeed = false)
		{
			Console.ForegroundColor = color;

			if (streamChunkSize != null && this._streamOutput && text != null)
			{
				for (var i = 0; i < text.Length; i += streamChunkSize.Value)
				{
					var chunk = text.Substring(i, Math.Min(streamChunkSize.Value, text.Length - i));
					Console.Write(chunk);
					Thread.Sleep(1);
				}
			}
			else
			{
				Console.Write(text);
			}

			if (!suppressLineFeed)
			{
				Console.Write(Environment.NewLine);
			}

			Console.ResetColor();
		}

		private void ConsoleClearLine()
		{
			Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
			Console.Write(new string(' ', Console.WindowWidth));
		}

		#endregion

	}
}
