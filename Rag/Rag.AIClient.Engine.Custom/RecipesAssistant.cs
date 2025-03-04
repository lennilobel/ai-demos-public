using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb;
using System;
using System.Text;

namespace Rag.AIClient.Engine.Custom
{
	public class RecipesAssistant : CosmosDbMoviesAssistant
    {
		public RecipesAssistant(IRagProvider ragProvider)
            : base(ragProvider)
        {
        }

        protected override void ShowBanner()
        {
            Console.WriteLine("Recipes Assistant");
            Console.WriteLine();
        }

		protected override string[] Questions => [
			"How about some Italian appetizers?",
			"Show me your pizza recipes",
			"Pineapple in the ingredients",
			"Please recommend delicious Italian desserts",
			"Fresh Mediterranean salad options",
			"I love chicken, kebab, and falafel",
			"Got any Asian stir fry recipes?",
			"Give me some soup choices",
			"Traditional Korean breakfast",
		];

		protected override string BuildChatSystemPrompt()
        {
            var sb = new StringBuilder();

			sb.AppendLine($"You are an assistant that helps people find recipes from a database. You are upbeat and friendly.");

			return sb.ToString();
        }

        protected override string BuildChatResponse(string question)
        {
            var sb = new StringBuilder();

			sb.AppendLine($"The user asked the question \"{question}\" and the database returned the recipes below");
			sb.AppendLine($"Generate a response that starts with a sentence or two related to the user's question,");
			sb.AppendLine($"followed by each recipe's details. For the details, list the ingredients as a comma-separated");
			sb.AppendLine($"string, and list the instructions as a numbered list:");

			return sb.ToString();
        }

        protected override string BuildImageGenerationPrompt()
        {
            var sb = new StringBuilder();

            return sb.ToString();
        }

        // Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
		protected override string GetVectorSearchSql() =>
			@"
                  SELECT TOP 10
                    c.id,
                    c.name,
                    c.ingredients,
                    c.prepTimeMinutes,
                    c.cookTimeMinutes,
                    c.servings,
                    c.difficulty,
                    c.cuisine,
                    c.caloriesPerServing,
                    c.tags,
                    c.rating,
                    c.reviewCount,
                    c.mealType,
                    VectorDistance(c.vector, @vector) AS similarityScore
                FROM
                    c
                ORDER BY
                    VectorDistance(c.vector, @vector)
			";

    }
}
