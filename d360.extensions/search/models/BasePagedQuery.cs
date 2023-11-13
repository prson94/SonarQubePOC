using d360.core.search;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace d360.extensions.search.models
{
	internal abstract class BasePagedQuery<T> : IPagedQuery<T> where T : IPagedQuerySqlModel
	{
		protected int PageSize = 50000;
		protected long CurrentHighID;
		protected List<T> _data;
		protected SqlConnection _connection;
		protected string _query;
		public DynamicParameters _param;
		protected bool LastPage;
		protected readonly int _defaultQueryCommandTimeout = 180;

		protected BasePagedQuery(SqlConnection connection, DynamicParameters param = null)
		{
			_connection = connection;
			_param = new DynamicParameters();
			if (param != null)
			{
				foreach (var paramName in param.ParameterNames)
				{
					_param.Add(paramName, ((SqlMapper.IParameterLookup)param)[paramName]);
				}
			}
			_data = new List<T>();
		}

		//Hook, used to clean up temp tables
		protected virtual void OnLastPage()
		{
		}

		/// <summary>
		/// Fetches the next "page" of data. Starting with the requested AssetID
		/// No need to get any records with a lower AssetID's
		/// </summary>
		/// <param name="AssetID"></param>
		protected virtual void FetchDataPage(long AssetID)
		{
			if (LastPage)
			{
				return;
			}

			_param.Add("PagerAssetID", AssetID);
			_param.Add("PageSize", PageSize);
			try
			{
				_data = _connection.Query<T>(_query, _param, commandTimeout: _defaultQueryCommandTimeout).ToList();
				if (_data.Count() < PageSize)
				{
					//If we fetched less than PageSize, this is the last page of data
					LastPage = true;
					OnLastPage();
				}
				else
				{
					var MinAssetID = _data.Min(i => i.AssetID);
					var MaxAssetID = _data.Max(i => i.AssetID);
					if (MinAssetID == MaxAssetID)
					{
						//If min and max AssetID is the same, the whole "page" is the same asset and it can't be guaranteed that all records for one asset has been fetched
						throw new PagedQueryException("Search of " + typeof(T) + " got more than " + PageSize + " results for one AssetID");
					}
					else
					{
						//The page may have an incomplete set of records for the highest Asset ID, so remove those from the data stored.
						_data.RemoveAll(i => i.AssetID == MaxAssetID);
						CurrentHighID = _data.Max(i => i.AssetID);
					}
				}
			}
			catch (Exception e)
			{
				throw new PagedQueryException($"Failed paged query for {AssetID}, {_query}. Error: {e.Message}");
			}
		}
		/// <summary>
		/// Fetches records from the query for the provided Asset ID
		/// </summary>
		/// <param name="AssetID"></param>
		/// <returns></returns>
		public List<T> GetByAssetID(long AssetID)
		{
			//If requested ID is higher than what is current, and last page has not been reached, fetch the next data page
			if (!LastPage && AssetID > CurrentHighID)
			{
				FetchDataPage(AssetID);
			}

			return _data.Where(i => i.AssetID == AssetID).ToList();
		}
	}
}
