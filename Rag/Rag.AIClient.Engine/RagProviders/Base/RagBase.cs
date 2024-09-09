namespace Rag.AIClient.Engine.RagProviders.Base
{
	public abstract class RagBase
	{
		public IRagProvider RagProvider { get; }

		protected RagBase(IRagProvider ragProvider)
		{
			this.RagProvider = ragProvider;
		}

	}
}
