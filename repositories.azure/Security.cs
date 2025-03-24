using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.security;
using Dapper;
using System.Data;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
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
							"insert into [security].[Rule] (Uid, Name, RoleId, SecurityType, AssetTypeId, ApplyToType, IsVisible, IsOverride, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
							"values (@Uid, @Name, @roleId, @securityType, @assetTypeId, @ApplyToType, @IsVisible, @IsOverride, @u, @dt, @u, @dt); " +
							"select SCOPE_IDENTITY();", 
							new { Uid = Guid.NewGuid(), model.Name, roleId, securityType = (int)securityType, assetTypeId, model.ApplyToType, model.IsVisible, IsOverride = false, u = CurrentUserId, dt = DateTime.UtcNow }, 
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
					}
					response = new(new ReadSecurityPolicy(), 201, true, "Policy created successfully.");
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadSecurityPolicyOverride>> CreatePolicyOverrideAsync(CreateSecurityPolicyOverride model)
		{
			RepositoryResponse<ReadSecurityPolicyOverride> response = null;

			if (model == null)
			{
				return new(400, "No valid data to create rule.");
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				d360.core.security.Rule rawRule = new() { 
					ApplyToType = false, IsOverride = true, IsVisible = true, 
					CreatedBy = CurrentUserId, CreatedOn = DateTime.UtcNow, Name = "", UpdatedBy = CurrentUserId, UpdatedOn = DateTime.UtcNow, 
					Uid = Guid.NewGuid() 
				};
				RuleWhen rawRuleWhen = new() { CheckType = 'S', Operator = Operator.Equals, Position = 1 };
				RuleThen rawRuleThen = new() { Operator = Operator.Equals, Position = 1 };

				// Data for validation.
				rawRule.SecurityType = model.SecurityType;
				var securityQuery = rawRule.SecurityType == RuleSecurityType.Group ?
					"select g.Id from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID where a.Uid = @SecurityUid;" :
					"select ResourceId from reporting.Global_Resource where Uid = @SecurityUid;";
				var qryData = await connection.QueryMultipleAsync(
					"select AssetTypeId from Asset where Uid = @AssetUid; " +
					"select Id from [security].[Role] where Uid = @RoleUid; " +
					"select Id from Asset where Uid = @AssetUid; " +
					securityQuery,
					new { model.RoleUid, model.AssetUid, model.SecurityUid }
				);
				rawRule.AssetTypeId = await qryData.ReadFirstOrDefaultAsync<int>();
				rawRule.RoleId = await qryData.ReadFirstOrDefaultAsync<int>();
				rawRuleWhen.AssetId = await qryData.ReadFirstOrDefaultAsync<long>();
				rawRuleThen.SecurityId = await qryData.ReadFirstOrDefaultAsync<int>();

				if (rawRule.AssetTypeId == 0)
				{
					response = new(404, "Could not find asset type based on AssetTypeUid provided.");
				}

				if (response == null && rawRule.RoleId == 0)
				{
					response = new(404, "Could not find role based on RoleUid provided.");
				}

				if (response == null && rawRuleWhen.AssetId == 0)
				{
					response = new(404, "Could not find asset based on AssetUid provided.");
				}

				if (response == null && rawRuleThen.SecurityId == 0)
				{
					if (rawRule.SecurityType == RuleSecurityType.Group)
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
					using (var trans = connection.BeginTransaction())
					{
						long ruleId = connection.QuerySingle<long>(
						"insert into [security].[Rule] (Uid, Name, RoleId, SecurityType, AssetTypeId, ApplyToType, IsVisible, IsOverride, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
						"values (@Uid, @Name, @RoleId, @SecurityType, @AssetTypeId, @ApplyToType, @IsVisible, @IsOverride, @CreatedBy, @CreatedOn, @UpdatedBy, @UpdatedOn); " +
						"select SCOPE_IDENTITY();",
						rawRule, 
						trans);

						rawRuleWhen.Id = ruleId;
						connection.Execute(
							"insert into [security].RuleWhen (Id, [Position], CheckType, [Operator], AssetId) values (@Id, @Position, @CheckType, @Operator, @AssetId)",
							new { rawRuleWhen.Id, rawRuleWhen.Position, rawRuleWhen.CheckType, Operator = (int)rawRuleWhen.Operator, rawRuleWhen.AssetId }, 
							trans);

						rawRuleThen.Id = ruleId;
						connection.Execute(
							"insert into [security].RuleThen (Id, [Position], [Operator], SecurityId) values (@Id, @Position, @Operator, @SecurityId)",
							new { rawRuleThen.Id, rawRuleThen.Position, Operator = (int)rawRuleThen.Operator, rawRuleThen.SecurityId }, 
							trans);

						trans.Commit();
					}
					
					response = new RepositoryResponse<ReadSecurityPolicyOverride>(
						new ReadSecurityPolicyOverride { 
							AssetUid = model.AssetUid, 
							RoleUid = model.RoleUid, 
							SecurityType = model.SecurityType, 
							SecurityUid = model.SecurityUid, 
							Uid = rawRule.Uid 
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
			RepositoryResponse<IEnumerable<AssetOwnerModel>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @"
declare @assetTypeId int, @assetId bigint
select @assetTypeId = AssetTypeID, @assetId = ID from Asset where Uid = @assetUid

select	o.RuleUid,
		o.RoleUid,
		o.RoleName,
		o.SecurityType,
		coalesce(u.Uid, u.Uid) as SecurityUid,
		coalesce(g.Name, u.FirstName + ' ' + u.LastName) as SecurityName,
		o.IsVisible,
		o.IsOverride
from	[security].Owners o
		left join [Group] g on g.Id = o.SecurityId and o.SecurityType = 1
		left join reporting.Global_Resource u on u.ResourceId = o.SecurityId and o.SecurityType = 2
where	AssetId = @assetId and o.IsVisible = 1
union
select	o.RuleUid,
		o.RoleUid,
		o.RoleName,
		o.SecurityType,
		coalesce(u.Uid, u.Uid) as SecurityUid,
		coalesce(g.Name, u.FirstName + ' ' + u.LastName) as SecurityName,
		o.IsVisible,
		o.IsOverride
from	[security].TypeLevelOwners o
		left join [Group] g on g.Id = o.SecurityId and o.SecurityType = 1
		left join reporting.Global_Resource u on u.ResourceId = o.SecurityId and o.SecurityType = 2
where	AssetTypeId = @assetTypeId and o.IsVisible = 1";
				response.Data = await connection.QueryAsync<AssetOwnerModel>(sql, new { assetUid });
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ReadSecurityPolicy>>> ReadPoliciesAsync()
		{
			RepositoryResponse<IEnumerable<ReadSecurityPolicy>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = @"
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
		inner join [security].[Role] ro on ro.Id = ru.RoleId and ru.IsOverride = 0
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
		) rt
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
			RepositoryResponse<dynamic> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryMultipleAsync(
					"select uid as [value], Name as [label] from security.[Role]; " +
					"select uid as [value], case t.[Class] when 1 then 'Business' when 2 then 'Model' when 6 then 'Policy' when 7 then 'Rule' else 'Technical' end + ': ' + p.[Path] as [label] from AssetType t cross apply dbo.GetAssetTypeTextPathById(t.Id, ' / ') p where [Class] in @classes; ",
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
			RepositoryResponse<dynamic> response = new(200);

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
			RepositoryResponse<dynamic> response = new(200);

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
			RepositoryResponse<dynamic> response = new(200);

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
			RepositoryResponse<dynamic> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>("select a.uid as [value], g.Name as [label] from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID order by g.Name");
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<dynamic>> ReadPolicyEditUserOptionsAsync()
		{
			RepositoryResponse<dynamic> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryAsync<dynamic>("select uid as [value], FirstName + ' ' + LastName + ' (' + Email + ')' as [label] from reporting.Global_Resource where State = 1 order by LastName, FirstName, Email");
				response.Data = query;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRolesAsync()
		{
			RepositoryResponse<IEnumerable<ReadRole>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				response.Data = await connection.QueryAsync<ReadRole>(
					"select uid, Name, Description, [Permissions], UpdatedOn from security.[Role] order by Name"
				);
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemovePolicyAsync(Guid uid)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var ruleId = await connection.QueryFirstAsync<int>(
					"declare @id int; " +
					"select @id = Id from [security].[Rule] where Uid = @uid;", new { uid }
					);

				if (ruleId == 0)
				{
					return new(404, "No matching rule found based on uid.");
				}

				response = new(true, 200, true, "Policy removed successfully.");

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on on r.Id = o.Id and r.Id = @ruleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on on r.Id = o.Id and r.Id = @ruleId; " +
					"delete [security].[Rule] where RoleId = @ruleId; ",
					new { ruleId }
				);
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> RemovePolicyOverrideAsync(Guid uid)
		{
			RepositoryResponse<bool> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var ruleId = await connection.QueryFirstAsync<int>(
					"select Id from [security].[Rule] where IsOverride = 1 and Uid = @uid;", new { uid }
					);

				if (ruleId == 0)
				{
					return new(404, "No matching rule found based on uid.");
				}

				response = new(true, 200, true, "Role assignment removed successfully.");

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on r.Id = o.Id and r.Id = @ruleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on r.Id = o.Id and r.Id = @ruleId; " +
					"delete [security].[Rule] where Id = @ruleId; ",
					new { ruleId }
				);
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

				response = new(true, 200, true, "Role removed successfully.");

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on r.Id = o.Id and r.RoleId = @roleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on r.Id = o.Id and r.RoleId = @roleId; " + 
					"delete [security].[Rule] where RoleId = @roleId; " +
					"delete [security].[Role] where Id = @roleId; ",
					new { roleId }
				);
			}

			return response;
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
							"update [security].[Rule] set Name = @Name, RoleId = @roleId, SecurityType = @securityType, AssetTypeId = @assetTypeId, [UpdatedBy] = @u, [UpdatedOn] = @dt where Id = @ruleId; ",
							new { roleId, model.Name, ruleId, securityType = (int)securityType, assetTypeId, u = CurrentUserId, dt }, 
							transaction: trans
						);

						connection.Execute("delete [security].RuleWhen where Id = @ruleId; ", new { ruleId }, transaction: trans);
						rawWhens.ForEach(w => {
							w.Id = ruleId;
							connection.Execute(
								"insert into [security].RuleWhen (Id, [Position], CheckType, FieldTypeId, IntersectTypeId, [Operator], [Value], AssetId) " +
								"values (@Id, @Position, @CheckType, @FieldTypeId, @IntersectTypeId, @Operator, @Value, @AssetId)",
								new { w.Id, w.Position, w.CheckType, w.FieldTypeId, w.IntersectTypeId, Operator = (int)w.Operator, w.Value, w.AssetId },
								transaction: trans);
						});

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

		public async Task<RepositoryResponse<ReadSecurityPolicyOverride>> UpdatePolicyOverrideAsync(Guid uid, CreateSecurityPolicyOverride model)
		{
			RepositoryResponse<ReadSecurityPolicyOverride> response = null;

			if (uid == Guid.Empty)
			{
				return new(400, "Uid is invalid.");
			}

			if (model == null)
			{
				return new(400, "No valid data to create rule.");
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				d360.core.security.Rule rawRule = new()
				{
					ApplyToType = false,
					IsOverride = true,
					IsVisible = true,
					CreatedBy = CurrentUserId,
					CreatedOn = DateTime.UtcNow,
					Name = "",
					UpdatedBy = CurrentUserId,
					UpdatedOn = DateTime.UtcNow,
					Uid = Guid.NewGuid()
				};
				RuleWhen rawRuleWhen = new() { CheckType = 'S', Operator = Operator.Equals, Position = 1 };
				RuleThen rawRuleThen = new() { Operator = Operator.Equals, Position = 1 };

				// Data for validation.
				rawRule.SecurityType = model.SecurityType;
				var securityQuery = rawRule.SecurityType == RuleSecurityType.Group ?
					"select g.Id from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID and a.Uid = @SecurityUid;" :
					"select ResourceId from reporting.Global_Resource where Uid = @SecurityUid;";
				var qryData = await connection.QueryMultipleAsync(
					"select Id from [security].[Rule] where IsOverride = 1 and Uid = @uid; " +
					"select AssetTypeId from Asset where Uid = @AssetUid; " +
					"select Id from [security].[Role] where Uid = @RoleUid; " +
					"select Id from Asset where Uid = @AssetUid; " +
					securityQuery,
					new { uid, model.RoleUid, model.AssetUid, model.SecurityUid }
				);
				rawRule.Id = await qryData.ReadFirstAsync<int>();
				rawRule.Uid = uid;
				rawRule.AssetTypeId = await qryData.ReadFirstAsync<int>();
				rawRule.RoleId = await qryData.ReadFirstAsync<int>();
				rawRuleWhen.AssetId = await qryData.ReadFirstAsync<long>();
				rawRuleThen.SecurityId = await qryData.ReadFirstAsync<int>();

				if (rawRule.Id == 0)
				{
					response = new(404, "Could not find assignment based on Uid provided.");
				}

				if (rawRule.AssetTypeId == 0)
				{
					response = new(404, "Could not find asset type based on AssetTypeUid provided.");
				}

				if (response == null && rawRule.RoleId == 0)
				{
					response = new(404, "Could not find role based on RoleUid provided.");
				}

				if (response == null && rawRuleWhen.AssetId == 0)
				{
					response = new(404, "Could not find asset based on AssetUid provided.");
				}

				if (response == null && rawRuleThen.SecurityId == 0)
				{
					if (rawRule.SecurityType == RuleSecurityType.Group)
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
					using (var trans = connection.BeginTransaction())
					{
						connection.Execute(
							"update [security].[Rule] " +
							"set RoleId = @RoleId, SecurityType = @SecurityType, AssetTypeId = @AssetTypeId, UpdatedBy = @UpdatedBy, UpdatedOn = @UpdatedOn " +
							"where Id = @Id; ",
							rawRule, 
							trans);

						rawRuleWhen.Id = rawRule.Id;
						connection.Execute(
							"delete [security].RuleWhen where Id = @Id; " +
							"insert into [security].RuleWhen (Id, [Position], CheckType, [Operator], AssetId) values (@Id, @Position, @CheckType, @Operator, @AssetId)",
							new { rawRuleWhen.Id, rawRuleWhen.Position, rawRuleWhen.CheckType, Operator = (int)rawRuleWhen.Operator, rawRuleWhen.AssetId }, 
							trans);

						rawRuleThen.Id = rawRule.Id;
						connection.Execute(
							"delete [security].RuleThen where Id = @Id; " +
							"insert into [security].RuleThen (Id, [Position], [Operator], SecurityId) values (@Id, @Position, @Operator, @SecurityId)",
							new { rawRuleThen.Id, rawRuleThen.Position, Operator = (int)rawRuleThen.Operator, rawRuleThen.SecurityId }, 
							trans);

						trans.Commit();
					}

					response = new RepositoryResponse<ReadSecurityPolicyOverride>(
						new ReadSecurityPolicyOverride
						{
							AssetUid = model.AssetUid,
							RoleUid = model.RoleUid,
							SecurityType = model.SecurityType,
							SecurityUid = model.SecurityUid,
							Uid = rawRule.Uid
						},
						200, true, "Role assignment updated successfully.");
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
