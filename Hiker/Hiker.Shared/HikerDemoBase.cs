using System;
using System.Threading.Tasks;

namespace Hiker.Shared
{
	public abstract class HikerDemoBase
	{
		protected void WriteLine(string text, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ResetColor();
		}

		public async Task PromptAndRun()
		{
			this.ShowMenu();
			while (true)
			{
				Console.Write("Selection: ");
				var input = Console.ReadLine();
				var demoId = input.ToUpper().Trim();

				if (await this.RunDemo(demoId))
				{
					continue;
				}
				else if (demoId == "Q")
				{
					break;
				}

				Console.WriteLine($"?{input}");
			}
		}

		protected virtual async Task<bool> RunDemo(string demoId)
		{
			return false;
		}

		protected async Task RunDemo(Func<Task> demoMethod)
		{
			try
			{
				await demoMethod();
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				while (ex.InnerException != null)
				{
					ex = ex.InnerException;
					message += Environment.NewLine + ex.Message;
				}
				Console.WriteLine($"Error: {ex.Message}");
			}
			Console.WriteLine();
			Console.Write("Done. Press any key to continue...");
			Console.ReadKey(true);
			Console.Clear();
			this.ShowMenu();
		}

		protected virtual void ShowMenu()
		{
		}

	}
}
