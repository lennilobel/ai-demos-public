using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Base
{
	public interface IDataPopulator
	{
		Task LoadData();
		Task ResetData();
		Task UpdateData();
	}
}
