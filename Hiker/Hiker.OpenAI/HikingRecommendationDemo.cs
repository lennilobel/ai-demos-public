using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Hiker.OpenAI
{
	public class HikingRecommendationDemo : HikerDemoBase
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
				MaxTokens = 400,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				NucleusSamplingFactor = 0.95f, // Top P
				DeploymentName = openAiGptDeploymentName
			};

			var humanMessageText = default(string);
			var completions = default(ChatCompletions);
			var aiResponse = default(ChatResponseMessage);

			// Configure the assistant with a system message
			humanMessageText = @"
You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly. 
You introduce yourself when first saying hello. When helping people out, you always ask them 
for this information to inform the hiking recommendation you provide:

1. Where they are located
2. What hiking intensity they are looking for

You will then provide three suggestions for nearby hikes that vary in length after you get that information. 
You will also share an interesting fact about the local nature on the hikes when making a recommendation.
			";
			base.WriteLine(humanMessageText, ConsoleColor.Cyan);
			completionOptions.Messages.Add(new ChatRequestSystemMessage(humanMessageText));

			// Say hello to the assistant
			humanMessageText = @"
Hi! 
Apparently you can help me find a hike that I will like?
			";
			base.WriteLine(humanMessageText, ConsoleColor.Yellow);
			completionOptions.Messages.Add(new ChatRequestUserMessage(humanMessageText));

			// Get the response from the assistant with information about how to request a recommendation
			completions = await openAIClient.GetChatCompletionsAsync(completionOptions);
			aiResponse = completions.Choices[0].Message;
			base.WriteLine(aiResponse.Content, ConsoleColor.Green);
			completionOptions.Messages.Add(new ChatRequestAssistantMessage(aiResponse.Content));

			// Supply the recommendation request to the assistant
			humanMessageText = @"
I live in the greater Montreal area and would like an easy hike. I don't mind driving a bit to get there.
I don't want the hike to be over 10 miles round trip. I'd consider a point-to-point hike.
I want the hike to be as isolated as possible. I don't want to see many people.
I would like it to be as bug free as possible.
			";
//			humanMessageText = @"
//I live in New Hampshire.
//I want a very challenging hike.
//I want to climb a few peaks.
//I can start at 6am and finish at 6pm.
//			";
			base.WriteLine(humanMessageText, ConsoleColor.Yellow);
			completionOptions.Messages.Add(new ChatRequestUserMessage(humanMessageText));

			// Get the response from the assistant with the recommendation
			completions = await openAIClient.GetChatCompletionsAsync(completionOptions);
			aiResponse = completions.Choices[0].Message;
			base.WriteLine(aiResponse.Content, ConsoleColor.Green);
		}
	}
}
