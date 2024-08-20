using Rag.MoviesClient.RagProviders.Base;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql.AzureSql
{
	public class AzureSqlDataVectorizer : DataVectorizerBase
	{
		protected override async Task VectorizeMovies(int[] movieIds)
		{
			Debugger.Break();

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "VectorizeMovies",
				storedProcedureParameters: [("@MovieIdsCsv", movieIds == null ? null : string.Join(',', movieIds))]
			);
		 }

	}
}
