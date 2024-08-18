using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public class HikingRecommendationWithToolDemo : SemanticKernelDemoBase
	{
		private const string GetWeatherToolName = "get_current_weather";

		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiGptDeploymentName = config["OpenAiGptDeploymentName"];

			// Create a Kernel containing the Azure OpenAI Chat Completion Service
			var kernelBuilder = Kernel.CreateBuilder();
			//kernelBuilder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));	// log all model interactions
			var kernel = kernelBuilder
				.AddAzureOpenAIChatCompletion(openAiGptDeploymentName, openAiEndpoint, openAiKey)
				.Build();

			// Add a new plugin with a local .NET function that should be available to the AI model
			kernel.ImportPluginFromFunctions("WeatherPlugin",
			[
				KernelFunctionFactory.CreateFromMethod(
					method: async ([Description("The city, e.g. Montreal, Sidney")] string location, string units = null) => await this.GetWeather(location, units),
					functionName: "get_current_weather",
					description: "Get the current weather in a given location")
			]);

			var humanMessageText = default(string);

			// Start the conversation with a system message
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
			var chatHistory = new ChatHistory(humanMessageText);
			chatHistory.AddUserMessage("Hi!");
			await base.SendToSemanticKernel(kernel, chatHistory);

			// Continue the conversation
			// Supply the recommendation request to the assistant
			humanMessageText = @"
Is the weather is good today for a hike?
If yes, I live in the greater New York area and would like an easy hike. I don't mind driving a bit to get there.
I don't want the hike to be over 10 miles round trip. I'd consider a point-to-point hike.
I want the hike to be as isolated as possible. I don't want to see many people.
I would like it to be as bug free as possible.
			";
			chatHistory.AddUserMessage(humanMessageText);
			await base.SendToSemanticKernel(kernel, chatHistory);
		}

		private async Task<string> GetWeather(string location, string units)
		{
			//// Here you would call a weather API to get the weather for the location
			//return "Periods of rain or drizzle, 15 C";

			var apiKey = "fdc7c907cfd461ef8762091f68119b8e";
			var url = $"http://api.openweathermap.org/data/2.5/weather?q={location}&units={units}&appid={apiKey}";

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

			return result;
		}
	}
}
