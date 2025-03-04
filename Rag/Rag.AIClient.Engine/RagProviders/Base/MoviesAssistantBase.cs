using Rag.AIClient.Engine.Config;
using System;
using System.Text;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class MoviesAssistantBase : AIAssistantBase
	{
		protected MoviesAssistantBase(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

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
			"Actually, I meant just crime movies in that language.",
			"I love horror flicks.",
		];

		protected override string BuildChatSystemPrompt()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"You are a movies enthusiast who helps people discover films that they would enjoy watching.");
			sb.AppendLine($"Your demeanor is {DemoConfig.Instance.Demeanor}.");
			sb.AppendLine($"You will generate a natural language response from recommendations based on the similarity score included in the results returned from a vector search against a movies database.");
			sb.AppendLine($"Limit your responses to the recommendations returned by the database; do not embellish with any other recommendations you might have.");
			sb.AppendLine($"If there are no recommendations that fit the user's question, don't suggest alternatives.");
			sb.AppendLine($"Only include the following details of each movie recommendation: title, year, overview, {DemoConfig.Instance.IncludeDetails}.");
			sb.AppendLine($"Use consistent formatting for every recommendation.");
			sb.AppendLine($"Phrase your responses as though you are making the recommendations, rather than the database.");
			sb.AppendLine($"Sort the movie recommendations by year.");
			sb.AppendLine($"If there are no recommendations to give, be apologetic.");

			if (DemoConfig.Instance.ResponseLanguage != "English")
			{
				sb.AppendLine($"Translate your recommendations in {DemoConfig.Instance.ResponseLanguage}; don't include the recommendations in English. ");
			}

			return sb.ToString();
		}

		protected override string BuildChatResponse(string question)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"The movies database returned recommendations after the user asked: '{question}'.");

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
