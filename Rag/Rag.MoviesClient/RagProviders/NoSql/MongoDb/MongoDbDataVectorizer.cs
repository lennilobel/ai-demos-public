using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataVectorizer : DataVectorizerBase
	{
		private static class Context
		{
			public static int ItemCount;
			public static int ErrorCount;
			public static double RuCost;
		}

		protected override async Task VectorizeMovies(int[] movieIds)
		{
			Debugger.Break();

			Context.ItemCount = 0;
			Context.ErrorCount = 0;
			Context.RuCost = 0;

			// Raise the throughput on the container

			// Query for all the documents in the container (process results in batches of 100)

			// For each batch
			//  Retrieve the next batch of documents
			//  Generate text embeddings (vectors) for the batch of documents
			//  Update the documents back to the container with generated text embeddings (vectors)

			// Lower the throughput on the container

			throw new Exception("MongoDb RAG data vectorizer is not implemented");
		}

	}
}
