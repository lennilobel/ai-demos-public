using Rag.AIClient.Config;
using System;
using System.Text;

namespace Rag.AIClient.RagProviders.Base
{
	public abstract class MoviesAssistantBase : AIAssistantBase
	{
		protected override void ShowBanner()
		{
			Console.WriteLine(@"  __  __            _                  _            _     _              _   ");
			Console.WriteLine(@" |  \/  | _____   _(_) ___  ___       / \   ___ ___(_)___| |_ __ _ _ __ | |_ ");
			Console.WriteLine(@" | |\/| |/ _ \ \ / / |/ _ \/ __|     / _ \ / __/ __| / __| __/ _` | '_ \| __|");
			Console.WriteLine(@" | |  | | (_) \ V /| |  __/\__ \    / ___ \\__ \__ \ \__ \ || (_| | | | | |_ ");
			Console.WriteLine(@" |_|  |_|\___/ \_/ |_|\___||___/   /_/   \_\___/___/_|___/\__\__,_|_| |_|\__|");
			Console.WriteLine();
		}

		protected override string[] Questions => [
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

		protected override string BuildChatPrompt()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"You are a movies enthusiast who helps people discover films that they would enjoy watching.");
			sb.AppendLine($"Your demeanor is {DemoConfig.Instance.Demeanor}.");
			sb.AppendLine($"Only include the following details of each movie recommendation: title, year, overview, {DemoConfig.Instance.IncludeDetails}.");

			return sb.ToString();
		}

		protected override string BuildChatResponse(string question)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"The movies database returned recommendations after being asked '{question}'.");
			sb.AppendLine($"Generate a natural language response of these recommendations.");
			sb.AppendLine($"If the recommendations returned by the database do not fit the question, then apologize and explain that you have no matching information, and provide the database results as alternate suggestions.");
			sb.AppendLine($"Limit your response to the recommendations returned by the database; do not embellish with any other information.");
			sb.AppendLine($"Phrase your response as though you are making the recommendations, rather than the database.");
			sb.AppendLine($"List the recommendations in order of most similar to least similar.");

			return sb.ToString();
		}

		protected override string BuildImageGenerationPrompt()
		{
			var sb = new StringBuilder();

			sb.AppendLine("I am planning a 'Movie Discussion Night' event, where we will get together and discuss each of the results return by the database");
			sb.AppendLine("Make a collage poster depicting one image based on each movie.");
			sb.AppendLine("Generate a title \"Movie Discussion Night\" in big letters at the top of the poster.");
			sb.AppendLine("Generate a subtitle that says \"Let's Discuss...\".");

			return sb.ToString();
		}

	}
}
