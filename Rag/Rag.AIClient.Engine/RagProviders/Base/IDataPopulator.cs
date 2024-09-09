using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public interface IDataPopulator
	{
		Task LoadData();
		Task ResetData();
		Task UpdateData();
	}
}
