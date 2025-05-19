using d360.core;
using d360.core.entities;
using d360.core.entities.ChangeLog;
using d360.core.enums;
using d360.core.security;
using Dapper;
using DocumentFormat.OpenXml.EMMA;
using MoreLinq;
using Newtonsoft.Json;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReadSecurityPolicy = d360.core.security.ReadSecurityPolicy;
using RuleThen = d360.core.security.RuleThen;
using RuleWhen = d360.core.security.RuleWhen;

namespace repositories.azure
{
	public class Security : Repository, ISecurity
	{
		List<string> VALID_FIELDS = new List<string> { "Boolean", "Date", "DateTime", "Number", "Decimal", "Lookup", "Text" };

		readonly string READ_POLICY_SQL_BASE = @"
select	ru.uid,
		ru.name, 
		t.Uid as assetTypeUid,  
		t.Name as assetTypeName,
		ro.Uid as roleUid,
		ro.Name as roleName,
		ru.securityType,
		ru.applyToType,
		ru.IsVisible as visible,
		rw.Whens as whenConditions,
		rt.Thens as thenConditions
from	security.[Rule] ru 
		inner join [security].[Role] ro on ro.Id = ru.RoleId
		inner join AssetType t on t.Id = ru.AssetTypeId
		cross apply (
			select	(
					select		w.checkType,
								ft.Name as FieldName,
								it.Uid as IntersectTypeUid,
								w.[Operator],
								w.[Value],
								a.Uid as AssetUid
					from		[security].RuleWhen w
								left join FieldType ft on ft.ID = w.FieldTypeId
								left join IntersectType it on it.ID = w.IntersectTypeId
								left join Asset a on a.Id = w.AssetId
					where		w.Id = ru.Id
					order by	w.Position
					for json path
					) as Whens
		) rw
		cross apply (
			select	(
					select		ft.Name as FieldName,
								t.[Operator],
								t.[Value],
								ru.SecurityType,
								coalesce(ga.Uid, r.Uid) as SecurityUid
					from		[security].RuleThen t
								left join FieldType ft on ft.ID = t.FieldTypeId
								left join [Group] g on g.Id = t.SecurityId and ru.SecurityType = 1
								left join Asset ga on ga.Object = 'Group' and ga.ObjectId = g.Id and ru.SecurityType = 1
								left join reporting.Global_Resource r on r.ResourceId = t.SecurityId and ru.SecurityType = 2
					where		t.Id = ru.Id
					order by	t.Position
					for json path
					) as Thens
		) rt";

		public Security(DapperConnectionProvider provider) : base(provider) { }

		public async Task<RepositoryResponse<ReadSecurityPolicy>> CreatePolicyAsync(CreateSecurityPolicy model)
		{
			RepositoryResponse<ReadSecurityPolicy> response = null;

			response = validatePolicy(model);
			if (response != null)
			{
				return response;
			}
			model.Name = (model.Name ?? "").Trim();

			var IntersectTypeUids = model.When.Where(w => w.IntersectTypeUid.HasValue).Select(w => w.IntersectTypeUid.Value).ToList();
			var AssetUids = model.When.Where(w => w.AssetUid.HasValue).Select(w => w.AssetUid.Value).ToList();
			var SecurityUids = model.Then.Where(w => w.SecurityUid.HasValue).Select(w => w.SecurityUid.Value).ToList();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				// Data for validation.
				var qryData = await connection.QueryMultipleAsync(
					"select Id from AssetType where Uid = @AssetTypeUid; " +
					"select Id from [security].[Role] where Uid = @RoleUid; " +
					"select f.* from FieldType f inner join AssetType a on a.ID = f.AssetTypeID and a.Uid = @AssetTypeUid; " +
					"select i.* from IntersectType i inner join AssetType a on (a.ID = i.SubjectAssetTypeID or a.ID = i.ObjectAssetTypeID) and a.Uid = @AssetTypeUid and I.Uid in @IntersectTypeUids; " +
					"select * from FieldType where Object in ('GroupType'); " +
					"select * from FieldType where Object in ('ResourceType'); " +
					"select * from Asset where Uid in @AssetUids; " +
					"select * from [Group] where Uid in @SecurityUids; " +
					"select * from reporting.Global_Resource where Uid in @SecurityUids;",
					new { model.AssetTypeUid, model.RoleUid, IntersectTypeUids, AssetUids, SecurityUids }
				);
				var assetTypeId = await qryData.ReadFirstAsync<int>();
				var roleId = await qryData.ReadFirstAsync<int>();
				var assetTypeFields = await qryData.ReadAsync<FieldType>();
				var intersectTypes = await qryData.ReadAsync<IntersectType>();
				var groupFields = await qryData.ReadAsync<FieldType>();
				var userFields = await qryData.ReadAsync<FieldType>();
				var whenAssets = await qryData.ReadAsync<Asset>();
				var groups = await qryData.ReadAsync<d360.core.entities.Group>();
				var users = await qryData.ReadAsync<GlobalReportingResource>();

				var securityType = model.SecurityType;

				if (assetTypeId == 0)
				{
					response = new(404, "Could not find asset type based on AssetTypeUid provided.");
				}

				if (response == null && roleId == 0)
				{
					response = new(404, "Could not find role based on RoleUid provided.");
				}
				
				var rawWhens = new List<RuleWhen>();
				if (response == null && model.When.Count > 0)
				{
					(rawWhens, response) = validatePolicyWhenConditions(model.When, assetTypeId, intersectTypes, assetTypeFields, whenAssets);
				}

				var rawThens = new List<RuleThen>();
				if (response == null && model.Then.Count > 0)
				{
					(rawThens, response) = validatePolicyThenConditions(model.Then, securityType, groups, groupFields, users, userFields);
				}

				if (response == null)
				{
					await connection.OpenAsync();
					using (var trans = connection.BeginTransaction())
					{
						long ruleId = connection.QuerySingle<long>(
							"insert into [security].[Rule] (Uid, Name, RoleId, SecurityType, AssetTypeId, ApplyToType, IsVisible, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
							"values (@Uid, @Name, @roleId, @securityType, @assetTypeId, @ApplyToType, @IsVisible, @u, @dt, @u, @dt); " +
							"select SCOPE_IDENTITY();", 
							new { Uid = Guid.NewGuid(), model.Name, roleId, securityType = (int)securityType, assetTypeId, model.ApplyToType, model.IsVisible, u = CurrentUserId, dt = DateTime.UtcNow }, 
							trans);

						rawWhens.ForEach(w => {
							w.Id = ruleId;
							connection.Execute(
								"insert into [security].RuleWhen (Id, [Position], CheckType, FieldTypeId, IntersectTypeId, [Operator], [Value], AssetId) " +
								"values (@Id, @Position, @CheckType, @FieldTypeId, @IntersectTypeId, @Operator, @Value, @AssetId)", 
								new { w.Id, w.Position, w.CheckType, w.FieldTypeId, w.IntersectTypeId, Operator = (int)w.Operator, w.Value, w.AssetId }, 
								trans);
						});

						rawThens.ForEach(t => {
							t.Id = ruleId;
							connection.Execute(
								"insert into [security].RuleThen (ID, [Position], FieldTypeId, [Operator], [Value], SecurityId) " +
								"values (@Id, @Position, @FieldTypeId, @Operator, @Value, @SecurityId)",
								new { t.Id, t.Position, t.FieldTypeId, Operator = (int)t.Operator, t.Value, t.SecurityId },
								trans);
						});

						trans.Commit();

						var jsons = await connection.QuerySingleAsync<string>(
							$@"{READ_POLICY_SQL_BASE} where ru.Id = @ruleId for json path, WITHOUT_ARRAY_WRAPPER;", new { ruleId }
						);
						var jsonPayload = string.Concat(jsons);


						var policy = JsonConvert.DeserializeObject<ReadSecurityPolicy>(jsonPayload);
						response = new(policy, 201, true, "Policy created successfully.");
					}
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadSecurityPolicyOverride>> CreateOverrideAsync(CreateSecurityPolicyOverride model)
		{
			RepositoryResponse<ReadSecurityPolicyOverride> response = null;

			if (model == null)
			{
				return new(400, "No valid data to create rule.");
			}

			if (!string.IsNullOrEmpty(model.Context))
			{
				model.Context = (model.Context ?? "").Trim().RemoveHtml();
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{				
				// Data for validation.
				var securityQuery = model.SecurityType == RuleSecurityType.Group ?
					"select g.Id from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID where a.Uid = @SecurityUid;" :
					"select ResourceId from reporting.Global_Resource where Uid = @SecurityUid;";
				var qryData = await connection.QueryMultipleAsync(
					"select Id from [security].[Role] where Uid = @RoleUid; " +
					"select Id from Asset where Uid = @AssetUid; " +
					securityQuery,
					new { model.RoleUid, model.AssetUid, model.SecurityUid }
				);
				int? roleId = await qryData.ReadFirstOrDefaultAsync<int>();
				long? assetId = await qryData.ReadFirstOrDefaultAsync<long>();
				int? securityId = await qryData.ReadFirstOrDefaultAsync<int>();

				if (!roleId.HasValue)
				{
					response = new(404, "Could not find role based on RoleUid provided.");
				}

				if (!assetId.HasValue)
				{
					response = new(404, "Could not find asset based on AssetUid provided.");
				}

				if (!securityId.HasValue)
				{
					if (model.SecurityType == RuleSecurityType.Group)
					{
						response = new(404, "Could not find group based on SecurityUid provided.");
					}
					else
					{
						response = new(404, "Could not find user based on SecurityUid provided.");
					}
				}

				if (response == null)
				{
					await connection.OpenAsync();
					Guid overrideUid;
					using (var trans = connection.BeginTransaction())
					{
						overrideUid = connection.QuerySingle<Guid>(
							"insert into security.[Override] (RoleId, SecurityType, SecurityId, AssetId, Context, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)" +
							"output inserted.Id " +
							"values (@roleId, @securityType, @securityId, @assetId, @Context, @CurrentUserId, @currentDate, @CurrentUserId, @currentDate);",
						new { 
							roleId, 
							securityType = (int)model.SecurityType,
							securityId,
							assetId,
							model.Context,
							CurrentUserId,
							CurrentDate = DateTime.UtcNow
						}, 
						trans);

						await connection.UpdateChangeLogForAsset(assetId.Value, CurrentUserId, ChangeLogObject.RoleAssignment, ChangeLogAction.Created, new { securityType = model.SecurityType, securityId, roleId }, trans);

						trans.Commit();
					}
					
					response = new RepositoryResponse<ReadSecurityPolicyOverride>(
						new ReadSecurityPolicyOverride { 
							AssetUid = model.AssetUid, 
							RoleUid = model.RoleUid, 
							SecurityType = model.SecurityType, 
							SecurityUid = model.SecurityUid, 
							Uid = overrideUid, 
							Context = model.Context
						}, 
						201, true, "Role assignment created successfully.");
				}
			}

			return response;
		}
		
		public async Task<RepositoryResponse<ReadRole>> CreateRoleAsync(CreateRole model)
		{
			RepositoryResponse<ReadRole> response;

			model.Name = (model.Name ?? "").Trim();
			if (!string.IsNullOrEmpty(model.Description))
			{
				model.Description = model.Description.Trim();
			}

			response = validateRole(model);
			if (response != null)
			{
				return response;
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var existingCount = await connection.QueryFirstAsync<int>(
					"select count(1) from [security].[Role] where Name = @Name", new { model.Name }
				);

				if (existingCount > 0)
				{
					return new(400, "A role with the same name already exists.");
				}

				response = new(new(), 201, true, "Role created successfully.");

				var role = await connection.QuerySingleAsync<ReadRole>(
						$@"
declare @roleId int;
insert into [security].[Role] ([Uid], [Name], Description, [Permissions], [CreatedBy], [CreatedOn], [UpdatedBy], [UpdatedOn])
values (@Uid, @Name, @Description, @Permissions, @u, @dt, @u, @dt);
select @roleId = SCOPE_IDENTITY();
select * from [security].[Role] where Id = @roleId;",
				new { Uid = Guid.NewGuid(), model.Name, model.Description, model.Permissions, u = CurrentUserId, dt = DateTime.UtcNow });

				response.Data = role;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ResponsibilityGetBreakdownByResourceModel>>> ReadAssetCountsByResourceAndRoleAsync(Guid resourceUid, Guid? roleUid)
		{
			RepositoryResponse<IEnumerable<ResponsibilityGetBreakdownByResourceModel>> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				string roleQuery = roleUid.HasValue ? "select @roleId = Id from security.[Role] where Uid = @RoleUid;" : "";
				string addedFilter = roleUid.HasValue ? "and ResponsibilityTypeID = @roleId" : "";

				var query = await connection.QueryAsync<ResponsibilityGetBreakdownByResourceModel>($@"
declare	@resourceId int,
		@roleId int;
select	@resourceId = ResourceID from reporting.Global_Resource where Uid = @resourceUid;
{roleQuery}
select	a.Name,
		a.Class,
		a.Uid as AssetTypeUid,
		agg.AssetCount
from	(
		select	AssetTypeID,
				count(1) as AssetCount
		from	ResponsibilitySummary
		where	ResourceID = @resourceId
				{addedFilter}
		group by AssetTypeID
		) agg
		inner join AssetType a on a.ID = agg.AssetTypeID",
					new { resourceUid, roleUid }
				);
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ResponsibilityBreakdownResponse>>> ReadAssetCountsByRoleAsync(Guid? roleUid)
		{
			RepositoryResponse<IEnumerable<ResponsibilityBreakdownResponse>> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				string roleQuery = roleUid.HasValue ? "select @roleId = Id from security.[Role] where Uid = @RoleUid;" : "";
				string addedFilter = roleUid.HasValue ? "where ResponsibilityTypeID = @roleId" : "";

				var query = await connection.QueryAsync<ResponsibilityBreakdownResponse>($@"
declare	@roleId int;
{roleQuery}
select	ResponsibilityTypeID, 
		ResponsibilityTypeUID, 
		ResponsibilityTypeName,
		count(1) as AssetCount
from	ResponsibilitySummary
{addedFilter} 
group by ResponsibilityTypeID, 
		ResponsibilityTypeUID, 
		ResponsibilityTypeName
order by ResponsibilityTypeName",
					new { roleUid }
				);
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<PermissionInfo>>> ReadPermissionsByAssetAsync(Guid assetUid)
		{
			RepositoryResponse<IEnumerable<PermissionInfo>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @"
declare @assetTypeId int, @assetId bigint, @DefaultPermissions int, @Permissions int
select @assetTypeId = AssetTypeID, @assetId = ID from Asset where Uid = @assetUid
select @DefaultPermissions = [DefaultPermissions] from AssetType where ID = @assetTypeId;

select	@Permissions = max([Permissions])
from	(
		select	o.[Permissions]
		from	[security].Owners o
				inner join [Group] g on g.Id = o.SecurityId and o.SecurityType = 1
				inner join ResourceGroup rg on rg.GroupId = g.Id and rg.ResourceID = @CurrentUserId and AssetId = @assetId
		union
		select	[Permissions]
		from	[security].Owners o
		where	SecurityType = 2 and SecurityId = @CurrentUserId and AssetId = @assetId
		union
		select	o.[Permissions]
		from	[security].TypeLevelOwners o
				inner join [Group] g on g.Id = o.SecurityId and o.SecurityType = 1
				inner join ResourceGroup rg on rg.GroupId = g.Id and rg.ResourceID = @CurrentUserId and AssetTypeId = @assetTypeId
		union
		select	[Permissions]
		from	[security].TypeLevelOwners o
		where	SecurityType = 2 and SecurityId = @CurrentUserId and AssetTypeId = @assetTypeId
		) p;

if @Permissions is null
begin
	set @permissions = @DefaultPermissions
end

select @Permissions";
				var permission = await connection.QueryFirstAsync<int>(sql, new { assetUid, CurrentUserId });

				var list = Permission.AddRelationships.GetList();
				list.ForEach(p => {
					p.Selected = ((permission & p.Value) == p.Value);
				});

				response.Data = list;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<PermissionInfo>>> ReadPermissionsByAssetTypeAsync(Guid assetTypeUid)
		{
			RepositoryResponse<IEnumerable<PermissionInfo>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @"
declare @assetTypeId int, @DefaultPermissions int, @Permissions int;
select @assetTypeId = ID, @DefaultPermissions = [DefaultPermissions] from AssetType where Uid = @assetTypeUid;

select	@Permissions = max([Permissions])
from	(
		select	o.[Permissions]
		from	[security].TypeLevelOwners o
				inner join [Group] g on g.Id = o.SecurityId and o.SecurityType = 1
				inner join ResourceGroup rg on rg.GroupId = g.Id and rg.ResourceID = @CurrentUserId and AssetTypeId = @assetTypeId
		union
		select	[Permissions]
		from	[security].TypeLevelOwners o
		where	SecurityType = 2 and SecurityId = @CurrentUserId and AssetTypeId = @assetTypeId
		) p;

if @Permissions is null
begin
	set @permissions = @DefaultPermissions
end

select @Permissions";
				var permission = await connection.QueryFirstAsync<int>(sql, new { assetTypeUid, CurrentUserId });

				var list = Permission.AddRelationships.GetList();
				list.ForEach(p => {
					p.Selected = ((permission & p.Value) == p.Value);
				});

				response.Data = list;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<AssetOwnerModel>>> ReadVisibleOwnersByAssetAsync(Guid assetUid) 
		{
			RepositoryResponse<IEnumerable<AssetOwnerModel>> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @"
set nocount on;
declare @assetTypeId int, @assetId bigint
select	@assetTypeId = AssetTypeID, 
		@assetId = ID 
from	dbo.Asset 
where	Uid = @assetUid

select	ResponsibilityUid as Uid,
		ResponsibilityTypeUid as RoleUid,
		ResponsibilityTypeName as RoleName,
		SecurityType,
		iif(RuleId = 0, 1, 0) as IsOverride,
		GroupUid,
		GroupName,
		ResourceUid,
		ResourceName,
		RuleName,
		Context
from	ResponsibilitySummary
where	((AssetId = @assetId and ApplyToType = 0) OR (AssetTypeId = @assetTypeId and ApplyToType = 1))
		and IsVisible = 1";
				response.Data = await connection.QueryAsync<AssetOwnerModel>(sql, new { assetUid });
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ReadSecurityPolicy>>> ReadPoliciesAsync()
		{
			RepositoryResponse<IEnumerable<ReadSecurityPolicy>> response = new([], 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @$"
{READ_POLICY_SQL_BASE}
order by	ru.Name
for json path;";

				var jsons = await connection.QueryAsync<string>(sql);
				var jsonPayload = string.Concat(jsons);

				var policies = JsonConvert.DeserializeObject<IEnumerable<ReadSecurityPolicy>>(jsonPayload);

				response.Data = policies;
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditOptionsAsync()
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryMultipleAsync(
					"select uid as [value], Name as [label] from security.[Role] order by Name; " +
					"select * from (" +
					"	select	uid as [value], " +
					"			case t.[Class] when 1 then 'Business' when 2 then 'Model' when 6 then 'Policy' when 7 then 'Rule' else 'Technical' end + ': ' + p.[Path] as [label] " +
					"	from	AssetType t " +
					"			cross apply dbo.GetAssetTypeTextPathById(t.Id, ' / ') p " +
					"	where	[Class] in @classes" +
					") o order by label; ",
					new { 
						classes = new List<int> { 
							(int)AssetTypeClass.BusinessAsset, 
							(int)AssetTypeClass.Model, 
							(int)AssetTypeClass.Policy, 
							(int)AssetTypeClass.Rule,
							(int)AssetTypeClass.TechnicalAsset
						}
					}
				);
				var roles = await query.ReadAsync<dynamic>();
				var assetTypes = await query.ReadAsync<dynamic>();
				response.Data = new { roles, assetTypes };
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditAssetTypeOptionsAsync(Guid assetTypeUid)
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryMultipleAsync(
					"declare @id int; select @id = Id from AssetType where Uid = @assetTypeUid; " +
					"select Name as [value], FriendlyName as [label], [type] from FieldType where AssetTypeID = @id and [Type] in ('Boolean', 'Number', 'Decimal', 'Text', 'Lookup'); " +
					"select uid as [value], ObjectName + ' (' + PredicateName + ')' as [label] from IntersectTypeDetail where SubjectAssetTypeID = @id union " +
					"select uid as [value], SubjectName + ' (' + PredicateInverse + ')' as [label] from IntersectTypeDetail where ObjectAssetTypeID = @id; ",
					new { assetTypeUid }
				);
				var fields = await query.ReadAsync<dynamic>();
				var intersectTypes = await query.ReadAsync<dynamic>();
				response.Data = new { fields, intersectTypes };
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditFieldLookupOptionsAsync(Guid assetTypeUid, string fieldName)
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>(
					@"
select	ta.Uid as [value],
		tap.DisplayPath as [label]
from	AssetType a
		inner join FieldType f on f.AssetTypeID = a.ID
		inner join AssetType tat on tat.Object = f.LookupObjectType + 'Type' and tat.ObjectID = f.LookupObjectID
		inner join Asset ta on ta.AssetTypeID = tat.ID
		inner join AssetPath tap on tap.ID = ta.ID
where	a.Uid = @assetTypeUid and f.Name = @fieldName
order by tap.DisplayPath",
					new { assetTypeUid, fieldName }
				);
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditRelationLookupOptionsAsync(Guid intersectTypeUid, Guid startingAssetTypeUid)
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>(
					@"
declare @id int, @sId int, @oId int;
select @id = Id from AssetType where Uid = @startingAssetTypeUid;
select @sId = SubjectAssetTypeID, @oId = ObjectAssetTypeID from IntersectType where Uid = @intersectTypeUid;
select @id = iif(@id = @sId, @oId, @sId)
select	a.Uid as [value], p.DisplayPath as [label]
from	Asset a inner join AssetPath p on p.ID = a.ID and a.AssetTypeID = @id
order by p.DisplayPath",
					new { intersectTypeUid, startingAssetTypeUid }
				);
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditGroupOptionsAsync()
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>("select a.uid as [value], g.Name as [label] from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID order by g.Name");
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditUserOptionsAsync()
		{
			RepositoryResponse<dynamic> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>("select uid as [value], FirstName + ' ' + LastName + ' (' + Email + ')' as [label] from reporting.Global_Resource where State = 1 order by LastName, FirstName, Email");
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<Role>> ReadRawRoleAsync(Guid uid)
		{
			RepositoryResponse<Role> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				response.Data = await connection.QueryFirstOrDefaultAsync<Role>(
					"select * from security.[Role] where Uid = @uid",
					new { uid }
				);
				if (response.Data == null)
				{
					response.IsSuccess = false;
					response.StatusCode = 404;
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRolesAsync()
		{
			RepositoryResponse<IEnumerable<ReadRole>> response = new(null, 200, true);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				response.Data = await connection.QueryAsync<ReadRole>(
					"select uid, Name, Description, [Permissions], UpdatedOn from security.[Role] order by Name"
				);
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<dynamic>>> ReadGroupsAndUsersAsSecurityAsync(Guid assetUid, bool includeInternalUsers = false)
		{
			RepositoryResponse<IEnumerable<dynamic>> response = new(null, 200, true);

			string filter = "";
			if (!includeInternalUsers)
			{
				filter = " and Email not like '%infogix.com' and Email not like '%precisely.com' and Email not like '%syncsort.com'";
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				response.Data = await connection.QueryAsync<dynamic>(@"
declare	@assetId bigint, 
		@assetTypeId int;
select	@assetid = Id, 
		@assetTypeId = AssetTypeId 
from	dbo.Asset 
where	Uid = @assetUid;

select * from (
	select	1 as SecurityType, Uid, Name 
	from	[Group] 
	where	ID not in (
				select	SecurityID 
				from	ResponsibilitySummary 
				where	SecurityType = 1 
						and (
							(AssetId = @assetId and ApplyToType = 0) or (AssetTypeId = @assetTypeId and ApplyToType = 1)
						)
				)
			)
	union
	select	2 as SecurityType, Uid, FirstName + ' ' + LastName as Name 
	from	reporting.Global_Resource 
	where	State = 1
			and ResourceID not in (
				select	SecurityID 
				from	ResponsibilitySummary 
				where	SecurityType = 2 
						and (
							(AssetId = @assetId and ApplyToType = 0) or (AssetTypeId = @assetTypeId and ApplyToType = 1)
						)
				) {filter}
) o 
order by SecurityType asc, Name asc;
", new { assetUid });
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveOverrideAsync(Guid uid)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var @override = await connection.QueryFirstOrDefaultAsync<SecurityPolicyOverride>("select * from security.[Override] where Id = @uid;", new { uid });
				if (@override == null)
				{ 
					return new(404, "No matching override found based on uid.");
				}
				
				await connection.OpenAsync();
				using (var trans = connection.BeginTransaction())
				{
					await connection.ExecuteAsync("delete [security].[Override] where Id = @uid;", new { uid }, trans);
					await connection.UpdateChangeLogForAsset(@override.AssetId, CurrentUserId, ChangeLogObject.RoleAssignment, ChangeLogAction.Removed, 
						new { securityType = @override.SecurityType, securityId = @override.SecurityId, roleId = @override.RoleId }, trans);
					trans.Commit();
				}
				response = new(true, 200, true, "Role assignment removed successfully.");
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveOverridesByGroupAsync(int groupId)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				int impactedRows = await connection.ExecuteScalarAsync<int>("delete [security].[Override] where SecurityType = 1 and SecurityId = @groupId;", new { groupId });

				response = (impactedRows > 0) ?
					new(true, 200, true, "Role assignments removed successfully.") :
					new(false, 404, false, "Role assignments not found.");
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveOverridesByUserAsync(int userId)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				int impactedRows = await connection.ExecuteScalarAsync<int>("delete [security].[Override] where SecurityType = 2 and SecurityId = @userId;", new { userId });

				response = (impactedRows > 0) ?
					new(true, 200, true, "Role assignments removed successfully.") :
					new(false, 404, false, "Role assignments not found.");
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveOverridesByAssetRoleAndUsersAsync(long assetId, int roleId, List<Guid> users)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				int impactedRows = await connection.ExecuteScalarAsync<int>(
					"declare @userIds table(Id int);" +
					"insert into @userIds " +
					"	select ResourceID from reporting.Global_Resource where Uid in @users;" +
					"delete [security].[Override] where RoleId = @roleId and AssetId = @assetId and SecurityType = 2 and SecurityId in (select Id from @userIds);", 
					new { assetId, roleId, users }
					);

				response = (impactedRows > 0) ?
					new(true, 200, true, "Role assignments removed successfully.") :
					new(false, 404, false, "Role assignments not found.");
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemovePolicyAsync(Guid uid, bool softDelete = true)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var ruleId = await connection.QueryFirstAsync<int>(
					"declare @id int = 0; " +
					"select @id = Id from [security].[Rule] where Uid = @uid;" +
					"select @id;", new { uid }
					);

				if (ruleId == 0)
				{
					return new(404, "No matching rule found based on uid.");
				}

				if (softDelete)
				{
					await connection.ExecuteAsync("update [security].[Rule] set [State] = 3 where Id = @ruleId", new { ruleId });
				}
				else 
				{
					await connection.ExecuteAsync(
						"delete o from [security].RuleAssignment o inner join [security].[Rule] r on r.Id = o.RuleId and r.Id = @ruleId; " +
						"delete o from [security].RuleWhen o inner join [security].[Rule] r on r.Id = o.Id and r.Id = @ruleId; " +
						"delete o from [security].RuleThen o inner join [security].[Rule] r on r.Id = o.Id and r.Id = @ruleId; " +
						"delete [security].[Rule] where Id = @ruleId; ",
						new { ruleId }
					);
				}

				response = new(true, 200, true, "Policy removed successfully.");
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemoveRoleAsync(Guid uid)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var roleId = await connection.QueryFirstAsync<int>(
					"select Id from [security].[Role] where Uid = @uid;", new { uid }
					);

				if (roleId == 0)
				{
					return new(404, "No matching role found based on uid.");
				}

				int policyCount = await connection.QueryFirstAsync<int>(
					"select count(1) from security.[Rule] ru inner join [security].[Role] ro on ro.Uid = @uid and ro.Id = ru.RoleId and ru.State <> 3;", 
					new { uid }
					);

				if (policyCount > 0)
				{
					return new(409, "One or more security policies exist that are associated to this role. Remove these first.");
				}

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on r.Id = o.Id and r.RoleId = @roleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on r.Id = o.Id and r.RoleId = @roleId; " + 
					"delete [security].[Rule] where RoleId = @roleId; " +
					"delete [security].[Role] where Id = @roleId; ",
					new { roleId }
				);

				response = new(true, 200, true, "Role removed successfully.");
			}

			return response;
		}

		public async Task RunPolicyAsync(Guid? assetUid = null, Guid? executionUid = null, Guid? policyUid = null)
		{
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				long? assetId = null;
				long? executionId = null;
				long? ruleId = null;

				if (assetUid.HasValue)
				{
					assetId = await connection.QueryFirstAsync<long>(
						"select Id from [dbo].[Asset] where Uid = @assetUid;", new { assetUid }
						);
				}
				if (executionUid.HasValue)
				{
					executionId = await connection.QueryFirstAsync<long>(
						"select Id from [api].[Execution] where ExecutionID = @executionUid;", new { executionUid }
						);
				}
				if (policyUid.HasValue) 
				{
					ruleId = await connection.QueryFirstAsync<long>(
						"select Id from [security].[Rule] where Uid = @policyUid;", new { policyUid }
						);
				}

				await connection.ExecuteAsync(
					"exec security.RunRules @assetId, @ruleId, @executionId", 
					new { assetId, ruleId, executionId }, 
					commandTimeout: 1800
					);
			}
		}

		public async Task<RepositoryResponse<ReadSecurityPolicy>> UpdatePolicyAsync(Guid uid, ReadSecurityPolicy model)
		{
			RepositoryResponse<ReadSecurityPolicy> response;

			response = validatePolicy(model);
			if (response != null)
			{
				return response;
			}
			model.Name = (model.Name ?? "").Trim();

			var IntersectTypeUids = model.When.Where(w => w.IntersectTypeUid.HasValue).Select(w => w.IntersectTypeUid.Value).ToList();
			var AssetUids = model.When.Where(w => w.AssetUid.HasValue).Select(w => w.AssetUid.Value).ToList();
			var SecurityUids = model.Then.Where(w => w.SecurityUid.HasValue).Select(w => w.SecurityUid.Value).ToList();
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var query = await connection.QueryMultipleAsync(
					"declare @id int, @roleId int, @assetTypeId int; " +
					"select @assetTypeId = Id from AssetType where Uid = @AssetTypeUid; " +
					"select @id = Id from [security].[Rule] where Uid = @uid; " +
					"select @roleId = Id from [security].[Role] where Uid = @RoleUid; " +
					"select @assetTypeId; select @id; select @roleId; " +
					"select count(1) from [security].[Rule] where Id <> @id and RoleId = @roleId and Name = @Name; " +
					"select * from security.RuleThen where Id = @id;" +
					"select * from security.RuleWhen where Id = @id;" +
					"select f.* from FieldType f inner join AssetType a on a.ID = f.AssetTypeID and a.Uid = @AssetTypeUid; " +
					"select i.* from IntersectType i inner join AssetType a on (a.ID = i.SubjectAssetTypeID or a.ID = i.ObjectAssetTypeID) and a.Uid = @AssetTypeUid and I.Uid in @IntersectTypeUids; " +
					"select * from FieldType where Object in ('GroupType'); " +
					"select * from FieldType where Object in ('ResourceType'); " +
					"select * from Asset where Uid in @AssetUids; " +
					"select * from [Group] where Uid in @SecurityUids; " +
					"select * from reporting.Global_Resource where Uid in @SecurityUids;", new { uid, model.RoleUid, model.AssetTypeUid, model.Name, IntersectTypeUids, AssetUids, SecurityUids }
					);
				int assetTypeId = await query.ReadSingleAsync<int>();
				int ruleId = await query.ReadSingleAsync<int>();
				int roleId = await query.ReadSingleAsync<int>();
				int matchingAlternateRuleCount = await query.ReadSingleAsync<int>();
				var existingThens = await query.ReadAsync<RuleThen>();
				var existingWhens = await query.ReadAsync<RuleWhen>();
				var assetTypeFields = await query.ReadAsync<FieldType>();
				var intersectTypes = await query.ReadAsync<IntersectType>();
				var groupFields = await query.ReadAsync<FieldType>();
				var userFields = await query.ReadAsync<FieldType>();
				var whenAssets = await query.ReadAsync<Asset>();
				var groups = await query.ReadAsync<d360.core.entities.Group>();
				var users = await query.ReadAsync<GlobalReportingResource>();

				var securityType = model.SecurityType;

				if (ruleId == 0)
				{
					return new(404, "No matching rule found based on uid.");
				}

				if (roleId == 0)
				{
					return new(404, "No matching role found based on uid.");
				}

				if (assetTypeId == 0)
				{
					return new(404, "No matching asset type found based on uid.");
				}

				if (matchingAlternateRuleCount > 0)
				{
					return new(409, "Another rule found with this name and role.");
				}

				var rawWhens = new List<RuleWhen>();
				if (response == null && model.When.Count > 0)
				{
					(rawWhens, response) = validatePolicyWhenConditions(model.When, assetTypeId, intersectTypes, assetTypeFields, whenAssets);
				}

				var rawThens = new List<RuleThen>();
				if (response == null && model.Then.Count > 0)
				{
					(rawThens, response) = validatePolicyThenConditions(model.Then, securityType, groups, groupFields, users, userFields);
				}

				if (response == null)
				{
					response = new(new(), 200, true, "Policy updated successfully.");
					var dt = DateTime.UtcNow;

					await connection.OpenAsync();
					using (var trans = connection.BeginTransaction())
					{ 
						connection.Execute(
							"update [security].[Rule] set Name = @Name, IsVisible = @IsVisible, RoleId = @roleId, SecurityType = @securityType, AssetTypeId = @assetTypeId, [UpdatedBy] = @u, [UpdatedOn] = @dt where Id = @ruleId; ",
							new { roleId, model.Name, model.IsVisible, ruleId, securityType = (int)securityType, assetTypeId, u = CurrentUserId, dt }, 
							transaction: trans
						);

						#region When Processing

						connection.Execute("delete [security].RuleWhen where Id = @ruleId; ", new { ruleId }, transaction: trans);
						//var uWhens = from e in existingWhens
						//			 join r in rawWhens on e.CheckType equals r.CheckType
						//			 where e.Position == r.Position && e.FieldTypeId == r.FieldTypeId && e.IntersectTypeId == r.IntersectTypeId
						//			 select new { e.Position, r.Value, r.AssetId, r.Operator };
						//var cWhens = (from r in rawWhens
						//			  where !existingWhens.Any(e => e.Position == r.Position && e.CheckType == r.CheckType && e.FieldTypeId == r.FieldTypeId && e.IntersectTypeId == r.IntersectTypeId)
						//			  select r).ToList();
						//var dWhens = (from e in existingWhens
						//			  where !rawWhens.Any(r => r.Position == e.Position && r.CheckType == e.CheckType && r.FieldTypeId == e.FieldTypeId && r.IntersectTypeId == e.IntersectTypeId)
						//			  select e).ToList();
						//// Update existing whens
						//uWhens.ForEach(u => {
						//	connection.Execute(
						//		"update [security].RuleWhen " +
						//		"set [Operator] = @Operator, [Value] = @Value, AssetId = @AssetId " +
						//		"where Id = @ruleId and [Position] = @Position;",
						//		new { ruleId, u.Position, Operator = (int)u.Operator, u.Value, u.AssetId },
						//		transaction: trans);
						//});
						//// Create new whens
						//cWhens.ForEach(c =>
						//{
						//	connection.Execute(
						//		"insert into [security].RuleWhen (Id, [Position], CheckType, FieldTypeId, IntersectTypeId, [Operator], [Value], AssetId) " +
						//		"values (@ruleId, @Position, @CheckType, @FieldTypeId, @IntersectTypeId, @Operator, @Value, @AssetId)",
						//		new { ruleId, c.Position, c.CheckType, c.FieldTypeId, c.IntersectTypeId, Operator = (int)c.Operator, c.Value, c.AssetId },
						//		transaction: trans);
						//});
						//// Remove old whens
						//dWhens.ForEach(d =>
						//{
						//	connection.Execute("delete [security].RuleWhen where Id = @ruleId and [Position] = @Position;", new { ruleId, d.Position }, transaction: trans);
						//});

						rawWhens.ForEach(w =>
						{
							w.Id = ruleId;
							connection.Execute(
								"insert into [security].RuleWhen (Id, [Position], CheckType, FieldTypeId, IntersectTypeId, [Operator], [Value], AssetId) " +
								"values (@Id, @Position, @CheckType, @FieldTypeId, @IntersectTypeId, @Operator, @Value, @AssetId)",
								new { w.Id, w.Position, w.CheckType, w.FieldTypeId, w.IntersectTypeId, Operator = (int)w.Operator, w.Value, w.AssetId },
								transaction: trans);
						});

						#endregion When Processing

						connection.Execute("delete [security].RuleThen where Id = @ruleId; ", new { ruleId }, transaction: trans);
						rawThens.ForEach(t => {
							t.Id = ruleId;
							connection.Execute(
								"insert into [security].RuleThen (Id, [Position], FieldTypeId, [Operator], [Value], SecurityId) " +
								"values (@Id, @Position, @FieldTypeId, @Operator, @Value, @SecurityId)",
								new { t.Id, t.Position, t.FieldTypeId, Operator = (int)t.Operator, t.Value, t.SecurityId },
								transaction: trans);
						});

						trans.Commit();
					}

					response.Data = model;				
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpdateOverrideAsync(Guid uid, UpdateSecurityPolicyOverride model)
		{
			RepositoryResponse<bool> response = null;

			if (uid == Guid.Empty)
			{
				return new(400, "Uid is invalid.");
			}

			if (model == null)
			{
				return new(400, "No valid data to update override.");
			}

			if (!string.IsNullOrEmpty(model.Context))
			{
				model.Context = (model.Context ?? "").Trim().RemoveHtml();
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				// Data for validation.
				Guid? overrideId = await connection.QueryFirstOrDefaultAsync<Guid>(
					"select Id from [security].[Override] where Id = @uid; ",
					new { uid }
				);

				if (!overrideId.HasValue)
				{
					response = new(404, "Could not find assignment based on Uid provided.");
				}

				if (response == null)
				{
					await connection.OpenAsync();
					connection.Execute(
						"update [security].[Override] " +
						"set Context = @Context, UpdatedBy = @UpdatedBy, UpdatedOn = getutcdate() " +
						"where Id = @uid; ",
						new { 
							uid, 
							Context = model.Context.ReplaceHtmlEntities(),
							UpdatedBy = CurrentUserId
						});
					response = new RepositoryResponse<bool>(true, 200, true, "Override updated successfully.");
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadRole>> UpdateRoleAsync(Guid uid, CreateRole model)
		{
			RepositoryResponse<ReadRole> response;
			
			model.Name = (model.Name ?? "").Trim();
			if (!string.IsNullOrEmpty(model.Description))
			{
				model.Description = model.Description.Trim();
			}

			response = validateRole(model);
			if (response != null)
			{
				return response;
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var query = await connection.QueryMultipleAsync(
					"declare @id int; " +
					"select @id = Id from [security].[Role] where Uid = @uid; " +
					"select @id; " +
					"select count(1) from [security].[Role] where Id <> @id and Name = @Name; ", new { uid, model.Name }
					);
				int roleId = await query.ReadSingleAsync<int>();
				int matchingAlternateRoleCount = await query.ReadSingleAsync<int>();

				if (roleId == 0)
				{
					return new(404, "No matching role found based on uid.");
				}

				if (matchingAlternateRoleCount > 0)
				{
					return new(409, "Another role found with this name.");
				}

				response = new(new(), 200, true, "Role updated successfully.");

				var dt = DateTime.UtcNow;
				await connection.ExecuteAsync(
					"update [security].[Role] " +
					"set Name = @Name, Description = @Description, [Permissions] = @Permissions, [UpdatedBy] = @u, [UpdatedOn] = @dt " +
					"where Id = @roleId;",
					new { roleId, model.Name, model.Description, model.Permissions, u = CurrentUserId, dt }
				);

				response.Data = new() { Description = model.Description, Name = model.Name, Uid = uid, UpdatedOn = dt };
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpsertOverridesByAssetRoleAndUsersAsync(long assetId, int roleId, List<Guid> users)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				int impactedRows = await connection.ExecuteAsync(
					"declare @dt datetime = getutcdate();" +
					"declare @securityAssets table([Type] int, Id int);" +
					"insert into @securityAssets " +
					"	select 2, ResourceID from reporting.Global_Resource where Uid in @users;" +
					"insert into @securityAssets " +
					"	select 1, ID from [Group] where Uid in @users;" +
					"insert into [security].[Override] (RoleId, SecurityType, SecurityId, AssetId, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
					"	select	@roleId, u.Type, u.Id, @assetId, @CurrentUserId, @dt, @CurrentUserId, @dt" +
					"	from	@securityAssets u" +
					"			left join [security].[Override] e on e.RoleId = @roleId and e.AssetId = @assetId and e.SecurityType = u.Type and e.SecurityId = u.Id" +
					"	where	e.Id is null;",
					new { assetId, roleId, users, CurrentUserId }
					);

				response = (impactedRows > 0) ?
					new(true, 200, true, "Role assignments merged successfully.") :
					new(false, 404, false, "Role assignments could not be merged.");
			}

			return response;
		}

		RepositoryResponse<ReadSecurityPolicy> validatePolicy(ISecurityPolicy model)
		{
			RepositoryResponse<ReadSecurityPolicy> result = null;

			if (model == null)
			{
				result = new(400, "No valid data to create security policy.");
			}

			if (result != null)
			{
				model.Name = (model.Name ?? "").Trim();
				if (string.IsNullOrEmpty(model.Name))
				{
					result = new(400, "Name must be populated.");
				}
			}

			if (result != null)
			{
				if (model.Name.Length < 3 || model.Name.Length > 250)
				{
					result = new(400, "Name must longer than three characters and less than 250 characters.");
				}
			}

			if (result != null)
			{
				if (!model.ApplyToType && (model.When == null || (model.When != null && model.When.Count == 0)))
				{
					result = new(400, "If rule does not apply to entire type, then you must apply asset filtering.");
				}
			}

			if (result != null)
			{
				if (model.Then == null || (model.Then != null && model.Then.Count == 0))
				{
					result = new(400, "You must apply user/group assignments.");
				}
			}

			if (result != null)
			{
				if (model.When != null && model.When.Any(w => !string.IsNullOrEmpty(w.FieldName) && w.IntersectTypeUid.HasValue))
				{
					result = new(400, "Each asset filter may only have a FieldName or an IntersectTypeUid populated, but not both.");
				}
			}

			if (result != null)
			{
				if (model.When != null && model.When.Any(w => w.IntersectTypeUid.HasValue && !w.AssetUid.HasValue))
				{
					result = new(400, "Each asset filter that has a populated IntersectTypeUid must also have a populated AssetUid.");
				}
			}

			if (result != null)
			{
				if (model.When != null && model.When.Any(w => !string.IsNullOrEmpty(w.FieldName) && (!w.AssetUid.HasValue && string.IsNullOrEmpty(w.Value))))
				{
					result = new(400, "Each asset filter that has a populated FieldName must also have either a populated AssetUid or a Value.");
				}
			}

			return result;
		}

		(List<RuleThen>, RepositoryResponse<ReadSecurityPolicy>) validatePolicyThenConditions(List<SecurityPolicyThen> conditions,
			RuleSecurityType securityType,
			IEnumerable<d360.core.entities.Group> groups,
			IEnumerable<FieldType> groupFields,
			IEnumerable<GlobalReportingResource> users,
			IEnumerable<FieldType> userFields)
		{
			RepositoryResponse<ReadSecurityPolicy> response = null;
			var rawThens = new List<RuleThen>();

			if (conditions.Count > 0)
			{
				int position = 0;
				conditions.ForEach(t =>
				{
					position++;
					if (response == null) // Once we have an error, just stop.
					{
						var rawThen = new RuleThen { Operator = t.Operator, Position = position };

						if (t.SecurityUid.HasValue)
						{
							// Direct security object assignment.

							if (securityType == RuleSecurityType.Group)
							{
								// Check groups
								var group = groups.SingleOrDefault(g => g.Uid == t.SecurityUid);
								if (group != null)
								{
									rawThen.SecurityId = group.ID;
								}
								else
								{
									response = new(404, "Could not find group based on SecurityUid provided.");
								}
							}
							else
							{
								// Check users
								var user = users.SingleOrDefault(u => u.Uid == t.SecurityUid);
								if (user != null)
								{
									rawThen.SecurityId = user.ResourceID;
								}
								else
								{
									response = new(404, "Could not find user based on SecurityUid provided.");
								}
							}
						}
						else
						{
							// Filter security object assign (non-direct)
							FieldType field = null;
							if (securityType == RuleSecurityType.Group)
							{
								field = groupFields.SingleOrDefault(f => f.Name == t.FieldName);
							}
							else
							{
								field = userFields.SingleOrDefault(f => f.Name == t.FieldName);
							}
							if (field != null)
							{
								if (VALID_FIELDS.Contains(field.Type))
								{
									rawThen.FieldTypeId = field.ID;
								}
								else
								{
									response = new(409, "Selected field not supported in security object filters based on its type.");
								}
							}
							else
							{
								response = new(404, "Could not find field based on FieldName provided.");
							}
						}

						if (response == null)
						{
							rawThens.Add(rawThen);
						}
					}
				});
			}

			return (rawThens, response);
		}

		(List<RuleWhen>, RepositoryResponse<ReadSecurityPolicy>) validatePolicyWhenConditions(List<SecurityPolicyWhen> conditions,
			int policyAssetTypeId,
			IEnumerable<IntersectType> intersectTypes,
			IEnumerable<FieldType> assetTypeFields,
			IEnumerable<Asset> whenAssets)
		{
			RepositoryResponse<ReadSecurityPolicy> response = null;
			var rawWhens = new List<RuleWhen>();

			if (conditions.Count > 0)
			{
				int position = 0;
				conditions.ForEach(w =>
				{
					position++;
					if (response == null) // Once we have an error, just stop.
					{
						var rawWhen = new RuleWhen { CheckType = w.CheckType[0], Operator = w.Operator, Position = position };

						if (string.IsNullOrEmpty(w.FieldName))
						{
							// Check intersect type.
							rawWhen.CheckType = 'R';

							var intersectType = intersectTypes.SingleOrDefault(i => i.uid == w.IntersectTypeUid);
							if (intersectType != null)
							{
								rawWhen.IntersectTypeId = intersectType.ID;

								var targetAssetTypeId = intersectType.SubjectAssetTypeID == policyAssetTypeId ? intersectType.ObjectAssetTypeID : intersectType.SubjectAssetTypeID;
								var whenAsset = whenAssets.SingleOrDefault(a => a.AssetTypeID == targetAssetTypeId && a.uid == w.AssetUid);
								if (whenAsset != null)
								{
									rawWhen.AssetId = whenAsset.ID;
								}
								else
								{
									response = new(404, "Could not find target asset in filter conditions based on AssetUid provided.");
								}
							}
							else
							{
								response = new(404, "Could not find intersect type based on IntersectTypeUid provided.");
							}
						}
						else
						{
							// Check field.
							rawWhen.CheckType = 'F';

							var field = assetTypeFields.SingleOrDefault(f => f.Name == w.FieldName);
							if (field != null)
							{
								if (VALID_FIELDS.Contains(field.Type))
								{
									rawWhen.FieldTypeId = field.ID;
									// should we check to see if asset is from valid lookup?
									if (w.AssetUid.HasValue && field.Type == "Lookup")
									{
										var whenAsset = whenAssets.SingleOrDefault(a => a.uid == w.AssetUid);
										if (whenAsset != null)
										{
											rawWhen.AssetId = whenAsset.ID;
										}
										else
										{
											response = new(404, "Could not find target asset in filter conditions based on AssetUid provided.");
										}
									}
									else
									{
										rawWhen.Value = w.Value;
									}
								}
								else
								{
									response = new(409, "Selected field not supported in asset filters based on its type.");
								}
							}
							else
							{
								response = new(404, "Could not find field based on FieldName provided.");
							}
						}

						// Add to the list of when if no errors found.
						if (response == null)
						{
							rawWhens.Add(rawWhen);
						}
					}
				});
			}

			return (rawWhens, response);
		}

		RepositoryResponse<ReadRole> validateRole(CreateRole model)
		{
			if (model.Permissions <= 0)
			{
				return new(400, "Permissions must have a value greater than 0.");
			}
			if (model.Name.Length > 250)
			{
				return new(400, "Name property must be less than 250 characters.");
			}
			if ((model.Description??"").Length > 4000)
			{
				return new(400, "Description property must be less than 4000 characters.");
			}
			if (string.IsNullOrEmpty(model.Name))
			{
				return new(400, "Name must be populated.");
			}
			if (model.Name.Length < 3)
			{
				return new(400, "Name must longer than three characters.");
			}

			return null;
		}
	}
}
