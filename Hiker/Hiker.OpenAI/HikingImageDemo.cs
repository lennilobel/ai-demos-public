using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using OpenAI.Images;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hiker.OpenAI
{
	public class HikingImageDemo : HikerDemoBase
	{
		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiDalleDeploymentName = config["OpenAiDalleDeploymentName"];

			var endpoint = new Uri(openAiEndpoint);
			var credentials = new AzureKeyCredential(openAiKey);
			var openAIClient = new AzureOpenAIClient(endpoint, credentials);
			var imageClient = openAIClient.GetImageClient(openAiDalleDeploymentName);

			var imagePrompt = @"
A postal card with a happy hiker waving, and a beautiful mountain in the background.
There is a trail visible in the foreground.
The postal card has text in red saying: 'You are invited for a hike!'
			";
			base.WriteLine(imagePrompt, ConsoleColor.Cyan);

			var options = new ImageGenerationOptions
			{
				Quality = GeneratedImageQuality.Standard,
				Size = GeneratedImageSize.W1024xH1024,
				//Style = GeneratedImageStyle.Vivid,
				ResponseFormat = GeneratedImageFormat.Uri,
			};

			var generatedImage = (await imageClient.GenerateImageAsync(imagePrompt, options)).Value;

			if (!string.IsNullOrEmpty(generatedImage.RevisedPrompt))
			{
				base.WriteLine("Input prompt revised to:", ConsoleColor.Green);
				base.WriteLine($"{generatedImage.RevisedPrompt}\n", ConsoleColor.Gray);
			}

			base.WriteLine("Generated image is ready at:", ConsoleColor.Green);
			base.WriteLine($"{generatedImage.ImageUri.AbsoluteUri}\n", ConsoleColor.Gray);

			this.OpenBrowser(generatedImage.ImageUri.AbsoluteUri);
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
