using Newtonsoft.Json.Linq;
using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.MongoDb
{
	public class MongoDbMoviesAssistant : MoviesAssistantBase
    {
        protected override async Task<JObject[]> GetDatabaseResults(string question)
        {
			// Generate vectors from a natural language query (Embeddings API using a text embedding model)
			var vectors = await base.VectorizeQuestion(question);

			// Run a vector search in our database (Mongo DB vCore API vector support)
			var results = await this.RunVectorSearch(vectors);

			return results;
		}

		private async Task<JObject[]> RunVectorSearch(float[] vectors)
        {
			// Run vector search

			throw new Exception("MongoDb RAG movies assistant is not implemented");
        }

    }
}
