using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Base
{
	public interface IDataVectorizer
	{
		Task VectorizeData();
		Task VectorizeData(int movieId);
	}
}
