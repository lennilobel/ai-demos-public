using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class DataPopulatorBase : RagBase, IDataPopulator
	{
		protected DataPopulatorBase(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public abstract Task InitializeData();
		public abstract Task ResetData();
		public abstract Task UpdateData();
	}
}
