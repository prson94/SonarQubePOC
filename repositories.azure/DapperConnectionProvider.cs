using System.Data;
using System.Data.SqlClient;

namespace repositories.azure
{
	public class DapperConnectionProvider
	{
		public string ConnectionString { get; set; }

		public IDbConnection Connect()
			=> new SqlConnection(ConnectionString);
	}
}
