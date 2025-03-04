using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public class HikingRecommendationDemo : SemanticKernelDemoBase
	{
		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiGptDeploymentName = config["OpenAiGptDeploymentName"];

			var service = new AzureOpenAIChatCompletionService(openAiGptDeploymentName, openAiEndpoint, openAiKey);

			var humanMessageText = default(string);

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
			var chatHistory = new ChatHistory(humanMessageText);
			await base.SendToSemanticKernel(service, chatHistory);

			// Say hello to the assistant
			humanMessageText = @"
Hi! 
Apparently you can help me find a hike that I will like?
			";
			chatHistory.AddUserMessage(humanMessageText);
			await base.SendToSemanticKernel(service, chatHistory);

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
			chatHistory.AddUserMessage(humanMessageText);
			await base.SendToSemanticKernel(service, chatHistory);
		}

	}
}
