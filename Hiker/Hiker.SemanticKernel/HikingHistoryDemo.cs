using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public class HikingHistoryDemo : SemanticKernelDemoBase
	{
		public async Task Run()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			var openAiEndpoint = config["OpenAiEndpoint"];
			var openAiKey = config["OpenAiKey"];
			var openAiGptDeploymentName = config["OpenAiGptDeploymentName"];

			var service = new AzureOpenAIChatCompletionService(openAiGptDeploymentName, openAiEndpoint, openAiKey);

			var humanMessageText = default(string);

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
			var chatHistory = new ChatHistory(humanMessageText);
			base.WriteLastChatHistoryMessage(chatHistory);

			// Say hello to the assistant
			humanMessageText = @"
Hi! 
			";
			chatHistory.AddUserMessage(humanMessageText);
			await base.SendToSemanticKernel(service, chatHistory);

			// Supply the hiking history request to the assistant
			humanMessageText = @"
I would like to know the ratio of hikes I did in Canada compared to hikes done in other countries.
			";
			chatHistory.AddUserMessage(humanMessageText);
			await base.SendToSemanticKernel(service, chatHistory);
		}


	}
}
