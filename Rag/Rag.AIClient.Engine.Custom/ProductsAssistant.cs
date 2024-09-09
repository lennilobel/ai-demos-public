using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb;
using System;
using System.Text;

namespace Rag.AIClient.Engine.Custom
{
	public class ProductsAssistant : CosmosDbMoviesAssistant
    {
		public ProductsAssistant(IRagProvider ragProvider)
            : base(ragProvider)
        {
        }

        protected override void ShowBanner()
        {
            Console.WriteLine("Products Assistant");
            Console.WriteLine();
        }

        protected override string[] Questions => [
			"Beauty products with a lifetime guarantee.",
			"Executive chair, must ship in less than 7 business days.",
			"Executive chair, must ship in less than 2 business days.",
		];

        protected override string BuildChatPrompt()
        {
            var sb = new StringBuilder();

			sb.AppendLine($"You help people find products in the catalog. You are upbeat and friendly.");
			
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
                    vd.title,
                    vd.description,
                    vd.category,
                    vd.price,
                    vd.discountPercentage,
                    vd.rating,
                    vd.stock,
                    vd.tags,
                    vd.brand,
                    vd.sku,
                    vd.weight,
                    vd.dimensions,
                    vd.warrantyInformation,
                    vd.shippingInformation,
                    vd.availabilityStatus,
                    vd.reviews,
                    vd.returnPolicy,
                    vd.minimumOrderQuantity,
                    vd.SimilarityScore
                FROM (
                    SELECT
                        c.id,
                        c.title,
                        c.description,
                        c.category,
                        c.price,
                        c.discountPercentage,
                        c.rating,
                        c.stock,
                        c.tags,
                        c.brand,
                        c.sku,
                        c.weight,
                        c.dimensions,
                        c.warrantyInformation,
                        c.shippingInformation,
                        c.availabilityStatus,
                        c.reviews,
                        c.returnPolicy,
                        c.minimumOrderQuantity,
                        VectorDistance(c.vectors, @vectors, false) AS SimilarityScore
                    FROM
                        c
                ) AS vd
                ORDER BY
                    vd.SimilarityScore DESC
			";

    }
}
