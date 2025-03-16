using System;
using System.Threading.Tasks;

namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class DataVectorizerBase : RagBase, IDataVectorizer
	{
		protected DataVectorizerBase(IRagProvider ragProvider)
			: base(ragProvider)
		{
		}

		public async Task VectorizeData()
		{
			ConsoleHelper.WriteHeading("Vectorize Data", ConsoleHelper.UserColor);
			await this.VectorizeData(null);
		}

		public async Task VectorizeData(int[] ids = null)
		{
			var started = DateTime.Now;

			await this.VectorizeEntities(ids);

			var elapsed = DateTime.Now.Subtract(started);

			ConsoleHelper.WriteLine($"Data vectorized in {elapsed}", ConsoleHelper.UserColor);
		}

		protected abstract Task VectorizeEntities(int[] ids);

	}
}
