using Newtonsoft.Json;
using Rag.AIClient.Engine;
using Rag.AIClient.Engine.Config;
using Rag.AIClient.Engine.EmbeddingModels;
using Rag.AIClient.Engine.RagProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rag.AIClient
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
				_action = input.Trim();
				var command = _action.Split(' ')[0].ToUpper();
				if (_actionMethods.TryGetValue(command, out Func<Task> actionMethod))
				{
					await RunAction(actionMethod);
					ShowMenu();
				}
				else if (command == "Q")
				{
					break;
				}
				else
				{
					ConsoleOutput.WriteErrorLine($"?{input}");
				}
			}
		}

		private static void SetRagProvider()
		{
			var ragProvider = RagProviderFactory.GetRagProvider();

			var dataPopulator = ragProvider.GetDataPopulator();
			var dataVectorizer = ragProvider.GetDataVectorizer();
			var aiAssistant = ragProvider.GetAIAssistant();

			_actionMethods = new Dictionary<string, Func<Task>>()
			{
				{ "ID", dataPopulator.InitializeData },
				{ "VD", dataVectorizer.VectorizeData },
				{ "UD", dataPopulator.UpdateData},
				{ "RD", dataPopulator.ResetData },
				{ "AI", aiAssistant.RunAIAssistant },
				{ "HW", RunHelloWorldDemo},
				{ "CP", ChangeRagProvider },
				{ "CM", ChangeEmbeddingModel },
				{ "UC", UpdateConfiguration },
				{ "AC", ViewAppConfig },
				{ "IV", InitializeAndVectorize },
			};
		}

		private static void ShowMenu()
		{
			//Console.WindowWidth = 90;
			Console.OutputEncoding = Encoding.UTF8;
			Console.Clear();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(@"  ____      _    ____      _    ___    ____ _ _            _   ");
			Console.WriteLine(@" |  _ \    / \  / ___|    / \  |_ _|  / ___| (_) ___ _ __ | |_ ");
			Console.WriteLine(@" | |_) |  / _ \| |  _    / _ \  | |  | |   | | |/ _ \ '_ \| __|");
			Console.WriteLine(@" |  _ <  / ___ \ |_| |  / ___ \ | |  | |___| | |  __/ | | | |_ ");
			Console.WriteLine(@" |_| \_\/_/   \_\____| /_/   \_\___|  \____|_|_|\___|_| |_|\__|");
			Console.WriteLine();
			ConsoleOutput.WriteEnvironmentInfo();
			Console.WriteLine($"{new string('─', Console.WindowWidth - 1)}");
			Console.WriteLine();
			Console.ResetColor();
			Console.WriteLine("Make a selection");
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(" • ID - Initialize data        • CP - Change RAG provider");
			Console.WriteLine(" • VD - Vectorize data         • CM - Change embedding model");
			Console.WriteLine(" • UD - Update data            • UC - Update configuration");
			Console.WriteLine(" • RD - Reset data");
			Console.WriteLine();
			Console.WriteLine(" • HW - Hello RAG World demo");
			Console.WriteLine(" • AI - AI Assistant demo");
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
				ConsoleOutput.WriteErrorLine($"Error: {message}");
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
					ConsoleOutput.WriteErrorLine(ex.Message);
				}
			}

			if (updated)
			{
				ConsoleOutput.WriteLine();
				ConsoleOutput.WriteLine("Updated configuration:", ConsoleColor.White);
				ConsoleOutput.WriteLine(JsonConvert.SerializeObject(DemoConfig.Instance, Formatting.Indented), ConsoleColor.Gray);
			} 
		}

		private static async Task ChangeRagProvider()
		{
			var currentRagProviderType = RagProviderFactory.RagProviderType;
			try
			{
				var parameters = _action.Split(' ');
				
				var ragProviderType = (RagProviderType)Enum.Parse(typeof(RagProviderType), parameters[1], ignoreCase: true);
				var externalRagProviderType = default(string);

				if (ragProviderType == RagProviderType.External)
				{
					externalRagProviderType = parameters[2];
				}

				RagProviderFactory.RagProviderType = ragProviderType;
				RagProviderFactory.ExternalRagProviderType = externalRagProviderType;
				SetRagProvider();

				ConsoleOutput.WriteLine($"Edition has been changed to: {RagProviderFactory.GetRagProvider().ProviderName}", ConsoleColor.Yellow);
			} 
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine("Unable to change the RAG provider");
				ConsoleOutput.WriteErrorLine(ex.Message);
				RagProviderFactory.RagProviderType = currentRagProviderType;
			}
		}

		private static async Task ChangeEmbeddingModel()
		{
			var currentEmbeddingModelType = EmbeddingModelFactory.EmbeddingModelType;
			try
			{
				EmbeddingModelFactory.EmbeddingModelType = (EmbeddingModelType)Enum.Parse(typeof(EmbeddingModelType), _action.Split(' ')[1], ignoreCase: true);
				ConsoleOutput.WriteLine($"Embedding model has been changed to: {EmbeddingModelFactory.GetDeploymentName()}", ConsoleColor.Yellow);
			}
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine("Unable to change the embedding model");
				ConsoleOutput.WriteErrorLine(ex.Message);
				ConsoleOutput.WriteErrorLine($"Valid embedding model values are: {string.Join(", ", Enum.GetNames(typeof(EmbeddingModelType)))}");
				EmbeddingModelFactory.EmbeddingModelType = currentEmbeddingModelType;
			}
		}

		private static async Task ViewAppConfig()
		{
			ConsoleOutput.WriteHeading("App Config (appsettings.json)", ConsoleColor.Yellow);
			ConsoleOutput.WriteLine(JsonConvert.SerializeObject(Shared.AppConfig, Formatting.Indented), ConsoleColor.Gray);
		}

		private static async Task RunHelloWorldDemo() =>
			await new HelloRagWorld().RunDemo();

		private static async Task InitializeAndVectorize()
		{
			var started = DateTime.Now;

			await InitializeAndVectorize(RagProviderType.SqlServer, EmbeddingModelType.TextEmbedding3Large);
			await InitializeAndVectorize(RagProviderType.SqlServer, EmbeddingModelType.TextEmbedding3Small);
			await InitializeAndVectorize(RagProviderType.SqlServer, EmbeddingModelType.TextEmbeddingAda002);

			await InitializeAndVectorize(RagProviderType.AzureSql, EmbeddingModelType.TextEmbedding3Large);
			await InitializeAndVectorize(RagProviderType.AzureSql, EmbeddingModelType.TextEmbedding3Small);
			await InitializeAndVectorize(RagProviderType.AzureSql, EmbeddingModelType.TextEmbeddingAda002);

			//await InitializeAndVectorize(RagProviderType.AzureSqlEap, EmbeddingModelType.TextEmbedding3Large);	// EAP doesn't support large
			await InitializeAndVectorize(RagProviderType.AzureSqlEap, EmbeddingModelType.TextEmbedding3Small);
			await InitializeAndVectorize(RagProviderType.AzureSqlEap, EmbeddingModelType.TextEmbeddingAda002);

			await InitializeAndVectorize(RagProviderType.CosmosDb, EmbeddingModelType.TextEmbedding3Large);
			await InitializeAndVectorize(RagProviderType.CosmosDb, EmbeddingModelType.TextEmbedding3Small);
			await InitializeAndVectorize(RagProviderType.CosmosDb, EmbeddingModelType.TextEmbeddingAda002);

			//await InitializeAndVectorize(RagProviderType.MongoDb, EmbeddingModelType.TextEmbedding3Large);	// Free tier doesn't support large
			await InitializeAndVectorize(RagProviderType.MongoDb, EmbeddingModelType.TextEmbedding3Small);
			await InitializeAndVectorize(RagProviderType.MongoDb, EmbeddingModelType.TextEmbeddingAda002);

			Console.WriteLine($"Completed in {DateTime.Now.Subtract(started)}");
		}

		private static async Task InitializeAndVectorize(RagProviderType ragProviderType, EmbeddingModelType embeddingModelType)
		{
			ConsoleOutput.WriteHeading($"Initialize & Vectorize - {ragProviderType} {embeddingModelType}", ConsoleColor.Green);

			RagProviderFactory.RagProviderType = ragProviderType;
			EmbeddingModelFactory.EmbeddingModelType = embeddingModelType;

			var ragProvider = RagProviderFactory.GetRagProvider();
			var dataPopulator = ragProvider.GetDataPopulator();
			var dataVectorizer = ragProvider.GetDataVectorizer();

			try
			{
				await dataPopulator.InitializeData();
				await dataVectorizer.VectorizeData();
			}
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine(ex.Message);
			}
		}

		//private static void MakeStringIds()
		//{
		//	var filePath = @"C:\Projects\Sleek\ai-demos-private\Rag\Rag.AIClient\Data\products.json";
		//	var content = System.IO.File.ReadAllText(filePath);
		//	var updatedContent = System.Text.RegularExpressions.Regex.Replace(content, @"""id"":\s*(\d+)", @"""id"": ""$1""");
		//	System.IO.File.WriteAllText(filePath, updatedContent);
		//}

	}
}
