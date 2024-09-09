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
            "I love Asian food",
            "I need someting that cooks in less than 15 minutes",
			"Snacks or desserts",
			"Pasta dishes",
		];

        protected override string BuildChatPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"You help people find great recipes. You are upbeat and friendly.");

            return sb.ToString();
        }

        protected override string BuildChatResponse(string question)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"The user asked:  '{question}'.");
            sb.AppendLine($"The database returned the following similarity results from a vector search:");

            return sb.ToString();
        }

        protected override string BuildImageGenerationPrompt()
        {
            var sb = new StringBuilder();

            return sb.ToString();
        }

        // Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
		//  (by using a subquery, we only need to call VectorDistance once in the inner SELECT clause, and can reuse it in the outer ORDER BY clause)
		protected override string GetVectorSearchSql() =>
			@"
                  SELECT TOP 10
                    vd.id,
                    vd.name,
                    vd.ingredients,
                    vd.prepTimeMinutes,
                    vd.cookTimeMinutes,
                    vd.servings,
                    vd.difficulty,
                    vd.cuisine,
                    vd.caloriesPerServing,
                    vd.tags,
                    vd.rating,
                    vd.reviewCount,
                    vd.mealType,
                    vd.SimilarityScore
                FROM (
                    SELECT
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
                        VectorDistance(c.vectors, @vectors, false) AS SimilarityScore
                    FROM
                        c
                ) AS vd
                ORDER BY
                    vd.SimilarityScore DESC
			";

    }
}
