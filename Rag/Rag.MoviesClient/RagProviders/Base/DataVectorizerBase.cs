using System;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Base
{
	public abstract class DataVectorizerBase : RagProviderBase, IDataVectorizer
	{
		public async Task VectorizeData()
		{
			base.ConsoleWriteHeading("Vectorize Data", ConsoleColor.Yellow);
			await this.VectorizeData(null);
		}

		public async Task VectorizeData(int movieId)
		{
			base.ConsoleWriteLine($"Vectorize Data (Movie ID {movieId})", ConsoleColor.Yellow);
			await this.VectorizeData((int?)movieId);
		}

		private async Task VectorizeData(int? movieId = null)
		{
			var started = DateTime.Now;

			await this.VectorizeMovies(movieId);

			var elapsed = DateTime.Now.Subtract(started);

			base.ConsoleWriteLine($"Data vectorized in {elapsed}", ConsoleColor.Yellow);
		}

		protected abstract Task VectorizeMovies(int? movieId);

	}
}
