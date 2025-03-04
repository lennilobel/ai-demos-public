using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
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
			var openAIClient = new AzureOpenAIClient(endpoint, credentials);
			var chatClient = openAIClient.GetChatClient(openAiGptDeploymentName);

			var completionOptions = new ChatCompletionOptions
			{
				MaxOutputTokenCount = 400,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				TopP = 0.95f,
			};

			var conversation = new List<ChatMessage>();

			// Configure the assistant with a system prompt
			var systemPrompt = @"
You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly. 
You introduce yourself when first saying hello. When helping people out, you always ask them 
for this information to inform the hiking recommendation you provide:

1. Where they are located
2. What hiking intensity they are looking for

You will then provide three suggestions for nearby hikes that vary in length after you get that information. 
You will also share an interesting fact about the local nature on the hikes when making a recommendation.
			";
			base.WriteLine($"[System]: {systemPrompt}", ConsoleColor.Cyan);
			conversation.Add(new SystemChatMessage(systemPrompt));

			// Say hello to the assistant
			var userPrompt = @"
Hi! 
Apparently you can help me find a hike that I will like?
			";
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			// Get the response from the assistant with information about how to request a recommendation
			var completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
			var completionRole = completion.Role;
			var completionText = completion.Content[0].Text;

			base.WriteLine($"[{completionRole}]: {completionText}", ConsoleColor.Green);

			conversation.Add(new AssistantChatMessage(completionText));

			// Ask for a hiking recommendation near Montreal
			userPrompt = @"
I live in the greater Montreal area and would like an easy hike. I don't mind driving a bit to get there.
I don't want the hike to be over 10 miles round trip. I'd consider a point-to-point hike.
I want the hike to be as isolated as possible. I don't want to see many people.
I would like it to be as bug free as possible.
			";
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			// Get the response from the assistant with the recommendation
			completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
			completionRole = completion.Role;
			completionText = completion.Content[0].Text;

			base.WriteLine($"{completionRole}: {completionText}", ConsoleColor.Green);

			// Ask for a hiking recommendation in New Hampshire
			userPrompt = @"
I am visiting New Hampshire and have a car.
I want a very challenging hike.
I want to climb a few peaks.
I can start at 6am and finish at 6pm.
			";
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			// Get the response from the assistant with the recommendation
			completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
			completionRole = completion.Role;
			completionText = completion.Content[0].Text;

			base.WriteLine($"{completionRole}: {completionText}", ConsoleColor.Green);
		}
	}
}
