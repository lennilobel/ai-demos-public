using Rag.MoviesClient.RagProviders.Base;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql.AzureSql
{
	public class AzureSqlDataVectorizer : DataVectorizerBase
	{
		protected override async Task VectorizeMovies(int? movieId)
		{
			Debugger.Break();

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "VectorizeMovies",
				storedProcedureParameters: movieId == null ? null : new (string, object)[] { ("@MovieId", movieId) }
			);
		}

	}
}
