using Azure;
using Azure.AI.OpenAI;
using Hiker.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
			var openAIClient = new OpenAIClient(endpoint, credentials);

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

			var getWeatherToolDefinition = new ChatCompletionsFunctionToolDefinition()
			{
				Name = GetWeatherToolName,
				Description = "Get the current weather in a given location",
				Parameters = BinaryData.FromObjectAsJson(getWeatherParameters, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
			};

			var completionOptions = new ChatCompletionsOptions
			{
				MaxTokens = 400,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				NucleusSamplingFactor = 0.95f, // Top P
				DeploymentName = openAiGptDeploymentName,
				Tools = { getWeatherToolDefinition },
				ToolChoice = ChatCompletionsToolChoice.Auto
			};

			var humanMessageText = default(string);
			var completions = default(ChatCompletions);
			var aiResponse = default(ChatResponseMessage);

			// Configure the assistant with a system message
			humanMessageText = @"
You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly.
A good weather is important for a good hike. Only make recommendations if the weather is good or if people insist.
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
Is the weather is good today for a hike?
If yes, I would like an easy hike near Seattle. I don't mind driving a bit to get there.
I don't want the hike to be over 10 miles round trip. I'd consider a point-to-point hike.
I want the hike to be as isolated as possible. I don't want to see many people.
I would like it to be as bug free as possible.
			";
			base.WriteLine(humanMessageText, ConsoleColor.Yellow);
			completionOptions.Messages.Add(new ChatRequestUserMessage(humanMessageText));

			// Get the response from the assistant with the recommendation
			completions = await openAIClient.GetChatCompletionsAsync(completionOptions);

			// If the response includes a tool call (e.g., "get weather"), handle it and continue the conversation
			var completionsChoice = completions.Choices[0];
			if (completionsChoice.FinishReason == CompletionsFinishReason.ToolCalls)
			{
				// Include the function call message in the conversation history
				completionOptions.Messages.Add(new ChatRequestAssistantMessage(completionsChoice.Message));

				// Handle each tool call that is resolved
				foreach (var toolCall in completionsChoice.Message.ToolCalls)
				{
					var toolCallMessage = await this.GetToolCallResponseMessage(toolCall);
					completionOptions.Messages.Add(toolCallMessage);
				}

				// Base the answer on the the tool call results
				completions = await openAIClient.GetChatCompletionsAsync(completionOptions);
			}

			aiResponse = completions.Choices[0].Message;
			base.WriteLine(aiResponse.Content, ConsoleColor.Green);
		}

		// Handle tool call responses
		private async Task<ChatRequestToolMessage> GetToolCallResponseMessage(ChatCompletionsToolCall toolCall)
		{
			var functionToolCall = toolCall as ChatCompletionsFunctionToolCall;
			if (functionToolCall?.Name == GetWeatherToolName)
			{
				return await this.GetWeather(functionToolCall);
			}

			throw new Exception($"Tool '{functionToolCall?.Name}' is not supported");
		}

		private async Task<ChatRequestToolMessage> GetWeather(ChatCompletionsFunctionToolCall functionToolCall)
		{
			var functionArguments = JsonConvert.DeserializeObject<JObject>(functionToolCall.Arguments);

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

			base.WriteLine(result, ConsoleColor.Gray);

			return new ChatRequestToolMessage(result, functionToolCall.Id);
		}

	}
}
