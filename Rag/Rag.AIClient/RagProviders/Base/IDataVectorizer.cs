using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Base
{
	public interface IDataVectorizer
	{
		Task VectorizeData();
		Task VectorizeData(int[] movieIds = null);
	}
}
