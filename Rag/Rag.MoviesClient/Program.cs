using Newtonsoft.Json;
using Rag.MoviesClient.Config;
using Rag.MoviesClient.RagProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rag.MoviesClient
{
	public static class Program
	{
		private static Dictionary<string, Func<Task>> _actionMethods;
		private static string _action;

		private static async Task Main(string[] args)
		{
			Shared.Initialize();
			SetRagProvider();

			ShowMenu();
			while (true)
			{
				ConsoleOutput.Write("Selection: ");
				var input = Console.ReadLine();
				_action = input.ToUpper().Trim();
				var command = _action.Split(' ')[0];
				if (_actionMethods.TryGetValue(command, out Func<Task> actionMethod))
				{
					await RunAction(actionMethod);
					ShowMenu();
				}
				else if (_action == "Q")
				{
					break;
				}
				else
				{
					ConsoleOutput.WriteLine($"?{input}", ConsoleColor.Red);
				}
			}
		}

		private static void SetRagProvider()
		{
			var dataPopulator = RagProviderFactory.GetDataPopulator();
			var dataVectorizer = RagProviderFactory.GetDataVectorizer();
			var moviesAssistant = RagProviderFactory.GetMoviesAssistant();

			_actionMethods = new Dictionary<string, Func<Task>>()
			{
				{ "LD", dataPopulator.LoadData },
				{ "VD", dataVectorizer.VectorizeData },
				{ "UD", dataPopulator.UpdateData},
				{ "MA", moviesAssistant.RunMoviesAssistant },
				{ "CE", ChangeEdition },
				{ "UC", UpdateConfiguration },
				{ "RD", dataPopulator.ResetData },
				{ "AC", ViewAppConfig },
			};
		}

		private static void ShowMenu()
		{
			//Console.WindowWidth = 90;
			Console.OutputEncoding = Encoding.UTF8;
			Console.Clear();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(@"  ____      _    ____     __  __            _                ____ _ _            _   ");
			Console.WriteLine(@" |  _ \    / \  / ___|   |  \/  | _____   _(_) ___  ___     / ___| (_) ___ _ __ | |_ ");
			Console.WriteLine(@" | |_) |  / _ \| |  _    | |\/| |/ _ \ \ / / |/ _ \/ __|   | |   | | |/ _ \ '_ \| __|");
			Console.WriteLine(@" |  _ <  / ___ \ |_| |   | |  | | (_) \ V /| |  __/\__ \   | |___| | |  __/ | | | |_ ");
			Console.WriteLine(@" |_| \_\/_/   \_\____|   |_|  |_|\___/ \_/ |_|\___||___/    \____|_|_|\___|_| |_|\__|");
			Console.WriteLine();
			Console.WriteLine(@$"   Edition: {RagProviderFactory.GetProviderName()}");
			Console.WriteLine($"{new string('─', Console.WindowWidth - 1)}");
			Console.WriteLine();
			Console.ResetColor();
			Console.WriteLine("Make a selection");
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(" • LD - Load data              • CE - Change edition");
			Console.WriteLine(" • VD - Vectorize data         • UC - Update configuration");
			Console.WriteLine(" • UD - Update data            • RD - Reset data");
			Console.WriteLine();
			Console.WriteLine(" • MA - Movies assistant");
			Console.WriteLine();
			Console.WriteLine(" • Q  - Quit");
			Console.WriteLine();
			Console.ResetColor();
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
				ConsoleOutput.WriteLine($"Error: {message}", ConsoleColor.Red);
			}

			Console.WriteLine();
			Console.Write("Done. Press any key to continue...");
			Console.ReadKey(true);
		}

		private static async Task UpdateConfiguration()
		{
			ConsoleOutput.WriteHeading("Demo Configuration", ConsoleColor.Yellow);
			ConsoleOutput.WriteLine(JsonConvert.SerializeObject(DemoConfig.Instance, Formatting.Indented), ConsoleColor.Gray);

			var updated = false;
			while (true)
			{
				ConsoleOutput.WriteLine();
				ConsoleOutput.WriteLine("Change configuration property value", ConsoleColor.White);
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.Write("  Property: ");
				var propertyName = Console.ReadLine();

                if (propertyName.Trim().Length == 0)
                {
					break;
                }

				Console.Write("  Value:    ");
				var propertyValue = Console.ReadLine();

				try
				{
					var demoConfigType = typeof(DemoConfig);
					var property = demoConfigType.GetProperty(propertyName);
					if (property.PropertyType == typeof(bool))
					{
						property.SetValue(DemoConfig.Instance, bool.Parse(propertyValue));
					}
					else
					{
						property.SetValue(DemoConfig.Instance, propertyValue);
					}
					ConsoleOutput.WriteLine("Property updated successfully", ConsoleColor.Yellow);
					updated = true;
				}
				catch (Exception ex)
				{
					ConsoleOutput.WriteLine(ex.Message, ConsoleColor.Red);
				}
			}

			if (updated)
			{
				ConsoleOutput.WriteLine();
				ConsoleOutput.WriteLine("Updated configuration:", ConsoleColor.White);
				ConsoleOutput.WriteLine(JsonConvert.SerializeObject(DemoConfig.Instance, Formatting.Indented), ConsoleColor.Gray);
			} 
		}

		private static async Task ChangeEdition()
		{
			try
			{
				RagProviderFactory.RagProviderType = (RagProviderType)Enum.Parse(typeof(RagProviderType), _action.Split(' ')[1], ignoreCase: true);
				SetRagProvider();
				ConsoleOutput.WriteLine($"Edition has been changed to: {RagProviderFactory.GetProviderName()}", ConsoleColor.Yellow);
			}
			catch
			{
				ConsoleOutput.WriteLine("?Invalid syntax for change edition command", ConsoleColor.Red);
			}
		}

		private static async Task ViewAppConfig()
		{
			ConsoleOutput.WriteHeading("App Config (appsettings.json)", ConsoleColor.Yellow);
			ConsoleOutput.WriteLine(JsonConvert.SerializeObject(Shared.AppConfig, Formatting.Indented), ConsoleColor.Gray);
		}

	}
}
