using d360.core.search;
using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace d360.extensions.search.models
{
	internal class TempTablePagedQuery<T> : BasePagedQuery<T> where T : IPagedQuerySqlModel
	{
		private readonly string _tableIdentifier;
		private readonly string _alias = "pagedquery";
		private readonly string _initialQuery;
		private readonly int retryLimit = 3;

		/// <summary>
		/// Performs a paged/chunked query using a global temporary table to hold the result of the query
		/// and then paging from that
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="query">Query string</param>
		/// <param name="param"></param>
		public TempTablePagedQuery(SqlConnection connection, string query, DynamicParameters param = null) : base(connection, param)
		{
			PageSize = 150000;
			//generate random name for global temp table
			_tableIdentifier = "pagedQuery_" + Guid.NewGuid().ToString().Replace("-", "_");
			_initialQuery = query;

			CreateTempTable();
		}

		private void CreateTempTable()
		{
			//Use <T> to specify columns to select, as SqlMapper can slow down a lot over *
			var queryColumns = string.Join(", ", typeof(T).GetProperties().Select(p => $"{_alias}.{p.Name}").ToArray());
			_query = $"SELECT TOP (@PageSize) {queryColumns} FROM ##{_tableIdentifier} {_alias} WHERE {_alias}.AssetID >= @PagerAssetID ORDER BY {_alias}.sortid";
			var where = _param.ParameterNames.Contains("PagerAssetID") ? $"WHERE {_alias}.AssetID = @PagerAssetID" : "";

			try
			{
				var createstatement = $@"
                    DROP TABLE IF EXISTS ##{_tableIdentifier};

                    SELECT ROW_NUMBER() OVER (ORDER BY AssetID) AS sortid, {queryColumns}
                    INTO ##{_tableIdentifier}
                    FROM ({_initialQuery}) {_alias} {where};

                    CREATE UNIQUE INDEX UIX_{_tableIdentifier} ON ##{_tableIdentifier} (sortid); 

                    CREATE NONCLUSTERED INDEX IX_{_tableIdentifier}_AssetID ON ##{_tableIdentifier} (AssetID); 
                ";
				_connection.Execute(createstatement, _param, null, _defaultQueryCommandTimeout * 20); //Multiply timeout for statement creating the temp table
			}
			catch (Exception e)
			{
				throw new PagedQueryException($"TempTablePagedQuery failed to create temp table. Error: {e.Message}");
			}
		}

		~TempTablePagedQuery()
		{
			OnLastPage();
		}

		protected override void FetchDataPage(long AssetID)
		{
			var currentRetry = 0;
			for (; ; )
			{
				try
				{
					base.FetchDataPage(AssetID);
					break;
				}
				catch (Exception ex)
				{
					currentRetry++;
					if (currentRetry > retryLimit || !IsTransient(ex))
					{
						throw;
					}
					//re-create Temp Table
					CreateTempTable();
				}
			}
		}

		private bool IsTransient(Exception ex)
		{
			if (ex.Message.Contains($"Error: Invalid object name '##{_tableIdentifier}'"))
			{
				return true;
			}
			return false;
		}

		protected override void OnLastPage()
		{
			base.OnLastPage();
			try
			{
				_connection.Execute($"DROP TABLE IF EXISTS ##{_tableIdentifier}");
			}
			catch (Exception)
			{
				//If connection is closed, the temp table is automatically dropped
			}
		}
	}
}
