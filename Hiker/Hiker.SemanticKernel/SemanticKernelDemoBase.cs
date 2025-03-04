using Hiker.Shared;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public abstract class SemanticKernelDemoBase : HikerDemoBase
	{
		protected async Task SendToSemanticKernel(AzureOpenAIChatCompletionService service, ChatHistory chatHistory)
		{
			this.WriteLastChatHistoryMessage(chatHistory);
			chatHistory.Add(await service.GetChatMessageContentAsync(chatHistory, new OpenAIPromptExecutionSettings() { MaxTokens = 400 }));
			this.WriteLastChatHistoryMessage(chatHistory);
		}

		protected async Task SendToSemanticKernel(Kernel kernel, ChatHistory chatHistory)
		{
			this.WriteLastChatHistoryMessage(chatHistory);
			var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
			chatHistory.Add(await kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(chatHistory, settings, kernel));
			this.WriteLastChatHistoryMessage(chatHistory);
		}

		protected void WriteLastChatHistoryMessage(ChatHistory chatHistory)
		{
			var role = chatHistory.Last().Role.Label;
			var content = chatHistory.Last().Content;

			base.WriteLine($"*** {role} ***", ConsoleColor.White);
			
			var color = ConsoleColor.Gray;
			switch (role)
			{
				case "system":
					color = ConsoleColor.Cyan;
					break;

				case "user":
					color = ConsoleColor.Yellow;
					break;

				case "assistant":
					color = ConsoleColor.Green;
					break;
			}

			base.WriteLine(content, color);

			Console.WriteLine();
		}
	}
}
