using System;
using System.Collections.Generic;
using System.Linq;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.resources;
using d360.model.helpers;
using d360.model.helpers.filters;
using Dapper;

namespace repositories.azure
{
	public abstract class Repository
	{
		public int CurrentUserId { get; set; }
		public int CompanyId { get; set; }
		public string CompanyPrefix { get; set; }
		public bool IsAdministrator { get; set; }

		public Platform Platform { get { return Platform.Azure; } }

		public DapperConnectionProvider ConnectionProvider { get; set; }

		protected Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;		
		}

		public void ParseAdvancedFilterQueryParameter(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fieldList, out DynamicParameters dbArgs, out List<string> whereStatements)
		{
			dbArgs = new DynamicParameters();
			whereStatements = new List<string>();

			if (queryParams.Any(x => x.Key.Equals("_filter", StringComparison.OrdinalIgnoreCase)))
			{
				string value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
				if (!string.IsNullOrEmpty(value))
				{
					FilterDataProvider filterDataProvider = new FilterDataProvider((d360.model.ICompanyContext)this);
					//FilterDataProvider filterDataProvider = new FilterDataProvider();
					FilterExpressionParser filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CustomFields, false);
					filterExpressionParser.OverrideAllowedDefaultFields(fieldList);
					Dictionary<string, object> sqlParams = new Dictionary<string, object>();
					List<int> filteredFields = new List<int>();
					whereStatements.Add(filterExpressionParser.Parse(value, out sqlParams, out filteredFields));

					foreach (KeyValuePair<string, object> item in sqlParams)
					{
						dbArgs.Add(item.Key, item.Value);
					}
				}
			}
		}
		public string ParseOrderColumn(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fields, string defaultColumn)
		{
			string column = defaultColumn;

			if (queryParams.Any(x => x.Key.Equals("_order", StringComparison.OrdinalIgnoreCase)))
			{
				string order = queryParams.FirstOrDefault(x => x.Key.Equals("_order", StringComparison.OrdinalIgnoreCase)).Value;

				DefaultFilter field = fields.FirstOrDefault(i => i.ApiName.Equals(order, StringComparison.OrdinalIgnoreCase));

				if (field == null)
				{
					throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.InvalidRequestHttpErrorTitle, Error.InvalidOrderPassed);
				}

				column = field.SqlExpression;
			}

			return column;
		}

		public string ParseOrderDirection(IEnumerable<KeyValuePair<string, string>> queryParams, string defaultDirection = "desc")
		{
			string direction = defaultDirection;

			if (queryParams.Any(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("_sort", StringComparison.OrdinalIgnoreCase)))
			{
				string[] allowedDirections = new string[] { "asc", "desc" };
				string order = queryParams.FirstOrDefault(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("_sort", StringComparison.OrdinalIgnoreCase)).Value;

				if (allowedDirections.Contains(order.Trim().ToLower()))
				{
					direction = order;
				}
				else
				{
					throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.InvalidRequestHttpErrorTitle, Error.InvalidDirection);
				}
			}

			return direction;
		}

		public int ParsePageNumber(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultPage = 1)
		{
			int size = defaultPage;

			if (queryParams.Any(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)))
			{
				if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)).Value, out size))
				{
					if (size < 1)
					{
						size = defaultPage;
					}
				}
			}

			return size;
		}

		public int ParsePageSize(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultSize = 250)
		{
			int size = defaultSize;

			if (queryParams.Any(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)))
			{
				if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)).Value, out size))
				{
					if (size < 1)
					{
						size = defaultSize;
					}
				}
			}

			return size;
		}

		public string ParsePageOffsetSql(int pageNumber, int pageSize, int pageSizeLimit = 10000)
		{
			string offset = "";

			if (pageSize > 0 || pageNumber > 0)
			{
				if (pageSize < 1)
				{
					pageSize = 1;
				}

				if (pageNumber < 1)
				{
					pageNumber = 1;
				}

				if (pageSize > pageSizeLimit)
				{
					pageSize = pageSizeLimit;
				}

				if (pageNumber > 10000)
				{
					pageNumber = 10000;
				}

				offset = $" offset {pageSize * (pageNumber - 1)} rows fetch next {pageSize} rows only ";
			}

			return offset;
		}

		public bool HasAssetTypePermission(string type, int id, Permission permission)
		{
			using (var connection = ConnectionProvider.Connect())
			{
				bool hasPermission = IsAdministrator;
				bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);


				if (!hasPermission)
				{
					if (isReadPermission)
					{
						hasPermission = HasAssetTypeReadPermission(id);
					}
					else
					{
						hasPermission = connection.QuerySingle<bool>($@"
																			declare @t int;
																			select @t = ID from AssetType where [Object] = @type and [ObjectID] = @id;

																			if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																				begin
																					select 1;
																				end				                                                                        
																			else
																				begin
																					select 0;
																				end", new { id, type, r = CurrentUserId });
					}
				}

				return hasPermission;
			}
			
		}

		public bool HasAssetPermission(string type, int id, Permission permission)
		{
			bool hasPermission = IsAdministrator;

			if (!hasPermission)
			{

				if (permission == Permission.ReadAsset)
				{
					hasPermission = HasAssetDefaultReadPermission(type, id);
				}
				else
				{
					int? assetTypeID = null;
					using (var connection = ConnectionProvider.Connect(true))
					{
						if (type.EndsWith("Type"))
						{
							assetTypeID = connection.Query<int>("select ID from AssetType where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();

						}
						else
						{
							assetTypeID = connection.Query<int?>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
						}

						if (assetTypeID.HasValue)
						{
							hasPermission = HasPermission(type, id, assetTypeID.Value, permission);
						}
					}
				}
			}

			return hasPermission;
		}

		private bool HasAssetDefaultReadPermission(string type, int id)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{ 
				bool hasPermission = IsAdministrator;
			if (!hasPermission)
			{
				int assetTypeID = connection.Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();


				if (assetTypeID <= 0)
				{
					return true; // objects not in asset table we grant permission               
				}

				hasPermission = HasUserReadPermission(type, id, assetTypeID, CurrentUserId);
			}

			return hasPermission;
		}
		}

		public bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{ 
				Permission permission = Permission.ReadAsset;

			return connection.QuerySingle<bool>($@"	if exists(select 1 
																		 from asset a
																		 cross apply UserAssetPermissionsByAssetID(@r, @t, a.id) ua
																		 where a.Object = @type and a.ObjectID = @id 
																		 and ua.PermissionsBitMask & {(int)permission} = 0)
																	begin
																		select 0;
																		end
																	else
																	begin
																		select 1;
																	end", new { type, id = objectId, t = assetTypeId, r = resourceId });
			}
		}

		private bool HasPermission(string type, int objectId, int assetTypeId, Permission permission)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				return connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																						begin
																							select 1;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua inner join asset a on(ua.AssetID = a.id and a.Object = @type and a.ObjectID = @id) where ua.PermissionsBitMask & {(int)permission} = {(int)permission})
																						begin
																							select 1;
																							end
																						else
																						begin
																							select 0;
																						end", new { type, id = objectId, t = assetTypeId, r = CurrentUserId });
			}
		}

		private bool HasAssetTypeReadPermission(int assetTypeId)
		{
			using (var connection = ConnectionProvider.Connect())
			{
				Permission permission = Permission.ReadAsset;

				return connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissionsByAssetID(@r,@t,0) ua where ua.PermissionsBitMask & {(int)permission} = 0)
																						begin
																							select 0;
																						end				                                                                        
																						else
																						begin
																							select 1;
																						end", new { t = assetTypeId, r = CurrentUserId });
			}
		}
	}
}
