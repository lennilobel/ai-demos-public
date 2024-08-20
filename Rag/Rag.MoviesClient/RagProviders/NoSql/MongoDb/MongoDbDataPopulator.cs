using Rag.MoviesClient.RagProviders.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.NoSql.MongoDb
{
	public class MongoDbDataPopulator : IDataPopulator
    {
        public async Task LoadData()
        {
            Debugger.Break();

			ConsoleOutput.WriteHeading("Load Data", ConsoleColor.Yellow);

			// Create new database and container
			// Load movie documents to from Data\movies.json (all movies)

			throw new Exception("MongoDb RAG data populator is not implemented");
        }

        public async Task UpdateData()
        {
            Debugger.Break();

			ConsoleOutput.WriteHeading("Update Data", ConsoleColor.Yellow);

			// Load movie documents to from Data\movies-sw.json (three Star Wars trilogy movies)

			throw new Exception("MongoDb RAG data populator is not implemented");
		}

		public async Task ResetData()
        {
            Debugger.Break();

			ConsoleOutput.WriteHeading("Reset Data", ConsoleColor.Yellow);

			// Delete the three Star Wars trilogy movies from the container

			throw new Exception("MongoDb RAG data populator is not implemented");
		}

	}
}
