using System;

namespace Rag.MoviesClient.RagProviders.Base
{
	public abstract class RagProviderBase
	{
		protected void ConsoleWrite(object text, ConsoleColor color = ConsoleColor.Gray) =>
			this.ConsoleWriteLine(text, color, suppressLineFeed: true);

		protected void ConsoleWriteLine(object text = null, ConsoleColor color = ConsoleColor.Gray, bool suppressLineFeed = false)
		{
			Console.ForegroundColor = color;

			Console.Write(text);

			if (!suppressLineFeed)
			{
				Console.Write(Environment.NewLine);
			}

			Console.ResetColor();
		}

		protected void ConsoleWriteHeading(string text, ConsoleColor color)
		{
			var width = Console.WindowWidth;
			Console.WriteLine();
			Console.ForegroundColor = color;
			Console.WriteLine($"    ╔{new string('═', text.Length + 2)}╗");
			Console.WriteLine($"    ║ {text} ║");
			Console.WriteLine($"════╝{new string(' ', text.Length + 2)}╚{new string('═', width - text.Length - 8)}");
			Console.ResetColor();
			Console.WriteLine();
		}

	}
}
