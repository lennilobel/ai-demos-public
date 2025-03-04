using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hiker.OpenAI
{
	public class HikingRecommendationWithToolDemo : HikerDemoBase
	{
		private const string GetWeatherToolName = "get_current_weather";

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

			var getWeatherParameters = new
			{
				Type = "object",
				Properties = new
				{
					Location = new
					{
						Type = "string",
						Description = "The city, e.g. Montreal, Sidney",
					},
					Units = new
					{
						Type = "string",
						Description = "The unit system, e.g. imperial or metric",
						Enum = new[]
						{
							"imperial",		// Temp = Fahrenheith, Speed = Miles
							"metric"		// Temp = Celsius,     Speed = Meters
						},
					}
				},
				Required = new[] { "location" },
			};

			var getWeatherChatTool = ChatTool.CreateFunctionTool(
				functionName: GetWeatherToolName,
				functionDescription: "Get the current weather in a given location",
				functionParameters: BinaryData.FromObjectAsJson(getWeatherParameters, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
			);

			var completionOptions = new ChatCompletionOptions
			{
				MaxOutputTokenCount = 400,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				TopP = 0.95f,
				Tools = { getWeatherChatTool },
			};

			var conversation = new List<ChatMessage>();

			// Configure the assistant with a system prompt
			var systemPrompt = @"
You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly.
A good weather is important for a good hike. Only make recommendations if the weather is good or if people insist.
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

			// Ask for a hiking recommendation near Seattle, based on the current weather there
			userPrompt = @"
Is the weather is good today for a hike?
If yes, I would like an easy hike near Seattle. I don't mind driving a bit to get there.
I don't want the hike to be over 10 miles round trip. I'd consider a point-to-point hike.
I want the hike to be as isolated as possible. I don't want to see many people.
I would like it to be as bug free as possible.
			";
			base.WriteLine(userPrompt, ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			//completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;

			var requestCompletion = true;
			while (requestCompletion)
			{
				// Get the response from the assistant, which will first process the tool call, and then supply a recommendation
				completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
				conversation.Add(new AssistantChatMessage(completion));
				completionRole = completion.Role;

				switch (completion.FinishReason)
				{
					// Process tool calls, add the tool call output to the converstaion, and request a new completion
					case ChatFinishReason.ToolCalls:
						foreach (var toolCall in completion.ToolCalls)
						{
							var chatToolOutput = await this.HandleToolCallAsync(toolCall);
							conversation.Add(new ToolChatMessage(toolCall.Id, chatToolOutput));
						}

						break;

					// Show the recommendation and stop requesting additional completions
					case ChatFinishReason.Stop:
						completionText = completion.Content[0].Text;
						base.WriteLine($"[{completionRole}]: {completionText}", ConsoleColor.Green);
						requestCompletion = false;

						break;
				}
			}
		}

		private async Task<string> HandleToolCallAsync(ChatToolCall toolCall)
		{
			if (toolCall.FunctionName == GetWeatherToolName)
			{
				return await this.GetWeather(toolCall);
			}

			throw new Exception($"Chat tool function '{toolCall.FunctionName}' is not supported");
		}

		private async Task<string> GetWeather(ChatToolCall toolCall)
		{
			var functionArguments = JsonConvert.DeserializeObject<JObject>(toolCall.FunctionArguments.ToString());

			var location = functionArguments["location"].Value<string>();
			var units = functionArguments == null ? "imperial" : functionArguments["units"].Value<string>();
			var apiKey = "fdc7c907cfd461ef8762091f68119b8e";
			var url = $"http://api.openweathermap.org/data/2.5/weather?q={location}&units={units}&appid={apiKey}";

			location = location.Split(',')[0];
			using var client = new HttpClient();
			var response = await client.GetAsync(url);

			var result = default(string);
			if (response.IsSuccessStatusCode)
			{
				var responseBody = await response.Content.ReadAsStringAsync();
				var weatherDetail = JsonConvert.DeserializeObject<JObject>(responseBody);
				var weatherDescription = weatherDetail["weather"][0]["description"].Value<string>();
				var temperature = weatherDetail["main"]["temp"].Value<string>();
				var windSpeed = weatherDetail["wind"]["speed"].Value<string>();

				var temperatureUnits = units == "imperial" ? "Fahrenheit" : "Celsius";
				var windSpeedUnits = units == "imperial" ? "miles per hour" : "meters per second";
				result = $"Current weather in {location} is {weatherDescription}, {temperature} {temperatureUnits}, wind speed {windSpeed} {windSpeedUnits}";
			}
			else
			{
				result = $"Current weather in {location} is unknown";
			}

			//result = $"Current weather in {location} is heavy rain, 41.76 Fahrenheit, wind speed 40.22 miles per hour";
			//result = $"Current weather in {location} is clear sky, 85.15 Fahrenheit, wind speed 9.22 miles per hour";

			base.WriteLine($"[Tool]: {result}", ConsoleColor.Gray);

			return result;
		}

	}
}
