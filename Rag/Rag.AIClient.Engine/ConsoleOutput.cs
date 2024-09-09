using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders;
using System;

namespace Rag.AIClient.Engine
{
	public static class ConsoleOutput
    {
		public static void Write(object text, ConsoleColor color = ConsoleColor.Gray) =>
			WriteLine(text, color, suppressLineFeed: true);

		public static void WriteLine(object text = null, ConsoleColor color = ConsoleColor.Gray, bool suppressLineFeed = false)
		{
			Console.ForegroundColor = color;

			Console.Write(text);

			if (!suppressLineFeed)
			{
				Console.Write(Environment.NewLine);
			}

			Console.ResetColor();
		}

		public static void WriteErrorLine(string text) =>
			WriteLine(text, ConsoleColor.Red);

		public static void WriteHeading(string text, ConsoleColor color)
		{
			var width = Console.WindowWidth;
			Console.WriteLine();
			Console.ForegroundColor = color;
			Console.WriteLine($"    ╔{new string('═', text.Length + 2)}╗");
			Console.WriteLine($"    ║ {text} ║");
			Console.WriteLine($"════╝{new string(' ', text.Length + 2)}╚{new string('═', width - text.Length - 9)}");
			Console.ResetColor();
			Console.WriteLine();
		}

		public static void WriteEnvironmentInfo()
		{
			var provider = RagProviderFactory.GetRagProvider();

			Console.WriteLine(@$"   Edition:  {provider.ProviderName}");
			Console.WriteLine(@$"   Database: {provider.DatabaseName}");
			Console.WriteLine(@$"   Model:    {EmbeddingModelFactory.GetDeploymentName()}");
		}

	}
}
