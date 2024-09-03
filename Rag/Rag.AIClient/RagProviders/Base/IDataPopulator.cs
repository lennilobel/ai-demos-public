using System.Threading.Tasks;

namespace Rag.AIClient.RagProviders.Base
{
	public interface IDataPopulator
	{
		Task LoadData();
		Task ResetData();
		Task UpdateData();
	}
}
