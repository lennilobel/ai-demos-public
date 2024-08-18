using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public class HikingImageDemo : SemanticKernelDemoBase
	{
		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiDalleDeploymentName = config["OpenAiDalleDeploymentName"];

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			var textToImageService = new AzureOpenAITextToImageService(openAiDalleDeploymentName, openAiEndpoint, openAiKey, null);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

			var imagePrompt = @"
A postal card with a happy hiker waving, and a beautiful mountain in the background.
There is a trail visible in the foreground.
The postal card has text in red saying: 'You are invited for a hike!'
			";
			base.WriteLine(imagePrompt, ConsoleColor.Cyan);

			var imageUrl = await textToImageService.GenerateImageAsync(imagePrompt, 1024, 1024);

			base.WriteLine("Generated image is ready at:", ConsoleColor.Green);
			base.WriteLine($"{imageUrl}\n", ConsoleColor.Gray);

			this.OpenBrowser(imageUrl);
		}

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
				base.WriteLine($"Error opening browser: {ex.Message}", ConsoleColor.Red);
			}
		}

	}
}
