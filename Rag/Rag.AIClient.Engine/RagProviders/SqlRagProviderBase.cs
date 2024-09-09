using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.Sql;
using System.IO;

namespace Rag.AIClient.Engine.RagProviders
{
	public abstract class SqlRagProviderBase : RagProviderBase
    {
		public override string DatabaseName => this.SqlConfig.DatabaseName + GetDatabaseNameSuffix();

		public override string GetDataFilePath(string filename) => new FileInfo($@"Data\{filename}").FullName;

        public override IDataPopulator GetDataPopulator() => new SqlDataPopulator(this);

    }
}
