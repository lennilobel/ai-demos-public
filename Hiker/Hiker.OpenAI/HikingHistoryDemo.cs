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
You are upbeat and friendly. You introduce yourself as a Hiking History assistant when first saying hello. 

You will provide short answers to my questions, based on hiking history records provided by the user.
			";
			base.WriteLine($"[System]: {systemPrompt}", ConsoleColor.Cyan);
			conversation.Add(new SystemChatMessage(systemPrompt));

			// Provide the assistant with your hiking history
			var userPrompt = @"
Hi. Here is my hiking history. I will want you to analyze this history in response to questions I will ask.


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
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			// Get the response from the assistant with an acknowledgment that it understands your hiking history
			var completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
			var completionRole = completion.Role;
			var completionText = completion.Content[0].Text;

			base.WriteLine($"[{completionRole}]: {completionText}", ConsoleColor.Green);

			conversation.Add(new AssistantChatMessage(completionText));

			// Ask a question regarding hiking history
			userPrompt = @"
I would like to know the ratio of hikes I did in Canada compared to hikes done in other countries.
			";
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			// Get the response from the assistant with the history query results
			completion = await chatClient.CompleteChatAsync(conversation, completionOptions);
			completionRole = completion.Role;
			completionText = completion.Content[0].Text;

			base.WriteLine($"[{completionRole}]: {completionText}", ConsoleColor.Green);
		}
	}
}
