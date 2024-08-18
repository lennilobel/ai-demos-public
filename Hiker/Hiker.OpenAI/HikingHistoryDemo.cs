using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Hiker.OpenAI
{
	public class HikingHistoryDemo : HikerDemoBase
	{
		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiGptDeploymentName = config["OpenAiGptDeploymentName"];

			var endpoint = new Uri(openAiEndpoint);
			var credentials = new AzureKeyCredential(openAiKey);
			var openAIClient = new OpenAIClient(endpoint, credentials);

			var completionOptions = new ChatCompletionsOptions
			{
				MaxTokens = 1000,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				NucleusSamplingFactor = 0.95f, // Top P
				DeploymentName = openAiGptDeploymentName
			};

			var humanMessageText = default(string);
			var completions = default(ChatCompletions);
			var aiResponse = default(ChatResponseMessage);

			// Configure the assistant with a system message and some hiking history
			humanMessageText = @"
You are upbeat and friendly. You introduce yourself when first saying hello. 

You will provide short answers to my questions, based on my hiking records below:

              -=-=- Hiking History -=-=--

| Trail Name      | Hike Date  | Country  | Weather 
| --------------- | ---------- | -------- | --------
| Cascade Falls   | 2021-07-15 | Canada   | Sunny   
| Johnston Canyon | 2022-05-10 | Canada   | Cloudy  
| Lake Louise     | 2020-09-05 | Canada   | Rainy   
| Angel's Landing | 2023-06-20 | USA      | Sunny   
| Gros Morne      | 2021-08-25 | Canada   | Foggy   
| Hocking Hills   | 2022-04-01 | USA      | Sunny   
| The Chief       | 2020-07-05 | Canada   | Sunny   
| Skaftafell      | 2022-09-10 | Iceland  | Cloudy
| Buttress        | 1995-07-01 | USA      | Sunny   
| --------------- | ---------- | -------- | --------
			";
			base.WriteLine(humanMessageText, ConsoleColor.Cyan);
			completionOptions.Messages.Add(new ChatRequestSystemMessage(humanMessageText));

			// Say hello to the assistant
			humanMessageText = @"
Hi! 
			";
			base.WriteLine(humanMessageText, ConsoleColor.Yellow);
			completionOptions.Messages.Add(new ChatRequestUserMessage(humanMessageText));

			// Get the response from the assistant ...
			completions = await openAIClient.GetChatCompletionsAsync(completionOptions);
			aiResponse = completions.Choices[0].Message;
			base.WriteLine(aiResponse.Content, ConsoleColor.Green);
			completionOptions.Messages.Add(new ChatRequestAssistantMessage(aiResponse.Content));

			// Supply the hiking history request to the assistant
			humanMessageText = @"
I would like to know the ratio of hikes I did in Canada compared to hikes done in other countries.
			";
			base.WriteLine(humanMessageText, ConsoleColor.Yellow);
			completionOptions.Messages.Add(new ChatRequestUserMessage(humanMessageText));

			// Get the response from the assistant with the answer
			completions = await openAIClient.GetChatCompletionsAsync(completionOptions);
			aiResponse = completions.Choices[0].Message;
			base.WriteLine(aiResponse.Content, ConsoleColor.Green);
		}
	}
}
