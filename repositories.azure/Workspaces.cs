using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
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

		public Workspaces(DapperConnectionProvider provider): base(provider) { }

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

			queryParams.CheckForQueryParameter<Guid>("uid", "g.Uid", "@uid", ref dbArgs, ref queryFilters);
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
					if (ft.Type == "Lookup")
					{
						validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
						fieldColumns.Add($"{prefix}.FormattedValue as [{ft.Name}]");
						fieldJoins.Add($"left join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.[ObjectType] = 'Group' and {prefix}.ObjectID = G.ID)");
					}
					else 
					{
						validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
						fieldColumns.Add($"{prefix}.FormattedValue as [{ft.Name}]");
						fieldJoins.Add($"left join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.[ObjectType] = 'Group' and {prefix}.ObjectID = G.ID)");					
					}
				});
			}

			var countSql = $@"select count(1) from [Group] g";

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

		public async Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync()
		{
			return (await ReadSettingsAsync()).ToDictionary(k => k.ID.ToString(), v => v.Value);
		}

		public async Task<SettingInfo> ReadSettingAsync(Setting setting)
		{
			string sql = "select * from Setting where ID = @id";
			var model = setting.AsInfoModel();
			dynamic @override;
			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				@override = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { id = (int)setting });
			}

			if (@override != null)
			{
				if (@override.Value == "True" || @override.Value == "False")
				{
					@override.Value = @override.Value.ToLowerInvariant();
				}
				model.Value = @override.Value;
			}
			else
			{
				model.Value = model.DefaultValue;
			}

			return model;
		}

		public async Task<List<SettingInfo>> ReadSettingsAsync()
		{
			// Get the list of settings from the D3S_###.dbo.Setting table.
			// Get the full list of settings from the Setting enum.
			// Return a list of SettingInfo, merging the values present from the environment into the SettingInfo.Value property.

			List<dynamic> overrides;
			string sql = "select * from Setting";
			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				overrides = (await connection.QueryAsync<dynamic>(sql)).ToList();
			}

			List<SettingInfo> settings = [.. Setting.ActionMessage.GetAsList().OrderBy(s => (int)s.ID)];

			settings.ForEach(s =>
			{
				string defaultValue = s.DefaultValue;

				if (defaultValue == "True" || defaultValue == "False")
				{
					defaultValue = defaultValue.ToLowerInvariant();
				}

				if (overrides.Any(o => o.ID == (int)s.ID))
				{
					s.Value = overrides.First(o => o.ID == (int)s.ID).Value;

					if (s.Value == "True" || s.Value == "False")
					{
						s.Value = s.Value.ToLowerInvariant();
					}
				}
				else
				{
					s.Value = defaultValue;
				}
			});

			return settings;
		}

		public async Task<T> ReadSettingValueAsync<T>(Setting setting)
		{
			SettingInfo info = await ReadSettingAsync(setting);

			var checkType = default(T);

			if (checkType is Guid)
			{
				Guid guid = Guid.Parse(info.Value);

				return (T)Convert.ChangeType(guid, typeof(T));
			}

			return (T)Convert.ChangeType(info.Value, typeof(T));
		}

		public async Task<bool> RemoveGroupsAsync(List<Guid> uids)
		{
			string sql = @"
declare @ids table(ID int, Uid uniqueidentifier);
insert into @ids 
	select ID, Uid from [Group] where Uid in @uids;

insert into reporting.Global_Audit 
	(
	Object, ObjectID, ObjectName, 
	ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription, 
	[Version]
	)
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
			'',
			mv.[Version]
	from	[Group] g 
			cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = 'Group' and ObjectID = g.ID) mv
	where	g.ID in (select ID from @ids);

delete ResourceGroup where GroupID in (select ID from @ids);
delete Field where ObjectType = 'Group' and ObjectID in (select ID from @ids);
delete Asset where Object = 'Group' and ObjectID in (select ID from @ids);
delete [Group] where ID in (select ID from @ids);";

			bool response;
			using (var connection = ConnectionProvider.Connect())
			{
				int rowsUpdated = await connection.ExecuteAsync(sql, new { uids, CurrentUserId });
				response = (rowsUpdated > 0);
			}
			return response;
		}

		public async Task<bool> RemoveMemberFromGroupAsync(Guid groupUid, Guid userUid)
		{
			string sql = @"
declare @userId int,
		@groupId int;
select @groupId = ID from [Group] where Uid = @groupUid;
select @userId = ID from reporting.Global_Resource where Uid = @userUid;

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
				inner join reporting.Global_Resource gr on gr.ResourceUD = @userId
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

		public async Task<RepositoryResponse<bool>> RemoveSettingAsync(Setting setting)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("id", (int)setting);

			string sql = "delete Setting where ID = @id";

			var response = new RepositoryResponse<bool>(false, 0, false, "");
			using (var connection = ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}
			return response;
		}

		public async Task<RepositoryResponse<int>> RemoveUsersAsync(List<Guid> uids)
		{
			RepositoryResponse<int> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var recordsImpacted = await connection.ExecuteAsync(
@"
declare @ids table(ID int, Uid uniqueidentifier);
insert into @ids 
	select ResourceID, Uid from reporting.Global_Resource where Uid in @uids;

insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription, [Version])
	select	distinct
			'Resource', 
			res.ResourceId,
			SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
			@r, 
			getutcdate(), 
			'Deleted', 
			'Resource', 
			res.ResourceId,
			'Resource', 
			SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
			'This user has been removed.',
			mv.[Version]
	from	reporting.Global_Resource res
			cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = 'Resource' and ObjectID = res.ResourceID) mv
	where	res.ResourceID in (select ID from @ids);

update	Asset
set		State = @assetState
where	Object = 'Resource'
		and ObjectID in (select ID from @ids);

update	reporting.Global_Resource
set		State = @state
where	ResourceID in (select ID from @ids);", new { uids, state = (int)CompanyResourceState.Deleted, assetState = (int)State.Deleted , r = CurrentUserId}
				);

				response = new(recordsImpacted, 200, true);
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> UpsertGroupsAsync(int executionId, List<UpdateGroupModel> items, bool lookupFieldsPassedByValue = false)
		{
			RepositoryResponse<IEnumerable<GroupResponseResult>> response = new(null, 200, true);

			List<dynamic> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<dynamic>(
					"select f.ID, f.Name from FieldType f inner join AssetType a on a.Object = 'GroupType' and a.ObjectID = 1 and f.AssetTypeID = a.ID"
					)).ToList();
			}

			#region Data Tables

			var table = new DataTable();
			var fieldTable = new DataTable();

			table.Columns.Add("ItemNumber", typeof(int));
			table.Columns.Add("Uid", typeof(Guid));
			table.Columns.Add("Name", typeof(string));
			table.Columns.Add("Description", typeof(string));
			table.Columns.Add("PrimaryOwnerUid", typeof(Guid));
			table.Columns.Add("SecondaryOwnerUid", typeof(Guid));
			table.Columns.Add("IsActiveDirectoryGroup", typeof(bool));

			fieldTable.Columns.Add("ItemNumber", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));

			#endregion

			// Load user and field data into data tables.
			int itemNumber = 0;
			items.ForEach(u => {
				var row = table.NewRow();
				itemNumber++;
				row["ItemNumber"] = itemNumber;
				row["Name"] = u.Name;
				row["Description"] = u.Description;
				row["PrimaryOwnerUid"] = u.PrimaryOwnerUid;
				row["SecondaryOwnerUid"] = u.SecondaryOwnerUid;
				row["IsActiveDirectoryGroup"] = u.IsActiveDirectoryGroup;

				if (u.Uid.HasValue && u.Uid != Guid.Empty)
				{
					row["Uid"] = u.Uid.Value;
				}

				table.Rows.Add(row);

				foreach (var key in u.Fields.Keys)
				{
					var ft = fieldTypes.FirstOrDefault(o => o.Name == key.Trim());
					if (ft != null)
					{
						var fieldRow = fieldTable.NewRow();

						fieldRow["ItemNumber"] = itemNumber;
						fieldRow["FieldName"] = key.Trim();
						fieldRow["FieldValue"] = (u.Fields[key] ?? "").Trim();
						fieldRow["FieldTypeID"] = ft.ID;

						fieldTable.Rows.Add(fieldRow);
					}
				}
			});

			SqlBulkCopy bulkCopy = null;
			var UpdatedOn = DateTime.UtcNow;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					// Create temp tables.
					await connection.ExecuteAsync(CreateImportTemporaryTableSql("Group"), transaction: trans);

					bulkCopy = connection.CreateBulkCopy("#Items", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("Uid", "Uid");
					bulkCopy.ColumnMappings.Add("Name", "Name");
					bulkCopy.ColumnMappings.Add("Description", "Description");
					bulkCopy.ColumnMappings.Add("PrimaryOwnerUid", "PrimaryOwnerUid");
					bulkCopy.ColumnMappings.Add("SecondaryOwnerUid", "SecondaryOwnerUid");
					bulkCopy.ColumnMappings.Add("IsActiveDirectoryGroup", "IsActiveDirectoryGroup");
					await bulkCopy.WriteToServerAsync(table);

					bulkCopy = connection.CreateBulkCopy("#Fields", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
					bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
					bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					await bulkCopy.WriteToServerAsync(fieldTable);

					// Validate incoming data.
					await connection.ExecuteAsync(@"
update	T
set		T.PrimaryOwnerResourceID = S.ResourceID,
		T.IsValid = iif(S.ResourceID is null, 0, T.Valid)
from	#Items T
		left join reporting.Global_Resource S on S.Uid = T.PrimaryOwnerUid 
where	T.PrimaryOwnerUid is not null;

update	T
set		T.SecondaryOwnerResourceID = S.ResourceID,
		T.IsValid = iif(S.ResourceID is null, 0, T.Valid)
from	#Items T
		left join reporting.Global_Resource S on S.Uid = T.SecondaryOwnerUid 
where	T.SecondaryOwnerUid is not null;
", transaction: trans);

					// Merge into Group table.
					await connection.ExecuteAsync(@"
declare @assetTypeId int;
select @assetTypeId = ID from AssetType where Object = 'GroupType';

-- Determine the internal IDs of groups based on their incoming Uid.
update	t
set		t.GroupID = s.ID
from	#Items  t
		inner join [Group] s on s.Uid = t.Uid and t.Uid is not null and IsValid <> 0;

-- Update groups that already exists in environment, and were successfully validated.
update	t
set		T.Name = S.Name,
		T.Description = S.Description,
		T.IsActiveDirectoryGroup = S.IsActiveDirectoryGroup,
		T.PrimaryOwnerResourceID = S.PrimaryOwnerResourceID,
		T.SecondaryOwnerResourceID = S.SecondaryOwnerResourceID,
		T.UpdatedBy = @userId,
		T.UpdatedOn = @date
from	[Group] t
		inner join #Items s on s.GroupID = t.ID and IsValid <> 0;

-- Create groups that do not yet exist in environment, and were successfully validated.
insert into [Group] (Uid, Name, Description, IsActiveDirectoryGroup, PrimaryOwnerResourceID, SecondaryOwnerResourceID, UpdatedBy, UpdatedOn)
	select	coalesce(S.uid, newid()), S.Name, S.Description, S.IsActiveDirectoryGroup, S.PrimaryOwnerResourceID, S.SecondaryOwnerResourceID, @userId, @date
	from	#Items 
	where	GroupID is null
			and IsValid <> 0;

-- Get the new internal IDs for newly created groups.
update	t
set		t.GroupID = s.ID
from	#Items  t
		inner join [Group] s on s.Uid = t.Uid and t.Uid is not null and GroupID is null and IsValid <> 0;
", new { date = UpdatedOn, userId = CurrentUserId }, transaction: trans);

					// Merge into Asset table
					await connection.ExecuteAsync(
						CreateImportAssetTableMergeSql("Group"),
						new { date = UpdatedOn, userId = CurrentUserId },
						transaction: trans);

					connection.Execute(
						CreateImportFieldValidationSql("Group", lookupFieldsPassedByValue),
						new { userId = CurrentUserId, date = UpdatedOn },
						transaction: trans);

					response.Data = (await connection.QueryAsync<GroupResponseResult>(
						@"select ItemNumber, uid, Message, coalesce(IsSuccess, cast(1 as bit)) as Success from #Items;",
						transaction: trans)
					).ToList();

					trans.Commit();
				}

				// Update Execution record.
				await connection.ExecuteAsync(
					CreateImportCompleteExecutionSql(),
					new { date = UpdatedOn, executionId }
				);
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

		public async Task<RepositoryResponse<bool>> UpsertSettingAsync(Setting setting, string value)
		{
			var userErrorMessages = new List<string>();

			var response = new RepositoryResponse<bool>(false, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 400;

				return response;
			}

			var sql = @"
if exists(select 1 from [Setting] where ID = @id) 
begin 
	update [Setting] set [Value] = @value where ID = @id 
end 
else 
begin 
	insert [Setting] values (@id, @value) 
end";

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, new { id = (int)setting, value });
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<UserApiUpsertResult>>> UpsertUsersAsync(int executionId, List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			RepositoryResponse<IEnumerable<UserApiUpsertResult>> response = new(null, 200, true);

			List<dynamic> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<dynamic>(
					"select f.ID, f.Name from FieldType f inner join AssetType a on a.Object = 'ResourceType' and a.ObjectID = 1 and f.AssetTypeID = a.ID"
					)).ToList();
			}

			#region Data Tables

			var userTable = new DataTable();
			var fieldTable = new DataTable();

			userTable.Columns.Add("ItemNumber", typeof(int));
			userTable.Columns.Add("ResourceID", typeof(int));
			userTable.Columns.Add("Uid", typeof(Guid));
			userTable.Columns.Add("Username", typeof(string));
			userTable.Columns.Add("Email", typeof(string));
			userTable.Columns.Add("FirstName", typeof(string));
			userTable.Columns.Add("LastName", typeof(string));
			userTable.Columns.Add("State", typeof(int));
			userTable.Columns.Add("IsAdministrator", typeof(bool));

			fieldTable.Columns.Add("ItemNumber", typeof(int));
			fieldTable.Columns.Add("ResourceID", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));

			#endregion

			// Load user and field data into data tables.
			users.ForEach(u => {
				var userRow = userTable.NewRow();
				userRow["ItemNumber"] = u.ItemNumber;
				userRow["ResourceID"] = u.ResourceID;
				userRow["Uid"] = u.uid;
				userRow["Username"] = u.Username;
				userRow["Email"] = u.Email;
				userRow["FirstName"] = u.FirstName;
				userRow["LastName"] = u.LastName;
				userRow["State"] = u.State;
				userRow["IsAdministrator"] = u.IsAdministrator;
				userTable.Rows.Add(userRow);

				foreach (var key in u.Fields.Keys)
				{ 
					var ft = fieldTypes.FirstOrDefault(o => o.Name == key.Trim());
					if (ft != null)
					{
						var fieldRow = fieldTable.NewRow();

						fieldRow["ItemNumber"] = u.ItemNumber;
						fieldRow["ResourceID"] = u.ResourceID;
						fieldRow["FieldName"] = key.Trim();
						fieldRow["FieldValue"] = (u.Fields[key]??"").Trim();
						fieldRow["FieldTypeID"] = ft.ID;

						fieldTable.Rows.Add(fieldRow);
					}				
				}
			});

			SqlBulkCopy bulkCopy = null;
			var UpdatedOn = DateTime.UtcNow;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				connection.Open();
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					// Create temp tables.
					await connection.ExecuteAsync(CreateImportTemporaryTableSql("Resource"), transaction: trans);

					bulkCopy = connection.CreateBulkCopy("#Items", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
					bulkCopy.ColumnMappings.Add("Uid", "Uid");
					bulkCopy.ColumnMappings.Add("Username", "Username");
					bulkCopy.ColumnMappings.Add("Email", "Email");
					bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
					bulkCopy.ColumnMappings.Add("LastName", "LastName");
					bulkCopy.ColumnMappings.Add("State", "State");
					bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
					await bulkCopy.WriteToServerAsync(userTable);

					bulkCopy = connection.CreateBulkCopy("#Fields", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
					//bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
					bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
					bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
					bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					await bulkCopy.WriteToServerAsync(fieldTable);

					// Merge into Global_Resource table.
					await connection.ExecuteAsync(@"
merge	reporting.Global_Resource as T
using	(select * from #Items) as S
on		(T.ResourceID = S.ResourceID)
when	matched then
update  set
		T.IsAdministrator = S.IsAdministrator,
		T.State = S.State,
		T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.Email = S.Email,
		T.UpdatedOn = @UpdatedOn,
		T.MostRecentExecutionId = @executionId
when	not matched by target then
insert	(ResourceID, FirstName, LastName, Email, IsAdministrator, CreatedOn, State, Uid, UpdatedOn, MostRecentExecutionId)
values	(S.ResourceID, S.FirstName, S.LastName, S.Email, S.IsAdministrator, @UpdatedOn, S.State, S.Uid, @UpdatedOn, @executionId);
", new { UpdatedOn, executionId }, transaction: trans);

					// Merge into Asset table
					await connection.ExecuteAsync(
						CreateImportAssetTableMergeSql("Resource"), 
						new { date = UpdatedOn, userId = CurrentUserId }, 
						transaction: trans);

					connection.Execute(
						CreateImportFieldValidationSql("Resource", lookupFieldsPassedByValue), 
						new { userId = CurrentUserId, date = UpdatedOn },
						transaction: trans);

					response.Data = (await connection.QueryAsync<UserApiUpsertResult>(
						@"select ItemNumber, uid, Message, coalesce(IsSuccess, cast(1 as bit)) as Success from #Items;", 
						transaction: trans)
					).ToList();

					trans.Commit();
				}

				// Update Execution record.
				await connection.ExecuteAsync(
					CreateImportCompleteExecutionSql(), 
					new { date = UpdatedOn, executionId }
				);
			}

			return response;
		}
	}
}
