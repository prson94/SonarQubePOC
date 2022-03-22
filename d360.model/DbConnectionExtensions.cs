using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

using d360.core.entities;

using Dapper;

namespace d360.model
{
    public static class DbConnectionExtensions
    {
        public static int UpdateFieldMove(this DbConnection cnn, FieldType toField, FieldType fromField, int currentResourceID)
        {
            string updateSql = $" Update fieldtype set ColumnOrder ={toField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={toField.ID};";

            if (fromField != null)
            {
                updateSql += $" Update fieldtype set ColumnOrder ={fromField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={fromField.ID};";
            }

            return cnn.Execute(updateSql);
        }

        public static int UpdateFieldMove(this DbConnection cnn, List<FieldType> fields, int currentResourceID)
        {
            StringBuilder updateSql = new StringBuilder();
            foreach (FieldType f in fields)
            {
                updateSql.Append($" Update fieldtype set ColumnOrder ={f.ColumnOrder},UpdatedBy = {currentResourceID} where Id={f.ID};");
            }

            return cnn.Execute(updateSql.ToString());
        }

        public static SqlBulkCopy CreateBulkCopy(this SqlConnection company, string tableName, int batchSize = 5000, int timeout = 3600, SqlTransaction trans = null)
        {
            if (trans == null)
            {
                return new SqlBulkCopy(company)
                {
                    BatchSize = batchSize,
                    DestinationTableName = tableName,
                    BulkCopyTimeout = timeout
                };
            }
            else
            {
                return new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                {
                    BatchSize = batchSize,
                    DestinationTableName = tableName,
                    BulkCopyTimeout = timeout
                };
            }
        }

        public static async Task OpenIfClosed(this SqlConnection cnn)
        {
            if (cnn.State != ConnectionState.Open)
            {
                await cnn.OpenAsync();
            }
        }
    }
}
