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
	public class HikingBenefitsDemo : HikerDemoBase
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

			var userPrompt = @$"
Summarize the following text in a maximum of 20 words:

# Hiking Benefits

**Hiking** is a wonderful activity that offers a plethora of benefits for both your body and mind. Here are some compelling reasons why you should consider starting hiking:
1.	**Physical Fitness**:
	- **Cardiovascular Health**: Hiking gets your heart pumping, improving cardiovascular fitness. The varied terrain challenges your body and burns calories.
	- Strength and Endurance: Uphill climbs and uneven trails engage different muscle groups, enhancing strength and endurance.
	- Weight Management: Regular hiking can help you maintain a healthy weight.
2.	Mental Well-Being:
	- Stress Reduction: Nature has a calming effect. Hiking outdoors reduces stress, anxiety, and promotes relaxation.
	- Improved Mood: Fresh air, sunlight, and natural surroundings boost your mood and overall happiness.
	- Mindfulness: Disconnect from screens and immerse yourself in the present moment. Hiking encourages mindfulness.
3.	Connection with Nature:
	- Scenic Views: Explore breathtaking landscapes, from lush forests to mountain peaks. Nature's beauty rejuvenates the soul.
	- Wildlife Encounters: Spot birds, animals, and plant life. Connecting with nature fosters appreciation and wonder.
4.	Social Interaction:
	- Group Hikes: Join hiking clubs or go with friends. It's a great way to bond and share experiences.
	- Solitude: Solo hikes provide introspection and solitude, allowing you to recharge.
5.	Adventure and Exploration:
	- Discover Hidden Gems: Hiking takes you off the beaten path. Discover hidden waterfalls, caves, and scenic trails.
	- Sense of Accomplishment: Reaching a summit or completing a challenging trail gives a sense of achievement.
Remember, hiking can be tailored to your fitness level—start with shorter, easier trails and gradually progress. Lace up those hiking boots and embark on an adventure! 🌲🥾
			";
			base.WriteLine($"[User]: {userPrompt}", ConsoleColor.Yellow);
			conversation.Add(new UserChatMessage(userPrompt));

			var completion = (await chatClient.CompleteChatAsync(conversation, completionOptions)).Value;
			var completionRole = completion.Role;
			var completionText = completion.Content[0].Text;

			base.WriteLine($"[{completionRole}]: {completionText}", ConsoleColor.Green);
		}
	}
}
