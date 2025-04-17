using System.Data.SqlClient;

namespace repositories.azure.extensions
{
	internal static class BulkCopies
	{
		public static SqlBulkCopy CreateBulkCopy(this SqlConnection connection, string tableName, int batchSize = 5000, int timeout = 3600, SqlTransaction trans = null)
		{
			if (trans == null)
			{
				return new SqlBulkCopy(connection)
				{
					BatchSize = batchSize,
					DestinationTableName = tableName,
					BulkCopyTimeout = timeout
				};
			}
			else
			{
				return new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
				{
					BatchSize = batchSize,
					DestinationTableName = tableName,
					BulkCopyTimeout = timeout
				};
			}
		}

	}
}
