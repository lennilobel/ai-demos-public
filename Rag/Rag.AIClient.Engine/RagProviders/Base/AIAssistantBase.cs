using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.EmbeddingModels;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class AIAssistantBase : RagBase, IAIAssistant
	{
		private readonly bool _interactive = true;	// Wait for the user to press Enter for each question
		private readonly bool _streamOutput = true; // Stream output to simulate reading and writing

		protected TimeSpan _elapsedVectorizeQuestion;
		protected TimeSpan _elapsedRunVectorSearch;
		private TimeSpan _elapsedGenerateAnswer;
		private TimeSpan _elapsedGeneratePoster;

		private int _currentQuestionIndex;

		protected AIAssistantBase(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected abstract string[] Questions { get; }

		public async Task RunAIAssistant()
		{
			Debugger.Break();

			this.SayHello();

			var completionsOptions = this.InitializeCompletionsOptions();

			this.SetChatPrompt(completionsOptions);

			this._currentQuestionIndex = 0;
			while (true)
			{
				var question = this.GetQuestion();

				if (question == null)
				{
					break;
				}

				try
				{
					await this.ProcessQuestion(question, completionsOptions);
				}
				catch (Exception ex)
				{
					ConsoleOutput.WriteErrorLine(ex.Message);
				}
			}
		}

		private void SayHello()
		{
			Console.Clear();
			Console.ForegroundColor = ConsoleColor.Cyan;
			this.ShowBanner();
			ConsoleOutput.WriteEnvironmentInfo();
			Console.WriteLine();
			Console.ResetColor();
		}

		protected abstract void ShowBanner();

		private ChatCompletionsOptions InitializeCompletionsOptions() =>
			new()
			{
				MaxTokens = 1000,                   // Max number of tokens for the response; the more tokens you specify (spend), the more verbose the response
				Temperature = 1.0f,                 // Range is 0.0 to 2.0; controls "apparent creativity"; higher = more random, lower = more deterministic
				FrequencyPenalty = 0.0f,            // Range is -2.0 to 2.0; controls likelihood of repeating words; higher = less likely, lower = more likely
				PresencePenalty = 0.0f,             // Range is -2.0 to 2.0; controls likelihood of introducing new topics; higher = more likely, lower = less likely
				NucleusSamplingFactor = 0.95f,      // Range is 0.0 to 2.0; aka "Top P sampling"; temperature alternative; controls diversity of responses (1.0 is full random, lower values limit randomness)
				DeploymentName =                    // The deployment name to specify which completions model to use
					Shared.AppConfig.OpenAI.CompletionsDeploymentName,
			};

		private void SetChatPrompt(ChatCompletionsOptions completionsOptions)
		{
			var sb = new StringBuilder();
			sb.Append(this.BuildChatPrompt());

			if (DemoConfig.Instance.NoEmojis)
			{
				sb.AppendLine("Don't include emojis, because they won't render in my demo console application.");
			}

			if (DemoConfig.Instance.NoMarkdown)
			{
				sb.AppendLine("Don't include markdown syntax, because it won't render in my demo console application.");
			}

			if (DemoConfig.Instance.ResponseLanguage != "English")
			{
				sb.AppendLine($"Translate your recommendations in {DemoConfig.Instance.ResponseLanguage}; don't include the recommendations in English. ");
			}

			var prompt = sb.ToString();

			this.ConsoleWritePromptMessage(prompt);

			completionsOptions.Messages.Add(new ChatRequestSystemMessage(prompt));
		}

		protected abstract string BuildChatPrompt();

		private string GetQuestion()
		{
			ConsoleOutput.WriteHeading("User Question", ConsoleColor.Yellow);
			ConsoleOutput.WriteLine("[A] = Auto / [M] = Manual / [ESC] = Quit: ", suppressLineFeed: true);

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
						ConsoleOutput.WriteLine("> ", ConsoleColor.Yellow, suppressLineFeed: true);
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
			ConsoleOutput.WriteLine();

			return autoQuestion;
		}

		private async Task ProcessQuestion(string question, ChatCompletionsOptions completionsOptions)
		{
			// Get similarity results from the database using a vector search
			var results = await this.GetDatabaseResults(question);

			// Generate a natural language response (Completions API using a GPT model)
			var answer = await this.GenerateAnswer(question, results, completionsOptions);

			this.ConsoleWriteAssistantResponse(answer);

			if (DemoConfig.Instance.GeneratePosterImage)
			{
				// Generate an image based on the results (DALL-E model)
				await this.GeneratePosterImage(results);
			}

			// Done
			ConsoleOutput.WriteLine();
			ConsoleOutput.WriteLine($"Vectorized question:     {this._elapsedVectorizeQuestion}");
			ConsoleOutput.WriteLine($"Ran vector search:       {this._elapsedRunVectorSearch}");
			ConsoleOutput.WriteLine($"Generated response:      {this._elapsedGenerateAnswer}");
			if (DemoConfig.Instance.GeneratePosterImage)
			{
				ConsoleOutput.WriteLine($"Generated poster image:  {this._elapsedGeneratePoster}");
			}
		}

		protected async Task<float[]> VectorizeQuestion(string question)
		{
			var started = DateTime.Now;

			this.ConsoleWriteWaitingFor("Vectorizing question");

			var vector = default(float[]);
			try
			{
				var embeddingsOptions = new EmbeddingsOptions(
					deploymentName: EmbeddingModelFactory.GetDeploymentName(),  // Text embeddings model
					input: [question]);											// Natural language query

				var embeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
				var embeddingItems = embeddings.Value.Data;
				vector = embeddingItems[0].Embedding.ToArray();
			}
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine("Error vectorizing question");
				ConsoleOutput.WriteErrorLine(ex.Message);
			}

			this._elapsedVectorizeQuestion = DateTime.Now.Subtract(started);

			return vector;
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
			sb.Append(this.BuildChatResponse(question));
			sb.AppendLine();

			if (results.Length == 0)
			{
				sb.AppendLine($"The database has no recommendations.");
			}
			else
			{
				sb.AppendLine($"The database recommendations are:");
				sb.AppendLine();

				foreach (var result in results)
				{
					sb.AppendLine(JsonConvert.SerializeObject(result));
					sb.AppendLine();
				}
			}

			var userMessagePrompt = sb.ToString();

			this.ConsoleWritePromptMessage(userMessagePrompt);

			completionsOptions.Messages.Add(new ChatRequestUserMessage(userMessagePrompt));

			var completions = await Shared.OpenAIClient.GetChatCompletionsAsync(completionsOptions);    // GPT model
			var answer = completions.Value.Choices[0].Message.Content;                                  // Natural language answer

			this._elapsedGenerateAnswer = DateTime.Now.Subtract(started);

			return answer;
		}

		protected abstract string BuildChatResponse(string question);

		private async Task GeneratePosterImage(dynamic[] results)
		{
			var started = DateTime.Now;

			var sb = new StringBuilder();
			sb.Append(this.BuildImageGenerationPrompt());
			sb.AppendLine("The database results are:");

			var counter = 0;
			foreach (var result in results)
			{
				sb.AppendLine($"{++counter}. {result.title ?? result.Title}");
			}

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
				ConsoleOutput.WriteErrorLine("Error generating poster image");
				ConsoleOutput.WriteErrorLine(ex.Message);
			}

			this._elapsedGeneratePoster = DateTime.Now.Subtract(started);

			if (response == null)
			{
				return;
			}

			var generatedImage = response.Value.Data[0];
			if (!string.IsNullOrEmpty(generatedImage.RevisedPrompt) && DemoConfig.Instance.ShowInternalOperations)
			{
				this.ConsoleWritePromptMessage($"Input prompt revised to:\n{generatedImage.RevisedPrompt}");
			}

			this.ConsoleWriteAssistantResponse($"Generated image is ready at:\n{generatedImage.Url.AbsoluteUri}");
			this.OpenBrowser(generatedImage.Url.AbsoluteUri);
		}

		protected abstract string BuildImageGenerationPrompt();

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
				ConsoleOutput.WriteErrorLine($"Error opening browser: {ex.Message}");
			}
		}

		#region Console write helpers

		private void ConsoleWritePromptMessage(string text)
		{
			if (!DemoConfig.Instance.ShowInternalOperations)
			{
				return;
			}

			ConsoleOutput.WriteHeading("Prompt Message", ConsoleColor.Green);
			ConsoleOutput.WriteLine(text, ConsoleColor.Green);
		}

		private void ConsoleWriteAssistantResponse(string text)
		{
			if (!DemoConfig.Instance.ShowInternalOperations)
			{
				this.ConsoleClearLine();
			}

			ConsoleOutput.WriteHeading("Assistant Response", ConsoleColor.Cyan);
			this.ConsoleWriteStreamedLine(text, ConsoleColor.Cyan, streamChunkSize: 10);
		}

		protected void ConsoleWriteWaitingFor(string text)
		{
			if (DemoConfig.Instance.ShowInternalOperations)
			{
				return;
			}

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write($"   {text}...");
			Console.ResetColor();
		}

		private void ConsoleWriteStreamedLine(string text, ConsoleColor color = ConsoleColor.Gray, int? streamChunkSize = null, bool suppressLineFeed = false)
		{
			//streamChunkSize = null;	// uncomment this line to disable the streamed output effect
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
