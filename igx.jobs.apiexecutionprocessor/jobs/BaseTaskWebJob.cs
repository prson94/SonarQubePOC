using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace igx.jobs.apiexecutionprocessor
{
	public abstract class BaseTaskWebJob: BaseWebJob
	{
		protected BaseTaskWebJob(IConfiguration config) : base(config) { }

		internal bool HasWork(SqlConnection conn)
		{
			var existsSql = @"IF EXISTS (SELECT 1 FROM [queue].task where MachineAssigned is null and NumberOfRetries < 2)
													BEGIN
														select 1;
													END
													ELSE
													BEGIN
													   select 0;
													END";
			try
			{
				return conn.QuerySingle<bool>(existsSql);
			}
			catch (SqlException)
			{
				return false;
			}
		}
	}
}
