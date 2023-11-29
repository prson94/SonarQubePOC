using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace d360.extensions.search.models
{
	internal class PagedQuery<T> : BasePagedQuery<T> where T : IPagedQuerySqlModel
	{
		/// <summary>
		/// Performs a paged/chunked query
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="query">Query string</param>
		/// <param name="param"></param>
		public PagedQuery(SqlConnection connection, string query, DynamicParameters param = null) : base(connection, param)
		{
			//Use <T> to specify columns to select, as SqlMapper can slow down a lot over *
			var alias = "pagedquery";
			var queryColumns = string.Join(", ", typeof(T).GetProperties().Select(p => $"{alias}.{p.Name}").ToArray());
			_query = $"SELECT TOP (@PageSize) {queryColumns} FROM ({query}) {alias} WHERE {alias}.AssetID >= @PagerAssetID ORDER BY {alias}.AssetID option(recompile)";
		}
	}
}
