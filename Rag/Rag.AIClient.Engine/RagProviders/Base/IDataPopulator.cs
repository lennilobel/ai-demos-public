using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public interface IDataPopulator
	{
		Task InitializeData();
		Task ResetData();
		Task UpdateData();
	}
}
