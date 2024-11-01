using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using Dapper.Contrib.Extensions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using repositories.resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Workspaces : Repository, IWorkspaces
	{
		public int CompanyId { get; set; }
		public string WorkspaceId { get; set; }

		public Workspaces(DapperConnectionProvider provider): base(provider) { }

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
				fieldTypes = (await connection.QueryAsync<FieldType>("select * from FieldType where GroupTypeID = 1")).ToList();
			}

			List<string> fieldColumns = ["G.Uid", "G.Name", "G.Description", "gr1.uid as PrimaryOwnerUid", "gr2.uid as SecondaryOwnerUid", "G.IsActiveDirectoryGroup"];
			List<string> fieldJoins = [];
			if (fieldTypes.Count > 0)
			{
				fieldTypes.ForEach(ft =>
				{
					var prefix = $"f_{ft.ID}";
					switch (ft.Type)
					{
						case "Lookup":
							validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
							fieldColumns.Add($"{prefix}.FormattedValue as [{ft.Name}]");
							fieldJoins.Add($"inner join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.GroupID = G.ID) or {prefix}.GroupID = 0");
							break;
						default:
							validOrderFields.Add(new SortColumnOption(ft.Name, $"{prefix}.FormattedValue"));
							fieldColumns.Add($"{prefix}.FormattedValue as [{ft.Name}]");
							fieldJoins.Add($"inner join Field {prefix} on ({prefix}.FieldTypeID = {ft.ID} and {prefix}.GroupID = G.ID) or {prefix}.GroupID = 0");
							break;
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

select	distinct 
		'Group', 
		g.ID, 
		G.Name, 
		@CurrentUserId, 
		GETUTCDATE(), 
		'Group removed', 
		'Group', 
		g.ID, 
		'Group', 
		G.Name, 
		'',
		mv.[Version]
from	[Group] g 
		cross apply (select coalesce(max([Version]),0)+1 as [Version] from reporting.Global_Audit where Object = 'Group' and ObjectID = g.ID) mv
where	g.ID in @ids;

delete ResourceGroup where GroupID in (select ID from @ids);
delete Field where GroupID in (select ID from @ids);
delete [Group] where ID in @ids;";

			bool response;
			using (var connection = ConnectionProvider.Connect())
			{
				int rowsUpdated = await connection.ExecuteAsync(sql, new { uids });
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

	insert into reporting.Global_Audit
		select	distinct 
				'Group', 
				g.ID, 
				G.Name, 
				@CurrentUserId, 
				GETUTCDATE(), 
				'Member removed', 
				'Group', 
				g.ID, 
				'Group', 
				G.Name,'[' + gr.FirstName + ' ' + gr.LastName + '] removed from the group.',
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
	where	res.uid in @uids;

update	reporting.Global_Resource
set		State = state
where	uid in @uids;", new { uids, state = (int)CompanyResourceState.Deleted }
				);

				response = new(recordsImpacted, 200, true);
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<GroupResponseResult>>> UpdateGroupsAsync(List<UpdateGroupModel> groups)
		{
			RepositoryResponse<IEnumerable<GroupResponseResult>> response = new(400);
			List<GroupResponseResult> groupResults = [];

			List<FieldType> fieldTypes = [];
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<FieldType>("select * from FieldType where GroupTypeID = 1 and IsEditable = 1")).ToList();
			}

			#region Data Tables

			var groupTable = new DataTable();
			var fieldTable = new DataTable();

			groupTable.Columns.Add("Uid", typeof(Guid));
			groupTable.Columns.Add("Name", typeof(string));
			groupTable.Columns.Add("Description", typeof(string));
			groupTable.Columns.Add("PrimaryOwnerUid", typeof(Guid));
			groupTable.Columns.Add("SecondaryOwnerUid", typeof(Guid));
			groupTable.Columns.Add("IsActiveDirectoryGroup", typeof(bool));

			fieldTable.Columns.Add("Uid", typeof(int));
			fieldTable.Columns.Add("FieldName", typeof(string));
			fieldTable.Columns.Add("FieldValue", typeof(string));
			fieldTable.Columns.Add("BooleanValue", typeof(bool));
			fieldTable.Columns.Add("DateValue", typeof(DateTime));
			fieldTable.Columns.Add("DecimalValue", typeof(decimal));
			fieldTable.Columns.Add("LookupValue", typeof(string));
			fieldTable.Columns.Add("NumberValue", typeof(long));
			fieldTable.Columns.Add("TextValue", typeof(string));
			fieldTable.Columns.Add("FieldTypeID", typeof(int));

			var tempFieldTable = fieldTable.Copy();

			#endregion

			// Resolve Lookup Values.
			var lookupFieldNames = fieldTypes.Where(ft => ft.Type == "Lookup").Select(ft => ft.Name);
			var rawListValues = (from gr in groups
								 from f in gr.Fields
								 from fv in f.Value.Split(',')
								 where lookupFieldNames.Contains(f.Key)
								 select new { Name = f.Key, RawValue = fv }
								).Distinct().ToList();

			var lookupTable = new DataTable();
			lookupTable.Columns.Add("Name", typeof(string));
			lookupTable.Columns.Add("Value", typeof(string));
			rawListValues.ForEach(l =>
			{
				var lookupRow = lookupTable.NewRow();
				lookupRow["Name"] = l.Name;
				lookupRow["Value"] = l.RawValue;
				lookupTable.Rows.Add(lookupRow);
			});

			List<dynamic> validatedLookupItems = null;
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.OpenAsync();
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					await connection.ExecuteAsync("create table #LookupTable ( ObjectID int, Name nvarchar(250), [Value] nvarchar(max) );", transaction: trans);

					var bulkLookupTableCopy = connection.CreateBulkCopy("#LookupTable", trans: trans);
					bulkLookupTableCopy.ColumnMappings.Add("Name", "Name");
					bulkLookupTableCopy.ColumnMappings.Add("Value", "Value");
					await bulkLookupTableCopy.WriteToServerAsync(lookupTable);

					validatedLookupItems = (await connection.QueryAsync<dynamic>(@"
update	t
set		t.ObjectID = s.[Value]
from	#LookupTable t
		inner join FieldType ft on ft.GroupTypeID = 1 and ft.Name = t.Name and ft.[Type] = 'Lookup'
		inner join FieldLookupValue s on s.FieldTypeID = ft.ID and s.[Text] = t.[Value];

select ObjectID, Name, [Value] from #LookupTable;
", transaction: trans
)).ToList();

					trans.Commit();
				}
			}

			// Load user and field data into data tables.
			groups.ForEach(g => {
				List<string> errors = [];

				fieldTypes.ForEach(ft =>
				{
					bool continueCheck = true;

					if (!g.Fields.ContainsKey(ft.Name) && ft.IsRequired)
					{
						continueCheck = false;
						errors.Add($"{ft.Name} is required, but not present");
					}

					if (continueCheck && g.Fields.ContainsKey(ft.Name))
					{
						string fieldValue = (g.Fields[ft.Name] ?? "").Trim();

						var fieldRow = tempFieldTable.NewRow();

						fieldRow["Uid"] = g.Uid;
						fieldRow["FieldName"] = ft.Name;
						fieldRow["FieldTypeID"] = ft.ID;

						if (ft.IsRequired && string.IsNullOrEmpty(fieldValue))
						{
							continueCheck = false;
							errors.Add($"{ft.Name} is required, but is empty");
						}

						if (continueCheck && ft.Type == DataType.Boolean.ToString())
						{
							if (bool.TryParse(fieldValue, out bool bValue))
							{
								fieldRow["BooleanValue"] = bValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a boolean value");
							}
						}

						if (continueCheck && ft.Type == DataType.Date.ToString())
						{
							if (DateTime.TryParse(fieldValue, out DateTime dValue))
							{
								fieldRow["DateValue"] = dValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a date value");
							}
						}

						if (continueCheck && ft.Type == DataType.DateTime.ToString())
						{
							if (!DateTime.TryParse(fieldValue, out DateTime dtValue))
							{
								fieldRow["DateValue"] = dtValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a datetime value");
							}
						}

						if (continueCheck && ft.Type == DataType.Decimal.ToString())
						{
							if (!decimal.TryParse(fieldValue, out decimal decValue))
							{
								fieldRow["DecimalValue"] = decValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a decimal value");
							}
						}

						if (continueCheck && ft.Type == DataType.Lookup.ToString())
						{
							if (ft.AllowMultipleValues)
							{
								var splitValues = fieldValue.Split(',').ToList();
								var splitLookupItems = validatedLookupItems.Where(v => splitValues.Contains(v.Value) && v.Name == ft.Name).ToList();
								if (splitLookupItems.Count == splitValues.Count)
								{
									fieldRow["LookupValue"] = string.Join(",", splitLookupItems.Select(v => ((int)v.ObjectID).ToString()));
								}
								else 
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not contain a valid lookup value");
								}
							}
							else 
							{
								var validatedLookupItem = validatedLookupItems.FirstOrDefault(v => v.Value == fieldValue && v.Name == ft.Name);
								if (validatedLookupItem == null)
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not contain a valid lookup value");
								}
								else 
								{
									fieldRow["LookupValue"] = validatedLookupItem.ObjectID;
								}
							}
						}

						if (continueCheck && ft.Type == DataType.Number.ToString())
						{
							if (!long.TryParse(fieldValue, out long lValue))
							{
								fieldRow["NumberValue"] = lValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a number value");
							}
						}

						if (continueCheck && ft.Type == DataType.Text.ToString())
						{
							if (ft.MinimumLength.HasValue)
							{
								if (fieldValue.Length < ft.MinimumLength.Value)
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not meet minimum length requirements");
								}
							}

							if (continueCheck && ft.MaximumLength.HasValue)
							{
								if (fieldValue.Length > ft.MaximumLength.Value)
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not meet maximum length requirements");
								}
							}

							if (continueCheck && ft.Length.HasValue)
							{
								if (fieldValue.Length != ft.Length.Value)
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not meet length requirements");
								}
							}

							if (continueCheck && !string.IsNullOrEmpty(ft.Pattern))
							{
								if (!Regex.Match(fieldValue, ft.Pattern).Success)
								{
									continueCheck = false;
									errors.Add($"{ft.Name} does not meet pattern requirements");
								}
							}

							if (!long.TryParse(fieldValue, out long lValue))
							{
								fieldRow["TextValue"] = fieldValue;
							}
							else
							{
								continueCheck = false;
								errors.Add($"{ft.Name} does not contain a number value");
							}
						}

						if (continueCheck && ft.Type == DataType.Html.ToString())
						{
							fieldRow["TextValue"] = fieldValue;
						}

						if (continueCheck)
						{
							tempFieldTable.Rows.Add(fieldRow);
						}
					}
				});

				if (errors.Count == 0)
				{
					var row = groupTable.NewRow();
					row["Uid"] = g.Uid;
					row["Name"] = g.Name;
					row["Description"] = g.Description;
					row["PrimaryOwnerUid"] = g.PrimaryOwnerUid;
					row["SecondaryOwnerUid"] = g.SecondaryOwnerUid;
					row["IsActiveDirectoryGroup"] = g.IsActiveDirectoryGroup;
					groupTable.Rows.Add(row);

					// Copy all fields as the group passed validation.
					foreach (DataRow r in tempFieldTable.Rows)
					{
						fieldTable.Rows.Add(r);
					}
					// Clear the temp table for the next pass.
					tempFieldTable.Rows.Clear();
				}
				else 
				{
					// Place in reject pile.
					groupResults.Add(new GroupResponseResult { Message = string.Join("; ", errors), Success = false, uid = g.Uid });
				}
			});

			SqlBulkCopy bulkCopy = null;
			var UpdatedOn = DateTime.UtcNow;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.OpenAsync();
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					// Create temp tables.
					await connection.ExecuteAsync(@"
create table #Groups (
	[Uid] uniqueidentifier, Name nvarchar(500), Description nvarchar(max),
	PrimaryOwnerUid uniqueidentifier, SecondaryOwnerUid uniqueidentifier, IsActiveDirectoryGroup bit,
	GroupId int, PrimaryOwnerId int, SecondaryOwnerId int,
	IsValid bit, Message nvarchar(2500), IsSuccess bit
);

create table #Fields (
	[Uid] uniqueidentifier, FieldName nvarchar(250), FieldTypeID int, FieldValue nvarchar(max), 
	BooleanValue bit, DateValue datetime, DecimalValue decimal(18,6), LookupValue nvarchar(max), NumberValue bigint, TextValue nvarchar(max)
);", transaction: trans);

					bulkCopy = connection.CreateBulkCopy("#Groups", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("Uid", "Uid");
					bulkCopy.ColumnMappings.Add("Name", "Name");
					bulkCopy.ColumnMappings.Add("Description", "Description");
					bulkCopy.ColumnMappings.Add("PrimaryOwnerUid", "PrimaryOwnerUid");
					bulkCopy.ColumnMappings.Add("SecondaryOwnerUid", "SecondaryOwnerUid");
					bulkCopy.ColumnMappings.Add("IsActiveDirectoryGroup", "IsActiveDirectoryGroup");
					await bulkCopy.WriteToServerAsync(groupTable);

					bulkCopy = connection.CreateBulkCopy("#Fields", 1000, 1200, trans);
					bulkCopy.ColumnMappings.Add("Uid", "Uid");
					bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
					bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
					bulkCopy.ColumnMappings.Add("BooleanValue", "BooleanValue");
					bulkCopy.ColumnMappings.Add("DateValue", "DateValue");
					bulkCopy.ColumnMappings.Add("DecimalValue", "DecimalValue");
					bulkCopy.ColumnMappings.Add("LookupValue", "LookupValue");
					bulkCopy.ColumnMappings.Add("NumberValue", "NumberValue");
					bulkCopy.ColumnMappings.Add("TextValue", "TextValue");
					await bulkCopy.WriteToServerAsync(fieldTable);

					// Group info Validation
					await connection.ExecuteAsync(@"
update	t
set		t.GroupId = g.ID
from	#Groups t
		left join [Group] g on g.Uid = t.Uid;

update	t
set		t.PrimaryOwnerId = u.ResourceID
from	#Groups t
		left join reporting.Global_Resource u on u.Uid = t.PrimaryOwnerUid;

update	t
set		t.SecondaryOwnerId = u.ResourceID
from	#Groups t
		left join reporting.Global_Resource u on u.Uid = t.SecondaryOwnerUid;

update	#Groups 
set		IsValid = 0,
		Message = 'Group not found based on Uid provided; '
where	GroupId is null;

update	#Groups 
set		IsValid = 0,
		Message = coalesce(Message,'') + 'Primary Owner not found based on Uid provided; '
where	PrimaryOwnerUid is not null 
		and PrimaryOwnerId is null;

update	#Groups 
set		IsValid = 0,
		Message = coalesce(Message,'') + 'Secondary Owner not found based on Uid provided; '
where	SecondaryOwnerUid is not null 
		and SecondaryOwnerId is null;

update	#Groups 
set		IsValid = 1
where	IsValid is null;
", new { UpdatedOn, CurrentUserId }, transaction: trans);

					await connection.ExecuteAsync(@"
insert into ResourceGroup (ResourceID, GroupID)
	select	g.PrimaryOwnerId, g.GroupId 
	from	#Groups g 
			left join ResourceGroup e on e.GroupID = g.GroupId and e.ResourceID = g.PrimaryOwnerId
	where	g.IsValid = 1 
			and g.PrimaryOwnerId is not null
			and e.ResourceID is null;

insert into ResourceGroup (ResourceID, GroupID)
	select	g.SecondaryOwnerId, g.GroupId 
	from	#Groups g 
			left join ResourceGroup e on e.GroupID = g.GroupId and e.ResourceID = g.SecondaryOwnerId
	where	g.IsValid = 1 
			and g.SecondaryOwnerId is not null
			and e.ResourceID is null;

update	t
set		t.Name = s.Name,
		t.Description = s.Description,
		t.PrimaryOwnerResourceID = s.PrimaryOwnerId,
		t.SecondaryOwnerResourceID = s.SecondaryOwnerId,
		t.IsActiveDirectoryGroup = s.IsActiveDirectoryGroup,
		t.UpdatedOn = @UpdatedOn,
		t.UpdatedBy = @CurrentUserId
from	[Group] t
		inner join #Groups s on s.GroupId = t.ID and s.IsValid = 1;
", new { UpdatedOn, CurrentUserId }, transaction: trans);

					// Save fields for groups.	
					await connection.ExecuteAsync(@"
merge	Field as t
using	(
		select	g.GroupID,
				f.* 
		from	#Fields f
				inner join #Groups g on g.Uid = f.Uid
		) as s
on		(t.GroupID = s.GroupID and t.FieldTypeID = s.FieldTypeID)
when	matched then
update	set
		t.Value = iif(s.LookupValue is null, null, s.LookupValue),
		t.FormattedValue = iif(s.LookupValue is null, coalesce(s.BooleanValue, s.DateValue, s.DecimalValue, s.NumberValue, s.TextValue), null),
		t.UpdatedBy = @CurrentUserId,
		t.UpdatedOn = @UpdatedOn
when	not matched by target then
insert	(GroupID, ObjectType, ObjectID, FieldTypeID, [Value], FormattedValue, UpdatedBy, UpdatedOn)
values	(
		s.GroupID, 
		'Group', s.GroupID, 
		s.FieldTypeID, 
		iif(s.LookupValue is null, null, s.LookupValue), 
		iif(s.LookupValue is null, coalesce(s.BooleanValue, s.DateValue, s.DecimalValue, s.NumberValue, s.TextValue), null), 
		@CurrentUserId, @UpdatedOn);",
						new { CurrentUserId, UpdatedOn }, transaction: trans
					);

					response.Data = (await connection.QueryAsync<GroupResponseResult>(@"
select	Uid, 
		coalesce(IsValid, cast(1 as bit)) as Success 
from	#Groups;", transaction: trans)
					).ToList();
					response.IsSuccess = true;

					trans.Commit();
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

		public async Task<RepositoryResponse<IEnumerable<UserApiUpsertResult>>> UpsertUsersAsync(int executionId, int resourceId, List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			RepositoryResponse<IEnumerable<UserApiUpsertResult>> response = new(null, 200, true);

			List<dynamic> fieldTypes = new();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				fieldTypes = (await connection.QueryAsync<dynamic>("select ID, Name from FieldType where ResourceTypeID = 1")).ToList();
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
				using (SqlTransaction trans = connection.BeginTransaction())
				{
					// Create temp tables.
					await connection.ExecuteAsync(@"
create table #Users (
	ItemNumber int, ResourceID int, [Uid] uniqueidentifier, Username nvarchar(500), Email nvarchar(500),
	FirstName nvarchar(250), LastName nvarchar(250), [State] int, IsAdministrator bit,
	IsValid bit, IsSuccess bit);

create table #Fields (
	ItemNumber int, ResourceID int, 
	FieldName nvarchar(250), FieldTypeID int, FieldValue nvarchar(max), LookupValue nvarchar(max)
);", transaction: trans);

					bulkCopy = connection.CreateBulkCopy("#Users", 1000, 1200, trans);
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
					bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
					bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
					bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
					bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					await bulkCopy.WriteToServerAsync(fieldTable);

					// Merge into Global_Resource table.
					await connection.ExecuteAsync(@"
merge	reporting.Global_Resource as T
using	(select * from #Users) as S
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
values	(S.ResourceID, S.FirstName, S.LastName, S.IsAdministrator, getutcdate(), S.State, Uid, @UpdatedOn, @executionId);
", new { UpdatedOn, executionId }, transaction: trans);

					// Validate lookup fields.
					if (lookupFieldsPassedByValue)
					{
						connection.Execute(@"
update	T
set		T.LookupValue = T.[FieldValue]
from	#Fields T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup'",
							transaction: trans);
					}
					else
					{
						connection.Execute(@"
declare @listFieldTypes table (FieldTypeID int, AllowMultipleValues bit);
declare @uniqueListValues table (FieldTypeID int, AllowMultipleValues bit, FieldValue nvarchar(max), LookupValue nvarchar(max))

insert into @listFieldTypes
select	t.FieldTypeID, s.AllowMultipleValues
from	#Fields t
		inner join FieldType s on s.ID = t.FieldTypeID and s.[Type] = 'Lookup'
group by t.FieldTypeID, s.AllowMultipleValues;

insert into @uniqueListValues
select	t.FieldTypeID, s.AllowMultipleValues, t.FieldValue
from	#Fields t
		inner join @listFieldTypes s on s.FieldTypeID = t.FieldTypeID
		cross apply string_split(t.FieldValue, ',') tmv
group by t.FieldTypeID, s.AllowMultipleValues, t.FieldValue;

update	t
set		t.LookupValue = t.[Value]
from	@uniqueListValues t
	inner join FieldLookupValue s on s.FieldTypeID = t.FieldTypeID and s.[Text] = t.FieldValue;

update	t
set		t.LookupValue = s.LookupValue
from	#Fields t
		inner join @listFieldTypes s on s.FieldTypeID = t.FieldTypeID and s.AllowMultipleValues = 0;

update	t
set		t.LookupValue = ms.LookupValue
from	#Fields t
		inner join FieldType ft on ft.ID = t.FieldTypeID and ft.[Type] = 'Lookup' and ft.AllowMultipleValues = 1
		cross apply (
			select	string_agg(s.LookupValue, ',') as LookupValue
			from	@listFieldTypes s
			where	s.FieldTypeID = t.FieldTypeID
					and LookupValue in (select [value] from string_split(t.FieldValue, ','))
		) ms;",
							transaction: trans);
					}

					// Save fields for users.	
					await connection.ExecuteAsync(@"
merge	Field as t
using	(
		select * from #Fields
		) as s
on		(t.ResourceID = s.ResourceID and t.FieldTypeID = s.FieldTypeID)
when	matched then
update	set
		t.Value = iif(s.LookupValue is null, null, s.LookupValue),
		t.FormattedValue = iif(s.LookupValue is null, s.FieldValue, null),
		t.UpdatedBy = @resourceId,
		t.UpdatedOn = @UpdatedOn
when	not matched by target then
insert	(ResourceID, ObjectType, ObjectID, FieldTypeID, [Value], FormattedValue, UpdatedBy, UpdatedOn)
values	(s.ResourceID, 'Resource', s.ResourceID, s.FieldTypeID, iif(s.LookupValue is null, null, s.LookupValue)), iif(s.LookupValue is null, s.FieldValue, null), @resourceId, @UpdatedOn)",
						new { resourceId, UpdatedOn }, transaction: trans
					);

					response.Data = (await connection.QueryAsync<UserApiUpsertResult>(@"
select	ItemNumber, 
		uid, 
		'' as Message, 
		coalesce(IsSuccess, cast(1 as bit)) as Success 
from	#User;", transaction: trans)
					).ToList();

					trans.Commit();
				}

				// Update Execution record.
				await connection.ExecuteAsync(@"
update	E 
set		E.[State] = 4,
		E.CompletedOn = @UpdatedOn,
		E.[Total] = iif(Tc.Cnt = 0, E.[Total], Tc.Cnt),
		E.Processed = iif(Pc.Cnt = 0, E.Processed, Pc.Cnt),
		E.[Error] = iif(Ec.Cnt = 0, E.[Error], Ec.Cnt)
from	api.Execution E
		cross apply ( select count(1) as Cnt from #Users where IsSuccess = 0  ) Ec
		cross apply ( select count(1) as Cnt from #Users where IsSuccess = 1 ) Pc
		cross apply ( select count(1) as Cnt from #Users ) Tc
where	E.Id = @executionId", new { UpdatedOn, executionId });
			}

			return response;
		}
	}
}
