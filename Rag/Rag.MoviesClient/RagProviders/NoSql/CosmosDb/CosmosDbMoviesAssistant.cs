using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.CosmosDb
{
	public class CosmosDbMoviesAssistant : MoviesAssistantBase
    {
        protected override async Task<JObject[]> GetDatabaseResults(string question)
        {
            // Generate vectors from a natural language query (Embeddings API using a text embedding model)
            var vectors = await base.VectorizeQuestion(question);

            // Run a vector search in our database (Cosmos DB NoSQL API vector support)
            var results = await this.RunVectorSearch(vectors);

            return results;
        }

		private async Task<JObject[]> RunVectorSearch(float[] vectors)
        {
            var started = DateTime.Now;

            base.ConsoleWriteWaitingFor("Running vector search");

            var database = Shared.CosmosClient.GetDatabase(Shared.AppConfig.CosmosDb.DatabaseName);
            var container = database.GetContainer(Shared.AppConfig.CosmosDb.ContainerName);

            // Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
            //  (by using a subquery, we only need to call VectorDistance once in the inner SELECT clause, and can reuse it in the outer ORDER BY clause)
            var sql = @"
                SELECT TOP 5
                    vd.id,
                    vd.title,
                    vd.budget,
                    vd.genres,
                    vd.original_language,
                    vd.original_title,
                    vd.overview,
                    vd.popularity,
                    vd.production_companies,
                    vd.release_date,
                    vd.revenue,
                    vd.runtime,
                    vd.spoken_languages,
                    vd.video,
                    vd.similarity_score
                FROM (
                    SELECT
                        c.id,
                        c.title,
                        c.budget,
                        c.genres,
                        c.original_language,
                        c.original_title,
                        c.overview,
                        c.popularity,
                        c.production_companies,
                        c.release_date,
                        c.revenue,
                        c.runtime,
                        c.spoken_languages,
                        c.video,
                        VectorDistance(c.vectors, @vectors, false) AS similarity_score
                    FROM
                        c
                ) AS vd
                ORDER BY
                    vd.similarity_score DESC
			";

            if (base._showInternalOperations)
            {
                base.ConsoleWriteHeading("COSMOS DB VECTOR SEARCH QUERY", ConsoleColor.Green);
                base.ConsoleWriteLine(sql, ConsoleColor.Green);
            }

            try
            {
                var query = new QueryDefinition(sql).WithParameter("@vectors", vectors);
                var iterator = container.GetItemQueryIterator<JObject>(query);
                var results = new List<JObject>();

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync();
                    foreach (var result in page)
                    {
                        results.Add(result);
                    }
                }

                if (base._showInternalOperations)
                {
                    base.ConsoleWriteHeading("COSMOS VECTOR SEARCH RESULT", ConsoleColor.Green);

                    var counter = 0;
                    foreach (var result in results)
                    {
                        base.ConsoleWriteLine($"{++counter}. {result["Title"]}", ConsoleColor.Green);
                        base.ConsoleWriteLine(JsonConvert.SerializeObject(result));
                    }
                }

                base._elapsedRunVectorSearch = DateTime.Now.Subtract(started);

                return results.ToArray();
            }
            catch (Exception ex)
            {
                base.ConsoleWriteLine("Error running vector search query", ConsoleColor.Red);
				base.ConsoleWriteLine(ex.Message, ConsoleColor.Red);

                return null;
            }
        }

    }
}
