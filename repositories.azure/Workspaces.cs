using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.resources;
using Dapper;
using DocumentFormat.OpenXml;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Workspaces : Repository, IWorkspaces
	{
		public int CompanyId { get; set; }
		
		public string WorkspaceId { get; set; }

		private readonly string GROUP_RESULTS_SQL = @"select ItemNumber, ExecutionItemUid, cast(JSON_VALUE(Properties, '$.Uid') as uniqueidentifier) as uid, Message, Success from api.ExecutionItem where ExecutionID = @executionId;";
		private readonly string FIELD_VALIDATION_COLUMNS = "f.ID, f.Name, f.Type, f.AllowMultipleValues, f.MinimumLength, f.MaximumLength, f.Length, f.Pattern, f.IsRequired";

		public Workspaces(DapperConnectionProvider provider): base(provider) { }


		FieldValidationResult isFieldValid(FieldTypeValidation ft, string value)
		{
			FieldValidationResult result;
			DataType type = (DataType)Enum.Parse(typeof(DataType), ft.Type);

			result = type.ValidateRestricted(ft.Name, ft.Type);
			if (!result.IsValid)
			{
				return result;
			}
			result = type.ValidateRequirement(ft.Name, ft.IsRequired, value);
			if (!result.IsValid)
			{
				return result;
			}

			switch (type)
			{
				case DataType.Boolean:
					result = type.ValidateBoolean(ft.Name, value);
					break;
				case DataType.Date:
					result = type.ValidateDate(ft.Name, value);
					break;
				case DataType.DateTime:
					result = type.ValidateDateTime(ft.Name, value);
					break;
				case DataType.Decimal:
					result = type.ValidateDecimal(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, value);
					break;
				case DataType.Html:
					result = type.ValidateText(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, ft.Pattern, value);
					break;
				case DataType.Lookup:
					result = type.ValidateList(ft.Name, ft.AllowMultipleValues, value);
					break;
				case DataType.Number:
					result = type.ValidateNumber(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, value);
					break;
				default:
					result = type.ValidateText(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, ft.Pattern, value);
					break;
			}

			if (result.IsValid && string.IsNullOrEmpty(result.CorrectedValue))
			{
				result.CorrectedValue = value;
			}

			return result;
		}

		(bool, List<string>) parseFieldAndAddToRow(DataRow row, List<FieldTypeValidation> fieldTypes, Dictionary<string, string> fields)
		{
			var jsonArray = JArray.Parse("[]");
			bool fieldsAreValid = true;
			List<string> validationMessages = [];
			foreach (var key in fields.Keys)
			{
				var ft = fieldTypes.FirstOrDefault(o => o.Name == key.Trim());
				if (ft != null)
				{
					var validationResult = isFieldValid(ft, (fields[key] ?? "").Trim());
					if (validationResult.IsValid)
					{
						var jsonObject = JObject.Parse("{}");

						jsonObject.Add("FieldName", key.Trim());
						jsonObject.Add("FieldValue", validationResult.CorrectedValue);
						jsonObject.Add("FieldTypeID", ft.ID);

						jsonArray.Add(jsonObject);
					}
					else
					{
						fieldsAreValid = false;
						validationMessages.Add(validationResult.Message);
					}
				}
			}
			row["CustomProperties"] = jsonArray.ToString();

			return (fieldsAreValid, validationMessages);
		}


		public async Task<RepositoryResponse<bool>> AddMembersToGroupAsync(Guid groupUid, List<Guid> userUids)
		{
			RepositoryResponse<bool> response = null;

			using (var connection = ConnectionProvider.Connect())
			{
				int? groupId = await connection.QueryFirstOrDefaultAsync<int>("select ID from [Group] where Uid = @groupUid", new { groupUid });
				if (groupId == null)
				{
					response = new(404, Error.GroupUidNotExists);
				}

				List<int> userIds = null;
				if (response == null)
				{
					userIds = (await connection.QueryAsync<int>(
						"select ResourceID from reporting.Global_Resource where Uid in @userUids", 
						new { userUids })).ToList();
					if (userIds.Count != userUids.Count)
					{
						response = new(404, Error.InvalidUserUids);
					}				
				}

				if (response == null)
				{
					var rowsUpdated = await connection.ExecuteAsync(@"
declare @date datetime = getutcdate();
declare @notPresentUserIds table(ID int);

insert into @notPresentUserIds
	select	t.[value] as ID 
	from	(select  ResourceID as [value] from reporting.Global_Resource where ResourceID in @userIds) t
			left join ResourceGroup s on s.GroupID = @groupId and s.ResourceID = t.[value]
	where	s.GroupID is null;

insert into ResourceGroup (GroupID, ResourceID)
	select	@groupId as GroupID,
			t.ID as ResourceID
	from	@notPresentUserIds t;

insert into reporting.Global_Audit
	(Object, ObjectID, ObjectName, ResourceID, [Date], [Action], ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version])
select	distinct 
		'Group', g.ID, G.Name, @CurrentUserId, @date, 'Added', 'Group', g.ID, 'Group', G.Name,'[' + gr.FirstName + ' ' + gr.LastName + '] added to the group.', mv.[Version]
from	[Group] g 
		inner join @notPresentUserIds npu on g.id = npu.id
		inner join ResourceGroup rg on rg.groupid = g.id
		inner join reporting.Global_Resource gr on gr.ResourceID = rg.ResourceID
		cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = 'Group' and ObjectID = g.ID) mv
where	g.id = @groupId;
", new { groupId, userIds, CurrentUserId });
					response = new RepositoryResponse<bool>(true, 200, true);
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadGroupsAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new RepositoryResponse<PagedApiBaseViewModel<dynamic>>(new PagedApiBaseViewModel<dynamic>(), 200, true);
			
			var dbArgs = new DynamicParameters();
			var queryFilters = new List<string>();
			var validOrderFields = new List<SortColumnOption> {
				new SortColumnOption("Name", "G.Name"),
				new SortColumnOption("PrimaryOwnerUid", "gr1.uid"),
				new SortColumnOption("SecondaryOwnerUid", "gr2.uid"),
				new SortColumnOption("IsActiveDirectoryGroup", "G.IsActiveDirectoryGroup")
			};
			response.Data.pageNum = queryParams.CheckForPageNumber();
			response.Data.pageSize = queryParams.CheckForPageSize();

			bool isUidValid = queryParams.CheckForQueryParameter<Guid>("uid", "g.Uid", "@uid", ref dbArgs, ref queryFilters);
			if (!isUidValid)
			{
				return new(400, "The Uid provided is invalid.");
			}
			queryParams.CheckForQueryParameter<string>("name", "g.Name", "@name", ref dbArgs, ref queryFilters);
			if (queryParams.Any(q => q.Key.ToLower() == "resourceuid"))
			{
				var _resourceUid = queryParams.FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value.Trim();
				if (!string.IsNullOrEmpty(_resourceUid))
				{
					Guid resourceUid;
					if (Guid.TryParse(_resourceUid, out resourceUid))
					{
						queryFilters.Add(@"exists(select 1 from ResourceGroup rg inner join reporting.Global_Resource r on r.ResourceID = rg.ResourceID and r.Uid = @resourceUid and rg.GroupID = G.ID)");
						dbArgs.Add("@resourceUid", resourceUid);
					}
					else
					{
						return new(400, "The ResourceUid provided is invalid.");
					}
				}
			}
			string simpleFilter = null;
			var simpleQueryFilters = new List<string>();
			if (queryParams.Any(q => q.Key.ToLower() == "_simplefilter"))
			{
				simpleFilter = queryParams.FirstOrDefault(q => q.Key.ToLower() == "_simplefilter").Value.Trim();
				if (!string.IsNullOrEmpty(simpleFilter))
				{
					simpleQueryFilters.Add(@"g.Name like @simpleFilter");
					dbArgs.Add("@simpleFilter", "%" + simpleFilter + "%");
				}
			}

			// _simpleFilter checks Name, Description,

			List<FieldType> fieldTypes = null;
			using (var connection = ConnectionProvider.Connect(true))
			{
				fieldTypes = (await connection.QueryAsync<FieldType>("select ft.* from fieldtype ft inner join assettype at on ft.assettypeid = at.id where at.Object = 'GroupType' and at.ObjectID = 1")).ToList();
			}

			List<string> fieldColumns = ["G.Uid", "G.Name", "G.Description", "gr1.uid as PrimaryOwnerUid", "gr2.uid as SecondaryOwnerUid", "G.IsActiveDirectoryGroup"];
			List<string> fieldJoins = [];
			if (fieldTypes.Count > 0)
			{
				fieldTypes.ForEach(ft =>
				{
					var prefix = $"f_{ft.ID}";
					DataType dt = (DataType)Enum.Parse(typeof(DataType), ft.Type);
					if (dt == DataType.Lookup)
					{
						validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
						fieldColumns.Add($"{prefix}.FormattedValue as [{ft.Name}]");
						fieldJoins.Add($"left join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.[ObjectType] = 'Group' and {prefix}.ObjectID = G.ID)");
					}
					else
					{
						string sqlDataType = dt.AsSqlDataType();
						validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
						fieldColumns.Add($"try_cast(case when LEN(ISNULL({prefix}.FormattedValue, '')) < 1 then null else {prefix}.FormattedValue end as {sqlDataType}) as [{ft.Name}]");
						fieldJoins.Add($"left join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.[ObjectType] = 'Group' and {prefix}.ObjectID = G.ID)");
					}
					if (!string.IsNullOrEmpty(simpleFilter) && ft.IsListable)
					{
						simpleQueryFilters.Add($"{prefix}.FormattedValue like @simpleFilter");
					}
				});
			}
			
			var countSql = $@"select count(1) from [Group] g";
			
			if (simpleQueryFilters.Count > 0)
			{
				queryFilters.Add(string.Join(" or ", simpleQueryFilters));
				countSql += $" {string.Join("\n", fieldJoins)}";
			}

			

			var sql = $@"
select	{string.Join(", ", fieldColumns)}
from	[Group] G
		left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
		left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
		{string.Join("\n", fieldJoins)}";

			if (queryFilters.Count > 0)
			{
				var whereSql = " where " + string.Join(" and ", queryFilters);
				sql += whereSql;
				countSql += whereSql;
			}

			var orderColumn = queryParams.CheckForSortColumn(validOrderFields, "G.Name");
			var direction = queryParams.CheckForSortDirection();
			sql += $" order by {orderColumn} {direction}";
			sql += $" offset {response.Data.pageSize * (response.Data.pageNum-1)} rows fetch next {response.Data.pageSize} rows only";

			using (var connection = ConnectionProvider.Connect())
			{
				response.Data.total = await connection.QuerySingleAsync<int>(countSql, dbArgs);
				var results = (await connection.QueryAsync<dynamic>(sql, dbArgs)).ToList();
				response.Data.items = results;
			}
			
			return response;
		}

		public async Task<IEnumerable<CompanyRebuildJobStatus>> ReadRebuildStatusesAsync()
		{
			IEnumerable<CompanyRebuildJobStatus> response = null;
			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				response = await connection.QueryAsync<CompanyRebuildJobStatus>("select * from RebuildJobStatus");
			}
			foreach (var job in response)
			{
				// If the job is older than 12 hours, just return it as inactive.
				if (job.State == CompanyRebuildJobStatusState.Active && job.LastStartedOn <= DateTime.UtcNow.AddHours(-12))
				{
					job.State = CompanyRebuildJobStatusState.Inactive;
				}
			}

			return response;
		}

		public async Task<bool> RemoveFavoritesAsync(int resourceId, List<int> favoriteIds)
		{
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				await connection.ExecuteAsync(@"delete Favorite where ResourceID = @resourceId and ID in @favoriteIds", new { resourceId, favoriteIds });
			}

			return true;
		}


		public async Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> RemoveGroupsAsync(int executionId, List<Guid> uids)
		{
			RepositoryResponse<IEnumerable<GroupResponseResult>> response = new(null, 200, true);

			#region Data Tables

			var table = new DataTable();

			table.Columns.Add("ExecutionId", typeof(int));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Properties", typeof(string));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			uids.ForEach(u => {
				var row = table.NewRow();
				var jsonObject = JObject.Parse("{}");

				itemNumber++;
				row["ExecutionId"] = executionId;
				row["ItemNumber"] = itemNumber;
				jsonObject.Add("Uid", u);
				row["Properties"] = jsonObject.ToString();

				table.Rows.Add(row);
			});

			SqlBulkCopy bulkCopy = null;
			var UpdatedOn = DateTime.UtcNow;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				bulkCopy = connection.CreateBulkCopy("api.ExecutionItem", 1000, 1200);
				bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
				bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulkCopy.ColumnMappings.Add("Properties", "Properties");
				await bulkCopy.WriteToServerAsync(table);

				await connection.ExecuteAsync(@"exec api.DeleteGroups @executionId", new { executionId });

				response.Data = (await connection.QueryAsync<GroupResponseResult>(GROUP_RESULTS_SQL, new { executionId })).ToList();
			}

			return response;
		}

		public async Task<bool> RemoveMemberFromGroupAsync(Guid groupUid, Guid userUid)
		{
			string sql = @"
declare @userId int,
		@groupId int;
select @groupId = ID from [Group] where Uid = @groupUid;
select @userId = ResourceID from reporting.Global_Resource where Uid = @userUid;

if exists(select 1 from ResourceGroup where GroupID = @groupId and ResourceID = @userId)
begin
	update [Group] set PrimaryOwnerResourceID = null where ID = @groupId and PrimaryOwnerResourceID = @userId;
	update [Group] set SecondaryOwnerResourceID = null where ID = @groupId and SecondaryOwnerResourceID = @userId;
	delete ResourceGroup where GroupID = @groupId and ResourceID = @userId;

	insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, [date], [Action], ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version])
		select	distinct 
				'Group', 
				g.ID, 
				G.Name, 
				@CurrentUserId, 
				GETUTCDATE(), 
				'Removed', 
				'Group', 
				g.ID, 
				'Group', 
				G.Name,
				'[' + gr.FirstName + ' ' + gr.LastName + '] removed from the group.',
				mv.[Version]
		from	[Group] g 
				inner join reporting.Global_Resource gr on gr.ResourceID = @userId
				cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = 'Group' and ObjectID = g.ID) mv
		where	g.ID = @groupId
end";

			bool response;
			using (var connection = ConnectionProvider.Connect())
			{
				var rowsUpdated = await connection.ExecuteAsync(sql, new { groupUid, userUid, CurrentUserId });
				response = (rowsUpdated > 0);
			}
			return response;
		}


		public async Task<RepositoryResponse<int>> RemoveUsersAsync(int executionId, List<Guid> uids)
		{
			RepositoryResponse<int> response = new(0, 200, true);

			#region Data Tables

			var table = new DataTable();

			table.Columns.Add("ExecutionId", typeof(int));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Properties", typeof(string));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			uids.ForEach(u => {
				var row = table.NewRow();
				var jsonObject = JObject.Parse("{}");

				itemNumber++;
				row["ExecutionId"] = executionId;
				row["ItemNumber"] = itemNumber;
				jsonObject.Add("Uid", u);
				row["Properties"] = jsonObject.ToString();

				table.Rows.Add(row);
			});

			SqlBulkCopy bulkCopy = null;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				bulkCopy = connection.CreateBulkCopy("api.ExecutionItem", 1000, 1200);
				bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
				bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulkCopy.ColumnMappings.Add("Properties", "Properties");
				await bulkCopy.WriteToServerAsync(table);

				var result = await connection.QueryFirstOrDefaultAsync<dynamic>(@"exec api.DeleteUsers @executionId", new { executionId });
				
				if (result != null)
				{
					if ( result.IsError == true)
					{
						if (!string.IsNullOrEmpty(result.ErrMessage) )
						{
							if ( result.ErrMessage.Contains("not found"))
							{
								response = new(404, result.ErrMessage);
							}
							else
							{
								response = new(400, result.ErrMessage);
							}
						}

					}
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<List<GroupResponseResult>>> UpsertGroupsAsync(int executionId, List<UpdateGroupModel> items, bool isInsert, bool lookupFieldsPassedByValue = false)
		{
			RepositoryResponse<List<GroupResponseResult>> response = new([], 200, true);

			List<FieldTypeValidation> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<FieldTypeValidation>(
					$"select {FIELD_VALIDATION_COLUMNS} from FieldType f inner join AssetType a on a.Object = 'GroupType' and a.ObjectID = 1 and f.AssetTypeID = a.ID"
					)).ToList();
			}

			#region Data Tables

			var table = new DataTable();

			table.Columns.Add("ExecutionId", typeof(int));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Properties", typeof(string));
			table.Columns.Add("CustomProperties", typeof(string));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			items.ForEach(u => {
				var row = table.NewRow();
				var jsonObject = JObject.Parse("{}");

				itemNumber++;
				row["ExecutionId"] = executionId;
				row["ItemNumber"] = itemNumber;

				if (u.Uid.HasValue && u.Uid != Guid.Empty)
				{
					jsonObject.Add("Uid", u.Uid.Value);
				}
				jsonObject.Add("Name", u.Name);
				jsonObject.Add("Description", u.Description);
				jsonObject.Add("IsActiveDirectoryGroup", u.IsActiveDirectoryGroup);

				if (u.PrimaryOwnerUid.HasValue && u.PrimaryOwnerUid != Guid.Empty) 
				{
					jsonObject.Add("PrimaryOwnerUid", u.PrimaryOwnerUid);
				}
				if (u.SecondaryOwnerUid.HasValue && u.SecondaryOwnerUid != Guid.Empty)
				{
					jsonObject.Add("SecondaryOwnerUid", u.SecondaryOwnerUid);
				}
				row["Properties"] = jsonObject.ToString();
				var fieldProcessingResult = parseFieldAndAddToRow(row, fieldTypes, u.Fields);
				
				if (fieldProcessingResult.Item1)
				{
					table.Rows.Add(row);
				}
				else
				{	// Add error to outgoing.
					response.Data.Add(new GroupResponseResult { ItemNumber = itemNumber, Message = string.Join("; ", fieldProcessingResult.Item2), Success = false });
				}
			});

			if (table.Rows.Count > 0)
			{ 
				SqlBulkCopy bulkCopy = null;
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					connection.Open();
					bulkCopy = connection.CreateBulkCopy("api.ExecutionItem", 1000, 1200);
					bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Properties", "Properties");
					bulkCopy.ColumnMappings.Add("CustomProperties", "CustomProperties");
					await bulkCopy.WriteToServerAsync(table);

					await connection.ExecuteAsync(@"exec api.UpsertGroups @executionId, @lookupFieldsPassedByValue", new { executionId, lookupFieldsPassedByValue });

					response.Data.AddRange(await connection.QueryAsync<GroupResponseResult>(GROUP_RESULTS_SQL, new { executionId }));
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpsertRebuildStatusAsync(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state, int timeOutInHours)
		{
			var response = new RepositoryResponse<bool>(true, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				string sql = @"select * from RebuildJobStatus where JobToken = @jobToken";
				var jobStatus = await connection.QueryFirstOrDefaultAsync<CompanyRebuildJobStatus>(sql, new { jobToken = (int)jobToken });

				if (jobStatus != null)
				{
					if (
						jobStatus.State == CompanyRebuildJobStatusState.Active && 
						jobStatus.LastStartedOn > DateTime.UtcNow.AddHours(-timeOutInHours) && 
						state == CompanyRebuildJobStatusState.Active)
					{
						response = new(409, OthersError.JobinActiveState);
					}
					else 
					{
						if (state == CompanyRebuildJobStatusState.Active)
						{
							sql =	"update RebuildJobStatus " +
									"set LastStartedOn = getutcdate(), LastStartedBy = @CurrentUserId, LastCompletedOn = null, State = @state " +
									"where JobToken = @jobToken";
							await connection.ExecuteAsync(sql, new { jobToken = (int)jobToken, CurrentUserId, state = (int)state });
						}
						else 
						{
							sql =	"update RebuildJobStatus " +
									"set LastCompletedOn = getutcdate(), State = @state " +
									"where JobToken = @jobToken";
							await connection.ExecuteAsync(sql, new { jobToken = (int)jobToken, CurrentUserId, state = (int)state });
						}
					}
				}
				else 
				{
					if (state == CompanyRebuildJobStatusState.Inactive)
					{
						response = new(409, OthersError.JobIsNotRunning);
					}
					else 
					{
						sql = "insert into RebuildJobStatus (JobToken, LastStartedBy, LastStartedOn, State) values (@jobToken, @CurrentUserId, getutcdate(), @state)";
						await connection.ExecuteAsync(sql, new { jobToken = (int)jobToken, CurrentUserId, state = (int)state });
					}
				}
			}

			return response;
		}


		public async Task<RepositoryResponse<List<UserApiUpsertResult>>> UpsertUsersAsync(int executionId, List<UserUpsertValidateModel> users, bool lookupFieldsPassedByValue = false)
		{
			RepositoryResponse<List<UserApiUpsertResult>> response = new([], 200, true);

			List<FieldTypeValidation> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<FieldTypeValidation>(
					$"select {FIELD_VALIDATION_COLUMNS} from FieldType f inner join AssetType a on a.Object = 'ResourceType' and a.ObjectID = 1 and f.AssetTypeID = a.ID"
					)).ToList();
			}

			#region Data Tables

			var table = new DataTable();

			table.Columns.Add("ExecutionId", typeof(int));
			table.Columns.Add("ExecutionItemUid", typeof(Guid));
			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Properties", typeof(string));
			table.Columns.Add("CustomProperties", typeof(string));
			table.Columns.Add("Success", typeof(bool));
			table.Columns.Add("Message", typeof(string));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			users.ForEach(u => {
				var row = table.NewRow();
				var jsonObject = JObject.Parse("{}");

				itemNumber++;
				row["ExecutionId"] = executionId;
				row["ItemNumber"] = itemNumber;

				if (u.users.ExecutionItemUid.HasValue)
				{
					row["ExecutionItemUid"] = u.users.ExecutionItemUid.Value;
				}

				if (u.users.uid.HasValue && u.users.uid != Guid.Empty)
				{
					jsonObject.Add("Uid", u.users.uid.Value);
				}
				jsonObject.Add("ObjectID", u.users.ResourceID);
				jsonObject.Add("Username", u.users.Username);
				jsonObject.Add("Email", u.users.Email);
				jsonObject.Add("FirstName", u.users.FirstName);
				jsonObject.Add("LastName", u.users.LastName);
				jsonObject.Add("State", (int)(u.users.State ?? CompanyResourceState.Active));
				jsonObject.Add("IsAdministrator", u.users.IsAdministrator);

				row["Properties"] = jsonObject.ToString();
				var fieldProcessingResult = parseFieldAndAddToRow(row, fieldTypes, u.users.Fields);

				var message = "";
				if (u.Success != null)
				{
					message = u.Message;
				}
				if (!fieldProcessingResult.Item1)
				{
					u.Success = false;
					message += string.Join("; ", fieldProcessingResult.Item2);
				}

				if (u.Success == null || u.Success == true)
				{
					row["Success"] = u.Success ?? (object)DBNull.Value;
					row["Message"] = u.Message;
					table.Rows.Add(row);
				}
				else
				{   // Add error to outgoing.
					response.Data.Add(new UserApiUpsertResult { ItemNumber = itemNumber, Message = message, Success = false });
				}
			});

			if (table.Rows.Count > 0)
			{ 
				SqlBulkCopy bulkCopy = null;

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					connection.Open();
					bulkCopy = connection.CreateBulkCopy("api.ExecutionItem", 1000, 1200);
					bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
					bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Success", "Success");
					bulkCopy.ColumnMappings.Add("Message", "Message");
					bulkCopy.ColumnMappings.Add("Properties", "Properties");
					bulkCopy.ColumnMappings.Add("CustomProperties", "CustomProperties");
					await bulkCopy.WriteToServerAsync(table);

					await connection.ExecuteAsync(@"exec api.UpsertUsers @executionId, @lookupFieldsPassedByValue", new { executionId, lookupFieldsPassedByValue });

					response.Data.AddRange(await connection.QueryAsync<UserApiUpsertResult>(GROUP_RESULTS_SQL, new { executionId }));
				}			
			}

			return response;
		}
		public async Task<List<UserUpsertValidateModel>> ValidateUserData(List<UserApiModel> users, bool isNew, bool IsAdministrator, bool lookupFieldsPassedByValue)
		{
			List<UserUpsertValidateModel> usersvalidate = new List<UserUpsertValidateModel>();
			UserUpsertValidateModel usersvalidaterow = new UserUpsertValidateModel();

			int itemNumber = 0;
			List<FieldTypeValidation> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				string sql = $@"SELECT {FIELD_VALIDATION_COLUMNS} 
							FROM [dbo].[FieldType] F 
							inner join AssetType Att on F.AssetTypeID = ATT.ID and ATT.Class = {(int)AssetTypeClass.User}";
				fieldTypes = (await connection.QueryAsync<FieldTypeValidation>(sql)).ToList();

			}

			foreach (var user in users)
			{
				var success = true;
				var messages = new List<string>();

				UserApiModel userrow = new UserApiModel();

				itemNumber++;
				userrow.ItemNumber = itemNumber;
				userrow.Username = user.Username;
				userrow.Email = user.Email;
				userrow.FirstName = user.FirstName;
				userrow.LastName = user.LastName;
				userrow.IsAdministrator = user.IsAdministrator;
				userrow.State = user.State ?? CompanyResourceState.Active;
				userrow.Fields = user.Fields;
				userrow.Password = user.Password;
				userrow.ResourceID = user.ResourceID;
				userrow.IsNew = user.IsNew;
				if (!user.ResourceID.HasValue)
				{
					if (string.IsNullOrEmpty(userrow.Password))
					{
						userrow.Password = PasswordHelper.CreateRandomPassword();
					}
				}

				if (!user.uid.HasValue || (user.uid.HasValue && user.uid == Guid.Empty))
				{
					user.uid = Guid.NewGuid();
				}

				userrow.uid = user.uid;

				#region "Validatation"
				if (!string.IsNullOrEmpty(user.Password))
				{
					if (string.IsNullOrEmpty(user.Password)
						|| user.Password.Length < 7 || user.Password.Length > 25
						|| !user.Password.Any(char.IsUpper) || !user.Password.Any(char.IsLower)
						|| !user.Password.Any(char.IsDigit))
					{
						success = false;
						messages.Add(MemberShipErrors.PasswordRule);
					}
				}
				if (string.IsNullOrEmpty(user.FirstName))
				{
					success = false;
					messages.Add(MemberShipErrors.FirstNameMissing);
				}

				if (string.IsNullOrEmpty(user.LastName))
				{
					success = false;
					messages.Add(MemberShipErrors.LastNameMissing);
				}

				if (user.FirstName != null && user.FirstName.Length > 250)
				{
					success = false;
					messages.Add(MemberShipErrors.FirstNameTooLong);
				}

				if (user.LastName != null && user.LastName.Length > 250)
				{
					success = false;
					messages.Add(MemberShipErrors.LastNameTooLong);
				}

				if (user.IsAdministrator && !IsAdministrator)
				{
					success = false;
					messages.Add("Non-administrator users cannot update the administrator flag.");
				}

				if (string.IsNullOrEmpty(user.Username) || !Regex.IsMatch(user.Username + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
				{
					success = false;
					messages.Add(MemberShipErrors.InvalidEmail);
				}
				else if (users.Count(u => u.Username.Trim().Equals(user.Username.Trim(), StringComparison.InvariantCultureIgnoreCase)) > 1)
				{
					success = false;
					messages.Add(MemberShipErrors.UsernameDuplicate);
				}

				if (user.Username != user.Email && (string.IsNullOrEmpty(user.Email) || !Regex.IsMatch(user.Email + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b")))
				{
					success = false;
					messages.Add(MemberShipErrors.InvalidEmail);
				}
				else if (user.Username != user.Email && (users.Count(u => u.Email.Trim().Equals(user.Email.Trim(), StringComparison.InvariantCultureIgnoreCase)) > 1))
				{
					success = false;
					messages.Add(MemberShipErrors.UsernameDuplicate);
				}

				if (user.Fields != null)
				{
					if (fieldTypes.Count == 0)
					{
						success = false;
						messages.Add(string.Format(MemberShipErrors.FieldTypeKeyNotFound, "No Custom Field Defined, But passed in payload"));
					}
					else
					{
						foreach (var field in user.Fields.Keys)
						{
							var fieldType = fieldTypes.FirstOrDefault(f => f.Name == field.Trim());

							if (fieldType == null)
							{
								success = false;
								messages.Add(string.Format(MemberShipErrors.FieldTypeKeyNotFound, field));
							}
							else
							{
								var validationResult = isFieldValid(fieldType, (user.Fields[field] ?? "").Trim());
								if (!validationResult.IsValid)
								{
									success = false;
									messages.Add(validationResult.Message);
								}
							}
						}
					}
				}

				#endregion
				usersvalidaterow.users = userrow;
				if (!success)
				{
					usersvalidaterow.Success = false;
				}


				usersvalidaterow.Message = messages.Any() ? string.Join(". ", messages) + ". " : "";

				usersvalidate.Add(usersvalidaterow);
			}

			//Store data into temp data

			#region Data Tables

			var userTable = new DataTable();
			var fieldTable = new DataTable();
			DateTime dtnow = DateTime.Now;

			userTable.Columns.Add("uid", typeof(Guid));
			userTable.Columns.Add("ResourceID", typeof(int));
			userTable.Columns.Add("ItemNumber", typeof(int));
			userTable.Columns.Add("Username", typeof(string));
			userTable.Columns.Add("FirstName", typeof(string));
			userTable.Columns.Add("LastName", typeof(string));
			userTable.Columns.Add("Password", typeof(string));
			userTable.Columns.Add("State", typeof(int));
			userTable.Columns.Add("IsAdministrator", typeof(bool));
			userTable.Columns.Add("IsNew", typeof(bool));
			userTable.Columns.Add("Success", typeof(bool));
			userTable.Columns.Add("Message", typeof(string));

			fieldTable.Columns.Add("ItemNumber", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));
			fieldTable.Columns.Add("LookupValue", typeof(string));

			foreach (var user in usersvalidate)
			{
				var row = userTable.NewRow();
				row["uid"] = user.users.uid;
				row["ResourceID"] = user.users.ResourceID ?? (object)DBNull.Value;
				row["ItemNumber"] = user.users.ItemNumber;
				row["Username"] = user.users.Username;
				row["FirstName"] = user.users.FirstName ?? (object)DBNull.Value;
				row["LastName"] = user.users.LastName ?? (object)DBNull.Value;
				row["Password"] = user.users.Password ?? (object)DBNull.Value;
				row["State"] = user.users.State;
				row["IsAdministrator"] = user.users.IsAdministrator;
				row["IsNew"] = user.users.IsNew;
				row["Success"] = user.Success ?? (object)DBNull.Value;
				row["Message"] = user.Message ?? "";
				userTable.Rows.Add(row);

				if (user.users.Fields != null)
				{
					foreach (var field in user.users.Fields.Keys)
					{
						var fieldRow = fieldTable.NewRow();
						fieldRow["ItemNumber"] = user.users.ItemNumber;
						fieldRow["FieldName"] = field ?? (object)DBNull.Value;
						fieldRow["FieldValue"] = user.users.Fields[field] ?? (object)DBNull.Value;
						var fieldType = fieldTypes.FirstOrDefault(f => f.Name == field);
						if (fieldType != null)
						{
							fieldRow["FieldTypeID"] = fieldType.ID;
						}
						else
						{
							fieldRow["FieldTypeID"] = (object)DBNull.Value;
						}
						fieldRow["LookupValue"] = (object)DBNull.Value;

						fieldTable.Rows.Add(fieldRow);
					}
				}
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					await connection.ExecuteAsync(
						sql: @"IF OBJECT_ID('tempdb..#TempUser') IS NOT NULL
									DROP TABLE #TempUser;

									CREATE TABLE #TempUser(
										ItemNumber int,
										[uid] uniqueidentifier,
										[ResourceID] [int],
										[Username] [nvarchar](250),
										[FirstName] [nvarchar](250),
										[LastName] [nvarchar](250),
										[Password] [nvarchar](50) MASKED WITH (FUNCTION = 'default()'),
										[State] [int],
										[IsAdministrator] [bit],
										[IsNew] [bit],
										[Success] [bit],
										[Message] [nvarchar](4000) not null,
										CONSTRAINT [PK_TempUser] PRIMARY KEY CLUSTERED ([ItemNumber] ASC )
									);

									IF OBJECT_ID('tempdb..#TempUserField') IS NOT NULL
									DROP TABLE #TempUserField;

									CREATE TABLE #TempUserField(
											ItemNumber int,
											[FieldName] [nvarchar](250),
											[FieldValue] [nvarchar](max),
											[LookupValue] [nvarchar](max),
											[FieldTypeID] [int],
											CONSTRAINT [PK_TempUserField] PRIMARY KEY CLUSTERED ([ItemNumber] ASC)
									);"
					,
						transaction: trans);
					try
					{
						var bulkCopy = connection.CreateBulkCopy("#TempUser", 1000, 1200, trans);

						bulkCopy.ColumnMappings.Add("uid", "uid");
						bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("Username", "Username");
						bulkCopy.ColumnMappings.Add("Password", "Password");
						bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
						bulkCopy.ColumnMappings.Add("LastName", "LastName");
						bulkCopy.ColumnMappings.Add("State", "State");
						bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
						bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
						bulkCopy.ColumnMappings.Add("Success", "Success");
						bulkCopy.ColumnMappings.Add("Message", "Message");

						await bulkCopy.WriteToServerAsync(userTable);

						bulkCopy = connection.CreateBulkCopy("#TempUserField", 1000, 1200, trans);

						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
						bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
						bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
						bulkCopy.ColumnMappings.Add("LookupValue", "LookupValue");

						await bulkCopy.WriteToServerAsync(fieldTable);

						await connection.ExecuteAsync(@"exec api.ValidateUser @lookupFieldsPassedByValue", new { lookupFieldsPassedByValue }, trans);

						trans.Commit();
					}
					catch (Exception)
					{
						try
						{
							if (trans != null)
							{
								trans.Rollback();
							}
						}
						catch
						{
							//  Block of code to handle errors
						}

						throw;
					}

					var sql = $@"select ItemNumber,Success,Message
								 from #TempUser
								 where coalesce(Success,1) = 0";

					var result = (await connection.QueryAsync<dynamic>(sql)).ToList();
					foreach (var user in usersvalidate)
					{
						var userrow = result.FirstOrDefault(f => f.ItemNumber == user.users.ItemNumber);

						if (userrow != null)
						{
							if (!userrow.Success)
							{
								user.Success = userrow.Success;
								user.Message = userrow.Message;
							}
						}
					}

				}
				#endregion
			}
			return usersvalidate;
		}
	}
}
