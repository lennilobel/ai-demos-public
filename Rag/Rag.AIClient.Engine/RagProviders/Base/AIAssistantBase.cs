using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using OpenAI.Images;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.EmbeddingModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

			var completionOptions = this.InitializeCompletionOptions();
			var conversation = new List<ChatMessage>();

			this.SetChatSystemPrompt(conversation);

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
					await this.ProcessQuestion(question, completionOptions, conversation);
				}
				catch (Exception ex)
				{
					ConsoleHelper.WriteErrorLine(ex.Message);
				}
			}
		}

		private void SayHello()
		{
			ConsoleHelper.Clear();
			ConsoleHelper.SetForegroundColor(ConsoleHelper.InfoColor);
			this.ShowBanner();
			ConsoleHelper.WriteEnvironmentInfo();
			Console.WriteLine();
			ConsoleHelper.ResetColor();
		}

		protected abstract void ShowBanner();

		private ChatCompletionOptions InitializeCompletionOptions() =>
			new()
			{
				MaxOutputTokenCount = 1000,	// Max number of tokens for the response; the more tokens you specify (spend), the more verbose the response
				Temperature = 1.0f,         // Range is 0.0 to 2.0; controls "apparent creativity"; higher = more random, lower = more deterministic
				FrequencyPenalty = 0.0f,    // Range is -2.0 to 2.0; controls likelihood of repeating words; higher = less likely, lower = more likely
				PresencePenalty = 0.0f,     // Range is -2.0 to 2.0; controls likelihood of introducing new topics; higher = more likely, lower = less likely
				TopP = 0.95f,				// Range is 0.0 to 2.0; temperature alternative; controls diversity of responses (1.0 is full random, lower values limit randomness)
			};

		private void SetChatSystemPrompt(List<ChatMessage> conversation)
		{
			var sb = new StringBuilder();
			sb.Append(this.BuildChatSystemPrompt());

			var systemPrompt = sb.ToString();

			this.ConsoleWritePromptMessage(systemPrompt);

			conversation.Add(new SystemChatMessage(systemPrompt));
		}

		protected abstract string BuildChatSystemPrompt();

		private string GetQuestion()
		{
			ConsoleHelper.WriteHeading("User Question", ConsoleHelper.UserColor);
			ConsoleHelper.WriteLine("[A] = Auto / [M] = Manual / [ESC] = Quit: ", color: ConsoleHelper.ForegroundColor, suppressLineFeed: true);

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
						ConsoleHelper.WriteLine("> ", ConsoleHelper.UserColor, suppressLineFeed: true);
						ConsoleHelper.SetForegroundColor(ConsoleHelper.UserColor);
						var manualQuestion = Console.ReadLine();
						if (!string.IsNullOrWhiteSpace(manualQuestion))
						{
							return manualQuestion;
						}
					}
				}
			}

			if (this._currentQuestionIndex == Questions.Length)
			{
				Console.WriteLine("There are no more auto-questions defined");
				return null;
			}

			var autoQuestion = this.Questions[_currentQuestionIndex++];

			this.ConsoleWriteQuestion($"> {autoQuestion} ");

			return autoQuestion;
		}

		private async Task ProcessQuestion(string question, ChatCompletionOptions completionOptions, List<ChatMessage> conversation)
		{
			// Get similarity results from the database using a vector search
			var results = await this.GetDatabaseResults(question);

			// Generate a natural language response (Completions API using a GPT model)
			await this.GenerateAnswer(question, results, completionOptions, conversation);

			if (DemoConfig.Instance.GeneratePosterImage)
			{
				// Generate an image based on the results (DALL-E model)
				await this.GeneratePosterImage(results);
			}

			// Done
			ConsoleHelper.WriteLine();
			ConsoleHelper.WriteLine($"Vectorized question:     {this._elapsedVectorizeQuestion}");
			ConsoleHelper.WriteLine($"Ran vector search:       {this._elapsedRunVectorSearch}");
			ConsoleHelper.WriteLine($"Generated response:      {this._elapsedGenerateAnswer}");
			if (DemoConfig.Instance.GeneratePosterImage)
			{
				ConsoleHelper.WriteLine($"Generated poster image:  {this._elapsedGeneratePoster}");
			}
		}

		protected async Task<float[]> VectorizeQuestion(string question)
		{
			var started = DateTime.Now;

			var vector = default(float[]);
			try
			{
				var input = new[] { question };
				var embeddingClient = Shared.AzureOpenAIClient.GetEmbeddingClient(EmbeddingModelFactory.GetDeploymentName());
				var embedding = (await embeddingClient.GenerateEmbeddingsAsync(input)).Value.First();

				vector = embedding.ToFloats().ToArray();
			}
			catch (Exception ex)
			{
				ConsoleHelper.WriteErrorLine("Error vectorizing question");
				ConsoleHelper.WriteErrorLine(ex.Message);
			}

			this._elapsedVectorizeQuestion = DateTime.Now.Subtract(started);

			return vector;
		}

		protected abstract Task<JObject[]> GetDatabaseResults(string question);

		private async Task<string> GenerateAnswer(string question, dynamic[] results, ChatCompletionOptions completionOptions, List<ChatMessage> conversation)
		{
			if (results == null)
			{
				return null;
			}

			var started = DateTime.Now;

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

			var userPrompt = sb.ToString();

			this.ConsoleWritePromptMessage(userPrompt);

			conversation.Add(new UserChatMessage(userPrompt));

			if (DemoConfig.Instance.ShowInternalOperations)
			{
				ConsoleHelper.WriteHeading("Conversation History", ConsoleHelper.SystemColor);
				var maxWidth = Math.Max(0, Console.WindowWidth - 10);
				var counter = 0;
				foreach (var message in conversation)
				{
					ConsoleHelper.WriteLine($" {++counter}) {message.GetType().Name}", ConsoleHelper.SystemColor);
					ConsoleHelper.WriteLine($"     {message.Content.First().Text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Substring(0, maxWidth)}...", ConsoleHelper.SystemColor);
					Console.WriteLine();
				}

				Console.WriteLine();
			}

			var chatClient = Shared.AzureOpenAIClient.GetChatClient(Shared.AppConfig.OpenAI.CompletionDeploymentName);
			var completionUpdates = chatClient.CompleteChatStreamingAsync(conversation);

			ConsoleHelper.WriteHeading("Assistant Response", ConsoleHelper.InfoColor);

			sb = new StringBuilder();
			ConsoleHelper.SetForegroundColor(ConsoleHelper.InfoColor);
			await foreach (var completionUpdate in completionUpdates)
			{
				foreach (var contentPart in completionUpdate.ContentUpdate)
				{
					Console.Write(contentPart.Text);
					sb.Append(contentPart.Text);
				}
			}
			Console.WriteLine();
			ConsoleHelper.ResetColor();
			var answer = sb.ToString();

			conversation.Add(new AssistantChatMessage(answer));

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

			var imageClient = Shared.AzureOpenAIClient.GetImageClient(Shared.AppConfig.OpenAI.DalleDeploymentName);

			var options = new ImageGenerationOptions
			{
				Quality = GeneratedImageQuality.Standard,
				Size = GeneratedImageSize.W1024xH1792,
				ResponseFormat = GeneratedImageFormat.Uri,
			};

			var generatedImage = default(GeneratedImage);
			try
			{
				generatedImage = (await imageClient.GenerateImageAsync(imagePrompt, options)).Value;
			}
			catch (Exception ex)
			{
				ConsoleHelper.WriteErrorLine("Error generating poster image");
				ConsoleHelper.WriteErrorLine(ex.Message);
			}

			this._elapsedGeneratePoster = DateTime.Now.Subtract(started);

			if (generatedImage == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty(generatedImage.RevisedPrompt) && DemoConfig.Instance.ShowInternalOperations)
			{
				this.ConsoleWritePromptMessage($"Input prompt revised to:\n{generatedImage.RevisedPrompt}");
			}

			ConsoleHelper.WriteHeading("Image Generation Response", ConsoleHelper.InfoColor);
			ConsoleHelper.WriteLine($"Generated image is ready at:\n{generatedImage.ImageUri.AbsoluteUri}", ConsoleHelper.InfoColor);

			this.OpenBrowser(generatedImage.ImageUri.AbsoluteUri);
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
				ConsoleHelper.WriteErrorLine($"Error opening browser: {ex.Message}");
			}
		}

		#region Console write helpers

		private void ConsoleWritePromptMessage(string text)
		{
			if (!DemoConfig.Instance.ShowInternalOperations)
			{
				return;
			}

			ConsoleHelper.WriteHeading("Prompt Message", ConsoleHelper.SystemColor);
			ConsoleHelper.WriteLine(text, ConsoleHelper.SystemColor);
		}

		private void ConsoleWriteQuestion(string text)
		{
			ConsoleHelper.SetForegroundColor(ConsoleHelper.UserColor);

			for (var i = 0; i < text.Length; i += 1)
			{
				Console.Write(text[i]);
				Thread.Sleep(1);
			}

			ConsoleHelper.ResetColor();
			ConsoleHelper.WriteLine();
		}

		private void ConsoleClearLine()
		{
			Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
			Console.Write(new string(' ', Console.WindowWidth));
		}

		#endregion

	}
}
