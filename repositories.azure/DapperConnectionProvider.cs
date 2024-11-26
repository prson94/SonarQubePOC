using System.Data;
using System.Data.SqlClient;

namespace repositories.azure
{
	public class DapperConnectionProvider
	{
		public string ReadWriteConnectionString { get; set; }
		public string ReadOnlyConnectionString { get; set; }

		public IDbConnection Connect(bool isReadOnly = false)
			=> new SqlConnection(isReadOnly ? ReadOnlyConnectionString : ReadWriteConnectionString);
	}
}
