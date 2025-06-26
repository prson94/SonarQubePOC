using d360.core.entities;
using d360.core.entities.Usage;
using d360.core.resources;
using Dapper;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Usage : Repository, IUsage
	{
		public int CompanyId { get; set; }
		
		public string WorkspaceId { get; set; }

		public Usage(DapperConnectionProvider provider): base(provider) { }

		public async Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadUsageDetailAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new RepositoryResponse<PagedApiBaseViewModel<dynamic>>(new PagedApiBaseViewModel<dynamic>(), 200, true);

			var dbArgs = new DynamicParameters();

			var orderBySql = "";
			var offsetSql = "";
			var orderDirection = "";
			var pageNum = -1;
			var pageSize = 200;
			var whereClause = "";
			bool includeTotal = true;

			List<string> whereClauseItems = new List<string>();

			string[] columns =
			{
					"resourceuid",
					"firstname",
					"lastname",
					"email",
					"timestamp" ,
					"browser",
					"language",
					"locale",
					"action",
					"sidebar",
					"tab"
				};

			#region handle queryparams
			
			if (!queryParams.IsQueryParameterValidInteger("_pagenum"))
			{
				return new(400, Error.InvalidpageNumNumberValue);
			}

			if (!queryParams.IsQueryParameterValidInteger("_pagesize"))
			{
				return new(400, Error.InvalidpageSizeNumberValue);
			}

			response.Data.pageNum = queryParams.CheckForPageNumber();
			response.Data.pageSize = queryParams.CheckForPageSize();
			
			if (queryParams.IsQueryParameterPresent("_direction"))
			{
				string[] allowedDirections = { "asc", "desc" };
				var order = queryParams.ReadQueryParameterValue("_direction");
				if (!allowedDirections.Contains(order.Trim().ToLower()))
				{
					return new(400, Error.InvalidDirection);
				}

				orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "asc";
			}

			if (!queryParams.IsQueryParameterPresent("_order"))
			{
				orderBySql = $"order by Timestamp {orderDirection}";
			}

			string errorMessage = null;
			HttpStatusCode code = HttpStatusCode.OK;

			var objectTablesThatRequireInnerJoin = new List<string>();

			queryParams.ToList().ForEach(q =>
			{
				var key = q.Key.ToLower();

				if (key.StartsWith("_"))
				{
					if (key == "_order")
					{
						if (columns.Contains(q.Value.ToLower()))
						{
							orderBySql = $"order by {q.Value} {orderDirection}";
						}
						else
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = Error.Invalid_Order;
						}
					}
					else if (key == "_pagenum")
					{
						if (int.TryParse(q.Value, out pageNum))
						{
							if (pageNum < 1) { pageNum = 1; }
						}
					}
					else if (key == "_pagesize")
					{
						if (int.TryParse(q.Value, out pageSize))
						{
							if (pageSize < 1) { pageSize = 1; }
						}
					}
					else if (key == "_includetotal")
					{
						if (!bool.TryParse(q.Value, out includeTotal))
						{
							includeTotal = true;
						}

					}
					else if (key == "_startdate")
					{
						DateTime startDate = DateTime.MinValue;
						if (!DateTime.TryParse(q.Value, out startDate))
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = Error.InvalidStartDate;
						}
						else
						{
							dbArgs.Add("startDate", startDate);
							whereClauseItems.Add("stat.Timestamp >= @startDate");
						}
					}
					else if (key == "_enddate")
					{

						DateTime endDate = DateTime.MaxValue;
						if (!DateTime.TryParse(q.Value, out endDate))
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = Error.InvalidEndDate;
						}
						else
						{
							dbArgs.Add("endDate", endDate);
							whereClauseItems.Add("stat.Timestamp <= @endDate");
						}
					}
					else if (key == "_resourceuid")
					{
						Guid ruid = Guid.Empty;
						if (Guid.TryParse(q.Value, out ruid))
						{
							int reccount = 0;
							using (var connection = ConnectionProvider.Connect(true))
							{
								reccount = connection.QuerySingleOrDefault<int>("select count(1) from reporting.Global_Resource where Uid = @ruid and uid != @emptyuid", new { ruid, emptyuid =  Guid.Empty});

							}
							if (reccount > 0)
							{
								whereClauseItems.Add("r.uid = @resourceUid");
								dbArgs.Add("resourceUid", ruid);
							}
							else
							{
								code = HttpStatusCode.BadRequest;
								errorMessage = Error.Invalid_ResourceUID;
							}
						}
						else
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = Error.Invalid_ResourceUID;
						}
					}
					else if (key == "_assetuid")
					{
						Guid auid = Guid.Empty;
						if (Guid.TryParse(q.Value, out auid))
						{
							int reccount = 0;
							using (var connection = ConnectionProvider.Connect(true))
							{
								reccount = connection.QuerySingleOrDefault<int>("select count(1) from asset where Uid = @auid and Uid != @emptyuid", new { auid, emptyuid = Guid.Empty });

							}

							if (reccount > 0)
							{
								whereClauseItems.Add("aid.uid = @assetuid");
								dbArgs.Add("assetuid", auid);
								objectTablesThatRequireInnerJoin.Add("aid");
							}
							else
							{
								code = HttpStatusCode.BadRequest;
								errorMessage = string.Format(Error.InvalidAssetUid, q.Value);
							}
						}
						else
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = string.Format(Error.InvalidAssetUid, q.Value);
						}
					}
					else if (key == "_assettypeuid")
					{
						Guid atuid = Guid.Empty;
						if (Guid.TryParse(q.Value, out atuid))
						{
							int reccount = 0;
							using (var connection = ConnectionProvider.Connect(true))
							{
								reccount = connection.QuerySingleOrDefault<int>("select count(1) from AssetType where Uid = @atuid and Uid != @emptyuid", new { atuid, emptyuid = Guid.Empty });

							}

							if (reccount > 0)
							{
								whereClauseItems.Add("(atid.uid = @assettypeuid )");
								dbArgs.Add("assettypeuid", atuid);
								objectTablesThatRequireInnerJoin.Add("atid");
							}
							else
							{
								code = HttpStatusCode.BadRequest;
								errorMessage = string.Format(Error.AssetTypeNotFound, q.Value);
							}
						}
						else
						{
							code = HttpStatusCode.BadRequest;
							errorMessage = string.Format(Error.AssetTypeNotFound, q.Value);
						}
					}
					else if (key == "_semanticuid")
					{
						Guid suid = Guid.Empty;
						if (Guid.TryParse(q.Value, out suid))
						{
							whereClauseItems.Add("(sid.uid = @semanticuid)");
							dbArgs.Add("semanticuid", suid);
							objectTablesThatRequireInnerJoin.Add("sid");
						}
					}
				}
			});

			if (!string.IsNullOrEmpty(errorMessage))
			{
				return new((int)code, errorMessage);
			}

			#endregion
			
			if (pageSize > 0 || pageNum > 0)
			{
				if (pageSize < 1) { pageSize = 1; }
				if (pageNum < 1) { pageNum = 1; }

				offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";

			}

			if (whereClauseItems.Count > 0)
			{
				whereClause = $" where {string.Join(" and ", whereClauseItems.ToArray())} ";
			}

			Func<string, string> contains = delegate (string p)
			{
				return (objectTablesThatRequireInnerJoin.Contains(p) ? "inner" : "left");
			};

			string tableSql = $@"
from	usage.Analytic stat
		inner join reporting.Global_Resource r on r.ResourceID = stat.UserId
		left join usage.Sidebar sidebar on sidebar.Id = stat.SidebarId
		left join usage.Tab tab on tab.Id = stat.TabId
		{contains("aid")} join Asset aid on aid.Id = stat.AssetId
		{contains("atid")} join AssetType atid on atid.Id = stat.AssetTypeId
		{contains("did")} join Report did on did.Id = stat.DashboardId
		{contains("iid")} join Issue iid on iid.Id = stat.IssueId
		{contains("sid")} join Semantic sid on sid.Id = stat.SemanticId
		{contains("tid")} join Tag tid on tid.Id = stat.TagId ";

			string sql = $@"
select	r.uid as ResourceUid,
		r.FirstName,
		r.LastName,
		r.Email,
		stat.Timestamp,
		stat.Browser,
		stat.Language,
		stat.Locale,
		stat.[Action],
		aid.Uid as AssetUid,
		atid.Uid as AssetTypeUid,
		did.Uid as DashboardUid,
		iid.Uid as IssueUid,
		sid.Uid as SemanticUid,
		tid.Uid as TagUid,
		sidebar.Value as Sidebar,
		tab.Value as Tab,
		case when stat.AssetId is not null and aid.uid is null then 'Asset not found. It may have been removed.'
			when stat.AssetTypeId is not null and atid.uid is null then 'Asset Type not found. It may have been removed.'
			when stat.DashboardId is not null and did.uid is null then 'Dashboard not found. It may have been removed.'
			when stat.IssueId is not null and iid.uid is null then 'Action not found. It may have been removed.'
			when stat.SemanticId is not null and sid.uid is null then 'Semantic not found. It may have been removed.'
			when stat.TagId is not null and tid.uid is null then 'Tag not found. It may have been removed.'
		end as [Message]
{tableSql}
{whereClause}
{orderBySql}
{offsetSql}";

			string countSql = $"select count(*) {tableSql} {whereClause}";
			using (var connection = ConnectionProvider.Connect(true))
			{
				var results = (await connection.QueryAsync<dynamic>(sql, dbArgs)).ToList();
				if (includeTotal)
				{
					response.Data.total  = await connection.QueryFirstOrDefaultAsync<int>(countSql, dbArgs);
					response.Data.items = results;
				}
				else
				{
					response.Data.items = results;
				}
			}
			return response;
		}

		public async Task<RepositoryResponse<bool>> CreateUsageAsync(UsageEntry value,string ipAddress)
		{
			RepositoryResponse<bool> response = null;
			bool isValid = false;

			if (value.Language?.Length == 2 && value.Locale?.Length == 5)
			{
				if (value.AssetUid.HasValue)
				{
					value.AssetTypeUid = null;
					value.DashboardUid = null;
					value.IssueUid = null;
					value.SemanticUid = null;
					value.TagUid = null;
					isValid = true;
				}
				else if (value.AssetTypeUid.HasValue)
				{
					value.AssetUid = null;
					value.DashboardUid = null;
					value.IssueUid = null;
					value.SemanticUid = null;
					value.TagUid = null;
					isValid = true;
				}
				else if (value.DashboardUid.HasValue)
				{
					value.AssetUid = null;
					value.AssetTypeUid = null;
					value.IssueUid = null;
					value.SemanticUid = null;
					value.TagUid = null;
					isValid = true;
				}
				else if (value.IssueUid.HasValue)
				{
					value.AssetUid = null;
					value.AssetTypeUid = null;
					value.DashboardUid = null;
					value.SemanticUid = null;
					value.TagUid = null;
					isValid = true;
				}
				else if (value.SemanticUid.HasValue)
				{
					value.AssetUid = null;
					value.AssetTypeUid = null;
					value.DashboardUid = null;
					value.IssueUid = null;
					value.TagUid = null;
					isValid = true;
				}
				else if (value.TagUid.HasValue)
				{
					value.AssetUid = null;
					value.AssetTypeUid = null;
					value.DashboardUid = null;
					value.IssueUid = null;
					value.SemanticUid = null;
					isValid = true;
				}
			}

			if (isValid)
			{
				using (var connection = ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync(
					"exec [usage].[Add] @UserId, @Browser, @Action, @Timestamp, @Language, @Locale, @Ip, " +
					"@AssetUid, @AssetTypeUid, @DashboardUid, @IssueUid, @SemanticUid, @TagUid, " +
					"@Sidebar, @Tab", new
					{
						UserId = CurrentUserId,
						Browser = (int)value.Browser,
						Action = (int)value.Action,
						Timestamp = DateTime.UtcNow,
						value.Language,
						value.Locale,
						Ip = ipAddress,
						value.AssetUid,
						value.AssetTypeUid,
						value.DashboardUid,
						value.IssueUid,
						value.SemanticUid,
						value.TagUid,
						value.Sidebar,
						value.Tab
					});
				}
				response = new RepositoryResponse<bool>(true, 200, true);
			}
			else
			{
				response = new RepositoryResponse<bool>(false, 400, false);
			}
			return response;
		}
	}
}
