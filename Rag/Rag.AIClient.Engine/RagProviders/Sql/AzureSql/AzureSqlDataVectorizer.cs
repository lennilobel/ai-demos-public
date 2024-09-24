using Rag.AIClient.Engine.RagProviders.Base;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Sql.AzureSql
{
	public class AzureSqlDataVectorizer : DataVectorizerBase
	{
		public AzureSqlDataVectorizer(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		protected override async Task VectorizeEntities(int[] movieIds)
		{
			Debugger.Break();

			await SqlDataAccess.RunStoredProcedure(
				storedProcedureName: "VectorizeMovies",
				storedProcedureParameters:
				[
					("@MovieIdsCsv", movieIds == null ? null : string.Join(',', movieIds)),
				]
			);
		}

	}
}
