using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Rag.MoviesClient.RagProviders.Sql
{
	public static class SqlDataAccess
    {
		private static int _outputLineNumber;

		public static async Task RunStoredProcedure(
			string storedProcedureName,
			(string ParameterName, object ParameterValue)[] storedProcedureParameters = null,
			Action<IDataReader> getResult = null,
			bool silent = false)
		{
			_outputLineNumber = 0;

			if (!silent)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"Executing stored procedure {storedProcedureName}");
				Console.ResetColor();
			}

			try
			{
				using var conn = new SqlConnection(RagProviderFactory.GetSqlConnectionString());
				await conn.OpenAsync();

				if (!silent)
				{
					conn.InfoMessage += new SqlInfoMessageEventHandler(OnStoredProcedureMessageReceived);
				}

				using var cmd = new SqlCommand(storedProcedureName, conn);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandTimeout = 60 * 30;

				if (storedProcedureParameters != null)
				{
					foreach (var (name, value) in storedProcedureParameters)
					{
						cmd.Parameters.AddWithValue(name, value);
					}
				}

				using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

				if (getResult != null)
				{
					while (await rdr.ReadAsync())
					{
						getResult(rdr);
					}
				}

				await rdr.CloseAsync();
			}
			catch (Exception ex)
			{
				ConsoleOutput.WriteErrorLine($"Error executing stored procedure '{storedProcedureName}'");
				ConsoleOutput.WriteErrorLine(ex.Message);
			}
		}

		private static void OnStoredProcedureMessageReceived(object sender, SqlInfoMessageEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			foreach (SqlError error in e.Errors)
			{
				Console.WriteLine($"{++_outputLineNumber,5}: {error.Message}");
			}
			Console.ResetColor();
		}

	}
}
