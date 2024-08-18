using Rag.MoviesClient.RagProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rag.MoviesClient
{
    public static class Program
	{
		private static async Task Main(string[] args)
		{
			Shared.Initialize();

			var dataPopulator = RagProviderFactory.GetDataPopulator();
			var dataVectorizer = RagProviderFactory.GetDataVectorizer();
			var moviesAssistant = RagProviderFactory.GetMoviesAssistant();

			var actionMethods = new Dictionary<string, Func<Task>>
			{
				{ "LD", dataPopulator.LoadData },
				{ "VD", dataVectorizer.VectorizeData },
				{ "UD", dataPopulator.UpdateData},
				{ "RD", dataPopulator.ResetData },
				{ "MA", moviesAssistant.RunMoviesAssistant },
			};

			ShowMenu();
			while (true)
			{
				Console.Write("Selection: ");
				var input = Console.ReadLine();
				var action = input.ToUpper().Trim();
				if (actionMethods.TryGetValue(action, out Func<Task> actionMethod))
				{
					await RunAction(actionMethod);
					ShowMenu();
				}
				else if (action == "Q")
				{
					break;
				}
				else
				{
					Console.WriteLine($"?{input}");
				}
			}
		}

		private static void ShowMenu()
		{
			Console.Clear();

			Console.OutputEncoding = Encoding.UTF8;
			Console.Clear();

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(@"  ____      _    ____     __  __            _                ____ _ _            _   ");
			Console.WriteLine(@" |  _ \    / \  / ___|   |  \/  | _____   _(_) ___  ___     / ___| (_) ___ _ __ | |_ ");
			Console.WriteLine(@" | |_) |  / _ \| |  _    | |\/| |/ _ \ \ / / |/ _ \/ __|   | |   | | |/ _ \ '_ \| __|");
			Console.WriteLine(@" |  _ <  / ___ \ |_| |   | |  | | (_) \ V /| |  __/\__ \   | |___| | |  __/ | | | |_ ");
			Console.WriteLine(@" |_| \_\/_/   \_\____|   |_|  |_|\___/ \_/ |_|\___||___/    \____|_|_|\___|_| |_|\__|");
			Console.WriteLine();
			Console.WriteLine(@$"   {RagProviderFactory.GetProviderName()} Edition");
			Console.WriteLine();
			Console.ResetColor();

			Console.WriteLine("LD - Load data");
			Console.WriteLine("VD - Vectorize data");
			Console.WriteLine("UD - Update data");
			Console.WriteLine("RD - Reset data");
			Console.WriteLine();
			Console.WriteLine("MA - Movies assistant");
			Console.WriteLine();
			Console.WriteLine("Q  - Quit");
			Console.WriteLine();
		}

		private static async Task RunAction(Func<Task> actionMethod)
		{
			try
			{
				await actionMethod();
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				while (ex.InnerException != null)
				{
					ex = ex.InnerException;
					message += Environment.NewLine + ex.Message;
				}
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Error: {message}");
				Console.ResetColor();
			}

			Console.WriteLine();
			Console.Write("Done. Press any key to continue...");
			Console.ReadKey(true);
		}

	}
}
