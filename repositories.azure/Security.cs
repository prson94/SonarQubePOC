using d360.core.entities;
using d360.core.security;
using Dapper;
using DocumentFormat.OpenXml.EMMA;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Rule = d360.core.security.Rule;
using RuleThen = d360.core.security.RuleThen;
using RuleWhen = d360.core.security.RuleWhen;

namespace repositories.azure
{
	public class Security : Repository, ISecurity
	{
		public Security(DapperConnectionProvider provider) : base(provider) { }
		
		public async Task<RepositoryResponse<Rule>> CreatePolicyAsync(CreateRule model)
		{
			RepositoryResponse<Rule> response = null;

			if (model == null)
			{
				return new RepositoryResponse<Rule>(400, "No valid data to create rule.");
			}
			model.Name = (model.Name ?? "").Trim();
			if (string.IsNullOrEmpty(model.Name))
			{
				return new RepositoryResponse<Rule>(400, "Name must be populated.");
			}
			if (model.Name.Length < 3 || model.Name.Length > 250)
			{
				return new RepositoryResponse<Rule>(400, "Name must longer than three characters and less than 250 characters.");
			}
			if (!model.ApplyToType && (model.When == null || (model.When != null && model.When.Count == 0)))
			{
				return new RepositoryResponse<Rule>(400, "If rule does not apply to entire type, then you must apply asset filtering.");
			}
			if (model.Then == null || (model.Then != null && model.Then.Count == 0))
			{
				return new RepositoryResponse<Rule>(400, "You must apply user/group assignments.");
			}
			if (model.When != null && model.When.Any(w => !string.IsNullOrEmpty(w.FieldName) && w.IntersectTypeUid.HasValue))
			{
				return new RepositoryResponse<Rule>(400, "Each asset filter may only have a FieldName or an IntersectTypeUid populated, but not both.");
			}
			if (model.When != null && model.When.Any(w => w.IntersectTypeUid.HasValue && !w.AssetUid.HasValue))
			{
				return new RepositoryResponse<Rule>(400, "Each asset filter that has a populated IntersectTypeUid must also have a populated AssetUid.");
			}
			if (model.When != null && model.When.Any(w => !string.IsNullOrEmpty(w.FieldName) && (!w.AssetUid.HasValue && string.IsNullOrEmpty(w.Value))))
			{
				return new RepositoryResponse<Rule>(400, "Each asset filter that has a populated FieldName must also have either a populated AssetUid or a Value.");
			}

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
					"select * from [Group] g inner join Asset a on a.Object = 'Group' and a.ObjectID = g.ID and a.Uid in @SecurityUids; " +
					"select * from reporting.Global_Resource where Uid in @SecurityUids;",
					new { model.AssetTypeUid, model.RoleUid, IntersectTypeUids, AssetUids, SecurityUids }
				);
				var assetTypeId = await qryData.ReadFirstAsync<int>();
				var roleId = await qryData.ReadFirstAsync<int>();
				var assetTypeFields = await qryData.ReadAsync<d360.core.entities.FieldType>();
				var intersectTypes = await qryData.ReadAsync<d360.core.entities.IntersectType>();
				var groupFields = await qryData.ReadAsync<d360.core.entities.FieldType>();
				var userFields = await qryData.ReadAsync<d360.core.entities.FieldType>();
				var whenAssets = await qryData.ReadAsync<d360.core.entities.Asset>();
				var groups = await qryData.ReadAsync<d360.core.entities.Group>();
				var users = await qryData.ReadAsync<d360.core.entities.GlobalReportingResource>();

				var securityType = (model.SecurityType == RuleSecurityType.Group ? 'G' : 'U');

				if (assetTypeId == 0)
				{
					response = new RepositoryResponse<Rule>(404, "Could not find asset type based on AssetTypeUid provided.");
				}

				if (response == null && roleId == 0)
				{
					response = new RepositoryResponse<Rule>(404, "Could not find role based on RoleUid provided.");
				}
				
				var validFields = new List<string> { "Boolean", "Date", "DateTime", "Number", "Decimal", "Lookup", "Text" };

				var rawWhens = new List<RuleWhen>();
				if (response == null && model.When.Count > 0)
				{
					int position = 0;
					model.When.ForEach(w =>
					{
						position++;
						if (response == null) // Once we have an error, just stop.
						{ 
							var rawWhen = new RuleWhen { Operator = w.Operator, Position = position };

							if (string.IsNullOrEmpty(w.FieldName)) 
							{
								// Check intersect type.
								rawWhen.CheckType = 'R';

								var intersectType = intersectTypes.SingleOrDefault(i => i.uid == w.IntersectTypeUid);
								if (intersectType != null)
								{
									rawWhen.IntersectTypeId = intersectType.ID;

									var targetAssetTypeId = intersectType.SubjectAssetTypeID == assetTypeId ? intersectType.ObjectAssetTypeID : intersectType.SubjectAssetTypeID;
									var whenAsset = whenAssets.SingleOrDefault(a => a.AssetTypeID == targetAssetTypeId && a.uid == w.AssetUid);
									if (whenAsset != null)
									{
										rawWhen.AssetId = whenAsset.ID;
									}
									else
									{
										response = new RepositoryResponse<Rule>(404, "Could not find target asset in filter conditions based on AssetUid provided.");
									}
								}
								else
								{
									response = new RepositoryResponse<Rule>(404, "Could not find intersect type based on IntersectTypeUid provided.");
								}
							}
							else
							{
								// Check field.
								rawWhen.CheckType = 'F';

								var field = assetTypeFields.SingleOrDefault(f => f.Name == w.FieldName);
								if (field != null)
								{
									if (validFields.Contains(field.Type))
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
												response = new RepositoryResponse<Rule>(404, "Could not find target asset in filter conditions based on AssetUid provided.");
											}
										}
										else 
										{
											rawWhen.Value = w.Value;
										}
									}
									else
									{
										response = new RepositoryResponse<Rule>(409, "Selected field not supported in asset filters based on its type.");
									}
								}
								else
								{
									response = new RepositoryResponse<Rule>(404, "Could not find field based on FieldName provided.");
								}
							}						
						}
					});
				}

				var rawThens = new List<RuleThen>();
				if (response == null && model.Then.Count > 0)
				{	
					int position = 0;
					model.Then.ForEach(t =>
					{
						position++;
						if (response == null) // Once we have an error, just stop.
						{
							var rawThen = new RuleThen { Operator = t.Operator, Position = position };

							if (t.SecurityUid.HasValue)
							{
								// Direct security object assignment.

								if (securityType == 'G')
								{
									// Check groups
									var group = groups.SingleOrDefault(g => g.Uid == t.SecurityUid);
									if (group != null)
									{
										rawThen.SecurityId = group.ID;
									}
									else
									{
										response = new RepositoryResponse<Rule>(404, "Could not find group based on SecurityUid provided.");
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
										response = new RepositoryResponse<Rule>(404, "Could not find user based on SecurityUid provided.");
									}
								}
							}
							else
							{
								// Filter security object assign (non-direct)
								FieldType field = null;
								if (securityType == 'G')
								{
									field = groupFields.SingleOrDefault(f => f.Name == t.FieldName);
								}
								else
								{
									field = userFields.SingleOrDefault(f => f.Name == t.FieldName);
								}
								if (field != null)
								{
									if (validFields.Contains(field.Type))
									{
										rawThen.FieldTypeId = field.ID;
									}
									else
									{
										response = new RepositoryResponse<Rule>(409, "Selected field not supported in security object filters based on its type.");
									}
								}
								else
								{
									response = new RepositoryResponse<Rule>(404, "Could not find field based on FieldName provided.");
								}
							}
						}
					});
				}

				if (response == null)
				{
					await connection.OpenAsync();
					using (var trans = connection.BeginTransaction())
					{
						long ruleId = await connection.QuerySingleAsync<long>(
							"insert into [security].[Rule] (Uid, Name, RoleId, SecurityType, AssetTypeId, ApplyToType, IsVisible, IsOverride, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
							"values (@Uid, @Name, @roleId, @securityType, @assetTypeId, @ApplyToType, @IsVisible, @IsOverride, @u, @dt, @u, @dt); " +
							"select SCOPE_IDENTITY();", 
							new { Uid = Guid.NewGuid(), model.Name, roleId, securityType = (int)((securityType == 'G') ? RuleSecurityType.Group : RuleSecurityType.User), assetTypeId, model.ApplyToType, model.IsVisible, IsOverride = false, u = CurrentUserId, dt = DateTime.UtcNow }, 
							trans);

						rawWhens.ForEach(async w => {
							await connection.ExecuteAsync(
								"insert into [security].RuleWhen ([Position], CheckType, FieldTypeId, IntersectTypeId, [Operator], [Value], AssetId) " +
								"values (@Position, @CheckType, @FieldTypeId, @IntersectTypeId, @Operator, @Value, @AssetId)", 
								w, 
								trans);
						});

						rawThens.ForEach(async t => {
							await connection.ExecuteAsync(
								"insert into [security].RuleThen ([Position], FieldTypeId, [Operator], [Value], SecurityId) " +
								"values (@Position, @FieldTypeId, @Operator, @Value, @SecurityId)",
								t,
								trans);
						});

						trans.Commit();
					}
					response = new RepositoryResponse<Rule>(new Rule(), 201, true, "");
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadRuleOverride>> CreatePolicyOverrideAsync(CreateRuleOverride model)
		{
			RepositoryResponse<ReadRuleOverride> response = null;

			if (model == null)
			{
				return new(400, "No valid data to create rule.");
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				Rule rawRule = new() { 
					ApplyToType = false, IsOverride = true, IsVisible = true, 
					CreatedBy = CurrentUserId, CreatedOn = DateTime.UtcNow, Name = "", UpdatedBy = CurrentUserId, UpdatedOn = DateTime.UtcNow, 
					Uid = Guid.NewGuid() 
				};
				RuleWhen rawRuleWhen = new() { CheckType = 'S', Operator = "Eq", Position = 1 };
				RuleThen rawRuleThen = new() { Operator = "Eq", Position = 1 };

				// Data for validation.
				rawRule.SecurityType = (model.SecurityType == RuleSecurityType.Group ? 'G' : 'U');
				var securityQuery = rawRule.SecurityType == 'G' ?
					"select Id from [Group] where Uid = @SecurityUid;" :
					"select ResourceId from reporting.Global_Resource where Uid = @SecurityUid;";
				var qryData = await connection.QueryMultipleAsync(
					"select AssetTypeId from Asset where Uid = @AssetUid; " +
					"select Id from [security].[Role] where Uid = @RoleUid; " +
					"select Id from Asset where Uid = @AssetUid; " +
					securityQuery,
					new { model.RoleUid, model.AssetUid, model.SecurityUid }
				);
				rawRule.AssetTypeId = await qryData.ReadFirstAsync<int>();
				rawRule.RoleId = await qryData.ReadFirstAsync<int>();
				rawRuleWhen.AssetId = await qryData.ReadFirstAsync<long>();
				rawRuleThen.SecurityId = await qryData.ReadFirstAsync<int>();

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
					if (rawRule.SecurityType == 'G')
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
					using (var trans = connection.BeginTransaction())
					{
						long ruleId = await connection.QuerySingleAsync(
						"insert into [security].[Rule] (Uid, Name, RoleId, SecurityType, AssetTypeId, ApplyToType, IsVisible, IsOverride, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) " +
						"values (@Uid, @Name, @RoleId, @SecurityType, @AssetTypeId, @ApplyToType, @IsVisible, @IsOverride, @CreatedBy, @CreatedOn, @UpdatedBy, @UpdatedOn); " +
						"select SCOPE_IDENTITY();",
						rawRule, trans);

						await connection.ExecuteAsync(
							"insert into [security].RuleWhen ([Position], CheckType, [Operator], AssetId) values (@Position, @CheckType, @Operator, @AssetId)",  
							rawRuleWhen, trans);

						await connection.ExecuteAsync(
							"insert into [security].RuleThen ([Position], [Operator], SecurityId) values (@Position, @Operator, @SecurityId)", 
							rawRuleThen, trans);

						trans.Commit();
					}
					
					response = new RepositoryResponse<ReadRuleOverride>(
						new ReadRuleOverride { 
							AssetUid = model.AssetUid, 
							RoleUid = model.RoleUid, 
							SecurityType = model.SecurityType, 
							SecurityUid = model.SecurityUid, 
							Uid = rawRule.Uid 
						}, 
						201, true);
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

			if (model.Name.Length < 250)
			{
				return new(400, "Name property must be less than 250 characters.");
			}
			if (model.Description.Length < 4000)
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

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var existingCount = await connection.QueryFirstAsync<int>(
					"select count(1) from [security].[Role] where Name = @Name", new { model.Name }
				);

				if (existingCount > 0)
				{
					return new(400, "A role with the same name already exists.");
				}

				response = new(new(), 201, true, "");

				var role = await connection.QuerySingleAsync<ReadRole>(
						$@"
declare @roleId int;
insert into [security].[Role] ([Uid], [Name], Description, [CreatedBy], [CreatedOn], [UpdatedBy], [UpdatedOn])
values (@Uid, @Name, @Description, @u, @dt, @u, @dt);
select @roleId = SCOPE_IDENTITY();
select * from [security].[Role] where Id = @roleId;",
				new { Uid = Guid.NewGuid(), model.Name, model.Description, u = CurrentUserId, dt = DateTime.UtcNow });

				response.Data = role;
			}

			return response;
		}
		
		public async Task<RepositoryResponse<IEnumerable<ReadRule>>> ReadPoliciesAsync()
		{
			RepositoryResponse<IEnumerable<ReadRule>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response.Data = await connection.QueryAsync<ReadRule>(@"
select	ru.Uid,
		ru.Name, 
		t.Uid as AssetTypeUid,  
		t.Name as AssetTypeName,
		ro.Uid as RoleUid,
		iif(ru.SecurityType = 'G', 1, 2) as SecurityType,
		ru.ApplyToType,
		ru.IsVisible
from	security.[Rule] ru 
		inner join [security].[Role] ro on ro.Id = ru.RoleId and ru.IsOverride = 0
		inner join AssetType t on t.Id = ru.AssetTypeId 
order by	ru.Name");
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRolesAsync()
		{
			RepositoryResponse<IEnumerable<ReadRole>> response = new(200);

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				response.Data = await connection.QueryAsync<ReadRole>(
					"select uid, Name, Description, UpdatedOn from security.[Role] order by Name"
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

				response = new(true, 200, true, "");

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
					"declare @id int; " +
					"select @id = Id from [security].[Rule] where IsOverride = 1 and Uid = @uid;", new { uid }
					);

				if (ruleId == 0)
				{
					return new(404, "No matching rule found based on uid.");
				}

				response = new(true, 200, true, "");

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on on r.Id = o.Id and r.Id = @ruleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on on r.Id = o.Id and r.Id = @ruleId; " +
					"delete [security].[Rule] where RoleId = @ruleId; ",
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
					"declare @id int; " +
					"select @id = Id from [security].[Role] where Uid = @uid;", new { uid }
					);

				if (roleId == 0)
				{
					return new(404, "No matching role found based on uid.");
				}

				response = new(true, 200, true, "");

				await connection.ExecuteAsync(
					"delete o from [security].RuleWhen o inner join [security].[Rule] r on on r.Id = o.Id and r.RoleId = @roleId; " +
					"delete o from [security].RuleThen o inner join [security].[Rule] r on on r.Id = o.Id and r.RoleId = @roleId; " + 
					"delete [security].[Rule] where RoleId = @roleId; " +
					"delete [security].[Role] where Id = @roleId; ",
					new { roleId }
				);
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadRule>> UpdatePolicyAsync(Guid uid, ReadRule model)
		{
			RepositoryResponse<ReadRule> response;

			model.Name = (model.Name ?? "").Trim();
			if (model.Name.Length < 250)
			{
				return new(400, "Name property must be less than 250 characters.");
			}
			if (string.IsNullOrEmpty(model.Name))
			{
				return new(400, "Name must be populated.");
			}
			if (model.Name.Length < 3)
			{
				return new(400, "Name must longer than three characters.");
			}

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				var query = await connection.QueryMultipleAsync(
					"declare @id int, @roleId int, @assetTypeId int; " +
					"select @id = Id from [security].[Rule] where Uid = @uid; " +
					"select @roleId = Id from [security].[Role] where Uid = @RoleUid; " +
					"select @assetTypeId = Id from AssetType where Uid = @AssetTypeUid; " +
					"select @id; select @roleId; select @assetTypeId; " +
					"select count(1) from [security].[Rule] where Id <> @id and RoleId = @roleId and Name = @Name; ", new { uid, model.RoleUid, model.AssetTypeUid, model.Name }
					);
				int ruleId = await query.ReadSingleAsync<int>();
				int roleId = await query.ReadSingleAsync<int>();
				int assetTypeId = await query.ReadSingleAsync<int>();
				int matchingAlternateRuleCount = await query.ReadSingleAsync<int>();

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

				response = new(new(), 200, true, "");

				var dt = DateTime.UtcNow;
				await connection.ExecuteAsync(
					"update [security].[Rule] set Name = @Name, RoleId = @roleId, AssetTypeId = @assetTypeId, [UpdatedBy] = @u, [UpdatedOn] = @dt where Id = @ruleId; ",
					new { roleId, model.Name, ruleId, assetTypeId, u = CurrentUserId, dt }
				);

				response.Data = model;
			}

			return response;
		}

		public async Task<RepositoryResponse<ReadRuleOverride>> UpdatePolicyOverrideAsync(Guid uid, CreateRuleOverride model)
		{
			RepositoryResponse<ReadRuleOverride> response = null;

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
				Rule rawRule = new()
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
				RuleWhen rawRuleWhen = new() { CheckType = 'S', Operator = "Eq", Position = 1 };
				RuleThen rawRuleThen = new() { Operator = "Eq", Position = 1 };

				// Data for validation.
				rawRule.SecurityType = (model.SecurityType == RuleSecurityType.Group ? 'G' : 'U');
				var securityQuery = rawRule.SecurityType == 'G' ?
					"select Id from [Group] where Uid = @SecurityUid;" :
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
					if (rawRule.SecurityType == 'G')
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
					using (var trans = connection.BeginTransaction())
					{
						await connection.QuerySingleAsync(
						"update [security].[Rule] " +
						"set RoleId = @RoleId, SecurityType = @SecurityType, AssetTypeId = @AssetTypeId, UpdatedBy = @UpdatedBy, UpdatedOn = @UpdatedOn " +
						"where Id = @Id; ",
						rawRule, trans);

						rawRuleWhen.Id = rawRule.Id;
						await connection.ExecuteAsync(
							"delete [security].RuleWhen where Id = @Id; " +
							"insert into [security].RuleWhen (Id, [Position], CheckType, [Operator], AssetId) values (@Id, @Position, @CheckType, @Operator, @AssetId)",
							rawRuleWhen, trans);

						rawRuleThen.Id = rawRule.Id;
						await connection.ExecuteAsync(
							"delete [security].RuleThen where Id = @Id; " +
							"insert into [security].RuleThen (Id, [Position], [Operator], SecurityId) values (@Id, @Position, @Operator, @SecurityId)",
							rawRuleThen, trans);

						trans.Commit();
					}

					response = new RepositoryResponse<ReadRuleOverride>(
						new ReadRuleOverride
						{
							AssetUid = model.AssetUid,
							RoleUid = model.RoleUid,
							SecurityType = model.SecurityType,
							SecurityUid = model.SecurityUid,
							Uid = rawRule.Uid
						},
						200, true);
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

			if (model.Name.Length < 250)
			{
				return new(400, "Name property must be less than 250 characters.");
			}
			if (model.Description.Length < 4000)
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

				response = new(new(), 200, true, "");

				var dt = DateTime.UtcNow;
				await connection.ExecuteAsync(
					"update [security].[Role] set Name = @Name, Description = @Description, [UpdatedBy] = @u, [UpdatedOn] = @dt where Id = @roleId;",
					new { roleId, model.Name, model.Description, u = CurrentUserId, dt }
				);

				response.Data = new() { Description = model.Description, Name = model.Name, Uid = uid, UpdatedOn = dt };
			}

			return response;
		}
	}
}
