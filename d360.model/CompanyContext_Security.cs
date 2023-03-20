using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.queue;

using Dapper;

namespace d360.model
{
	public partial class CompanyContext : BaseContext
	{
		#region DbSets

		public DbSet<Group> Groups { get; set; }

		public DbSet<ResourceGroup> ResourceGroups { get; set; }

		public DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

		public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

		public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

		public DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

		public DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

		public DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

		public DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

		#endregion

		#region Engine Methods

		public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAsset(long id)
		{
			try
			{
				return Database.Connection.Query<ResponsibilityType>(@"
																	select	RT.*
																	from	ResponsibilityType RT
																			inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = RT.ID
																			inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID
																			inner join Asset A on A.AssetTypeID = T.ID and A.ID = @id
																	order by RT.Name", new { id }).AsQueryable();
			}
			catch (SqlException ex)
			{
				throw CheckAndTranslateSqlException(ex, "Responsibility Type");
			}
		}

		public string GetNoReadSqlStatement(string identifier = null)
		{
			return GetNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetAssetTypeNoReadSqlStatement(string identifier = null)
		{
			return GetAssetTypeNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetID from ResponsibilityDetail where ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? CurrentResourceID.ToString() : identifier)}";
		}

		public string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetTypeID from ResponsibilityDetail where AssetID = 0 and ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? CurrentResourceID.ToString() : identifier)}";
		}

		public List<PermissionInfo> GetTypePermissions(string type, int typeID)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

			List<int> responsibilityAssignments = Filter<ResponsibilityDetail>(i =>
				i.Type == type && i.TypeID == typeID &&
				i.AssetID == 0 &&
				i.ResourceID == CurrentResourceID
			).Select(i => i.PermissionsBitMask).Distinct().ToList();

			permissions.ForEach(p =>
			{
				p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
			});

			//AddAsset no longer requires ApplyToType (GOV-13993). But because the ResponsibilityDetail view relies on the ...RuleResults tables,
			//we will need to check for this permission with HasAssetTypePermission() if it has not already been selected
			permissions
				.Where(p => p.Value == (int)Permission.AddAsset && !p.Selected)
				.ToList()
				.ForEach(p => p.Selected = HasAssetTypePermission(type, typeID, Permission.AddAsset)
			);

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		public List<PermissionInfo> GetPermissions(long assetId, int assetTypeId)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

			IEnumerable<int> responsibilityAssignments = Query<int>(@"select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = 0
														union select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = @assetId", new { r = CurrentResourceID, assetTypeId, assetId });

			if (responsibilityAssignments.Any())
			{
				permissions.ForEach(p =>
				{
					p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
				});
			}

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		/// <summary>
		/// Default to read unless the user explicitly has no read access to an asset.
		/// </summary>
		private bool HasAssetDefaultReadPermission(string type, int id)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();

				if (assetTypeID <= 0)
				{
					return true; // objects not in asset table we grant permission               
				}

				hasPermission = HasReadPermission(type, id, assetTypeID);
			}

			return hasPermission;
		}

		public bool HasAssetPermission(string type, int id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{

				if (permission == Permission.ReadAsset)
				{
					hasPermission = HasAssetDefaultReadPermission(type, id);
				}
				else
				{
					int? assetTypeID = null;

					if (type.EndsWith("Type"))
					{
						assetTypeID = Query<int?>("select ID from AssetType where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
					}
					else
					{
						assetTypeID = Query<int?>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
					}

					if (assetTypeID.HasValue)
					{
						hasPermission = HasPermission(type, id, assetTypeID.Value, permission);
					}
				}
			}

			return hasPermission;
		}

		private bool HasPermission(string type, int objectId, int assetTypeId, Permission permission)
		{
			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
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
																						end", new { type, id = objectId, t = assetTypeId, r = CurrentResourceID });
		}

		/// <summary>
		/// Used to determine if a user has read permissions on a given asset type.  Read is assumed to be present unless denied.
		/// </summary>        
		/// <param name="assetTypeId"></param>        
		/// <returns></returns>
		private bool HasAssetTypeReadPermission(int assetTypeId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = 0 and ua.AssetTypeID = @t and ua.AssetID = 0)
																						begin
																							select 0;
																						end				                                                                        
																						else
																						begin
																							select 1;
																						end", new { t = assetTypeId, r = CurrentResourceID });
		}

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <returns></returns>
		private bool HasReadPermission(string type, int objectId, int assetTypeId)
		{
			return HasUserReadPermission(type, objectId, assetTypeId, CurrentResourceID);
		}

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <param name="permission"></param>
		/// <param name="permission"></param>
		/// <returns></returns>
		public bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = 0 and ua.AssetTypeID = @t and ua.AssetID is not null)
																						begin
																							select 0;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua inner join asset a on(ua.AssetID = a.id and a.Object = @type and a.ObjectID = @id) where ua.PermissionsBitMask & {(int)permission} = 0)
																						begin
																							select 0;
																							end
																						else
																						begin
																							select 1;
																						end", new { type, id = objectId, t = assetTypeId, r = resourceId });
		}

		private bool HasPermission(long assetId, int assetTypeId, Permission permission)
		{
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);

			if (isReadPermission)
			{
				Asset asset = Assets.Single(a => a.ID == assetId);

				if (permission == Permission.ReadAsset)
				{
					return HasUserReadPermission(asset.Object, asset.ObjectID, assetTypeId, CurrentResourceID);
				}

				return HasPermission(asset.Object, asset.ObjectID, assetTypeId, permission);
			}
			else
			{
				return Database.Connection.QuerySingle<bool>($@"if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t and ua.AssetId = 0)
																						begin
																							select 1;
																							end
																						else if exists(select 1 from UserAssetPermissions(@r, @t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetID = @assetId)
																						begin
																							select 1;
																							end
																						else
																						begin
																							select 0;
																						end", new { assetId, t = assetTypeId, r = CurrentResourceID });
			}
		}

		public bool HasAssetPermission(long id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where ID = @id", new { id }).Single();
				hasPermission = HasPermission(id, assetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermissionByUid(Guid uid, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;

			if (!hasPermission)
			{
				Asset asset = Assets.Single(a => a.uid == uid);
				hasPermission = HasPermission(asset.ID, asset.AssetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetPermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(string type, int id, Permission permission)
		{
			bool hasPermission = CurrentResourceIsAdmin;
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);


			if (!hasPermission)
			{
				if (isReadPermission)
				{
					hasPermission = HasAssetTypeReadPermission(id);
				}
				else
				{
					int assetTypeID = Query<int>("select ID from AssetType where [Object] = @type and [ObjectID] = @id", new { id, type }).Single();
					hasPermission = Database.Connection.QuerySingle<bool>($@"if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																						begin
																							select 1;
																						end				                                                                        
																						else
																						begin
																							select 0;
																						end", new { t = assetTypeID, r = CurrentResourceID });
				}
			}

			return hasPermission;
		}

		public bool HasAssetTypePermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetTypePermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(int assetTypeId, Permission permission)
		{
			AssetType assetType = Query<AssetType>("select * from AssetType where ID = @id", new { id = assetTypeId }).Single();

			return HasAssetTypePermission(assetType.Object, assetTypeId, permission);
		}

		public void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation)
		{
			List<AssetMeasureModel> structuredMeasures = null;

			try
			{
				AssetType assetType = Filter<AssetType>(a => a.Object == relation.ObjectType && a.ObjectID == relation.ObjectID).SingleOrDefault();
				ResponsibilityType responsibility = Filter<ResponsibilityType>(a => a.ID == relation.ResponsibilityTypeID).SingleOrDefault();

				if (assetType != null && responsibility != null)
				{
					// Scoring - get asset measures that are impacted
					structuredMeasures = GetMeasureModelsBasedOnResponsibilityAllocation(assetType, responsibility);
				}
			}
			catch
			{
				throw;
			}
			using (DbContextTransaction trans = Database.BeginTransaction())
			{
				try
				{
					Database.ExecuteSqlCommand(@"
												delete	O 
												from	ResponsibilityTypeRelationOverrideItem O
														inner join Asset A on A.ID = O.AssetID and O.ResponsibilityTypeID = @ResponsibilityTypeID
														inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @ObjectType and T.ObjectID = @ObjectID;

												delete	O 
												from	[dbo].[ResponsibilityRuleResultSecurityAsset] O
														inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID		    

												delete	O 
												from	[dbo].[ResponsibilityRuleResultAsset] O
														inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID

												delete	ResponsibilityTypeRelationRule
												where	ResponsibilityTypeID = @ResponsibilityTypeID
														and Object = @ObjectType 
														and ObjectID = @ObjectID;

												delete	ResponsibilityTypeRelation
												where	ResponsibilityTypeID = @ResponsibilityTypeID
														and ObjectType = @ObjectType 
														and ObjectID = @ObjectID;",
														new SqlParameter("@ResponsibilityTypeID", relation.ResponsibilityTypeID),
														new SqlParameter("@ObjectType", relation.ObjectType),
														new SqlParameter("@ObjectID", relation.ObjectID)
														);
					trans.Commit();
				}
				catch
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
					}

					throw;
				}
			}

			// If you made it this far, then send to scoring engine.
			if (structuredMeasures != null)
			{
				CreateMeasureChangedResultExecution(structuredMeasures);
			}
		}

		#endregion

		#region Responsibility Rule Generation

		/// <summary>
		/// Re-process responsibility rules. By default this will re-process ALL rules unless passing a specific rule ID.
		/// </summary>
		/// <param name="ruleID">Optionally pass a specific rule by its ID.</param>
		public async Task ProcessResponsibilityRelationRules(int? ruleID = null, int timeout = 7200)
		{
			List<ResponsibilityAssetMeasureProcessedResult> results = new List<ResponsibilityAssetMeasureProcessedResult>();

			if (Connection.State != System.Data.ConnectionState.Open)
			{
				Connection.Open();
			}

			IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesForRerun(ruleID).ConfigureAwait(false);

			List<int> rulesRequiringRun = new List<int>();

			string ruleExceptionMessages = "";

			foreach (ResponsibilityTypeRelationRule rule in rules)
			{
				try
				{
					rule.SetDefinitionFromRaw();

					if (rule.ApplyToType)
					{
						await ProcessRuleForAssetType(rule, results).ConfigureAwait(false);
					}
					else
					{
						await ProcessRuleForAsset(rule, results).ConfigureAwait(false);
					}
				}
				catch (ApplicationException ex)
				{
					ruleExceptionMessages += ex.Message;
				}
			}

			// Send measure results to score engine.
			DateTime today = DateTime.UtcNow.Date;
			List<AssetMeasureModel> structuredMeasures = results.GroupBy(m => new { m.AssetUid })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = today,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).Distinct().ToList()
				}).ToList();

			CreateMeasureChangedResultExecution(structuredMeasures);

			if (!string.IsNullOrEmpty(ruleExceptionMessages))
			{
				throw new ApplicationException(ruleExceptionMessages);
			}
		}


		public async Task ProcessRulesForExecution(Guid executionId, int beginItemNumber, int endItemNumber)
		{
			List<ResponsibilityAssetMeasureProcessedResult> results = new List<ResponsibilityAssetMeasureProcessedResult>();

			if (Connection.State != System.Data.ConnectionState.Open)
			{
				Connection.Open();
			}

			IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesToRun(executionId, beginItemNumber, endItemNumber);

			List<int> rulesRequiringRun = new List<int>();

			string ruleExceptionMessages = "";

			foreach (ResponsibilityTypeRelationRule rule in rules)
			{
				try
				{
					if (await ShouldRuleRun(rule.ID))
					{
						rulesRequiringRun.Add(rule.ID);
						rule.SetDefinitionFromRaw();

						if (rule.ApplyToType)
						{
							await ProcessRuleForAssetType(rule, results);
						}
						else
						{
							await ProcessRuleForAsset(rule, results);
						}
					}
				}
				catch (ApplicationException ex)
				{
					ruleExceptionMessages += ex.Message;
				}
			}

			// Send measure results to score engine.
			DateTime today = DateTime.UtcNow.Date;
			List<AssetMeasureModel> structuredMeasures = results.GroupBy(m => new { m.AssetUid })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = today,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).Distinct().ToList()
				}).ToList();

			CreateMeasureChangedResultExecution(structuredMeasures);

			if (!string.IsNullOrEmpty(ruleExceptionMessages))
			{
				throw new ApplicationException(ruleExceptionMessages);
			}

		}

		#region Helper Methods

		/// <summary>
		/// Set a rule as having already been processed with the current date / time
		/// </summary>
		/// <param name="cnn"></param>
		/// <param name="ruleId"></param>
		/// <returns></returns>
		private async Task MarkResponsibilityRuleAsRan(int ruleId, SqlTransaction transaction)
		{
			await Connection.ExecuteAsync("update ResponsibilityTypeRelationRule set LastRunOn = @date where ID = @id", new { date = DateTime.UtcNow, id = ruleId }, transaction: transaction);
		}

		private async Task ProcessRuleForAsset(ResponsibilityTypeRelationRule rule, List<ResponsibilityAssetMeasureProcessedResult> results)
		{
			string sqlToExecute = "";

			using (SqlTransaction transaction = Connection.BeginTransaction())
			{
				try
				{
					string thenSql = GetThenResultsSql(rule, false, transaction, false, "", false);
					var whenQueryData = await GetWhenResultsSql(rule, transaction, false, false).ConfigureAwait(false);

					thenSql = string.Format(thenSql, "");

					//create impacted assets temporary table.
					sqlToExecute = "create table #changes (ActionType varchar(50), RuleID int, AssetID bigint)";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//merge into the asset table 
					sqlToExecute = $@"
							{whenQueryData.TempTableQuery}

							merge [dbo].[ResponsibilityRuleResultAsset] as T
									using	(
												{whenQueryData.SqlQuery}
											) as S
									on		@ruleId = T.RuleID and S.AssetID = T.AssetID
									when	matched then
											update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
									when	not matched by target then
											insert (RuleID, AssetID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetID,getutcdate(),0)
									when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
											delete
									output  $action as ActionType, 
											iif($action = 'DELETE', deleted.RuleID, inserted.RuleID), 
											iif($action = 'DELETE', deleted.AssetID, inserted.AssetID)
									into #changes;";
					whenQueryData.DbParameters.Add("ruleId", rule.ID);
					await Connection.ExecuteAsync(sqlToExecute, whenQueryData.DbParameters, transaction: transaction, commandTimeout: ApiTimeout);

					//merge into the resource table
					if (thenSql != null && thenSql.Length > 0)
					{
						sqlToExecute = $@"
							merge [dbo].[ResponsibilityRuleResultSecurityAsset] as T
									using	(
												{thenSql}
											) as S
									on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
									when	matched then
											update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
									when	not matched by target then
											insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
									when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
											delete;";
						await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID, appliesToType = rule.ApplyToType }, transaction: transaction);
					}

					sqlToExecute = @"
									select  A.Uid as AssetUid, 
											M.AllocationUid,
											M.Uid as MetricAssetUid,
											V.Uid as MetricAssetVersionUid
									from    #changes C 
										inner join Asset A on A.ID = C.AssetID and C.ActionType in ('DELETE', 'INSERT') 
										inner join AssetType T on T.ID = A.AssetTypeID
										inner join ResponsibilityTypeRelationRule R on R.ID = C.RuleID 
										inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
										inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
										inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
										inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
										and ( 
											(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
											(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
											) 
										and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
										and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
										and V.Definition <> '{}'";
					IEnumerable<ResponsibilityAssetMeasureProcessedResult> ruleResults =
						await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(sqlToExecute, new { today = DateTime.UtcNow.Date }, transaction: transaction);

					results.AddRange(ruleResults);

					//drop impacted assets temporary table.
					sqlToExecute = "drop table if exists #changes";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//First time a rule runs, queue the asset type for search re-indexing
					if (rule.LastRunOn == null)
					{
						sqlToExecute = "select * from [dbo].[AssetType] at WHERE at.[Object] = @Object and at.ObjectID = @ObjectID";
						AssetType assetType = Connection.Query<AssetType>(
							sqlToExecute,
							new { rule.Object, rule.ObjectID },
							transaction: transaction
						).SingleOrDefault();

						Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
						{
							CompanyID = CurrentCompanyID,
							AssetTypeUid = assetType.uid,
							Origin = "ProcessRuleForAsset, rule: " + rule.ID.ToString() + ", " + rule.Name
						});
					}

					await MarkResponsibilityRuleAsRan(rule.ID, transaction);

					transaction.Commit();
				}
				catch (Exception ex)
				{
					try
					{
						if (transaction != null)
						{
							transaction.Rollback();
						}
					}
					catch
					{
						//possible invalid rule ignore
					}

					throw new ApplicationException($"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n");
				}
			}
		}

		private async Task ProcessRuleForAssetType(ResponsibilityTypeRelationRule rule, List<ResponsibilityAssetMeasureProcessedResult> results)
		{
			string sqlToExecute = "";

			using (SqlTransaction transaction = Connection.BeginTransaction())
			{
				try
				{
					string thenSql = GetThenResultsSql(rule, false, transaction, false);
					thenSql = string.Format(thenSql, "");

					//create impacted assets temporary table.
					sqlToExecute = "create table #changes (ActionType varchar(50), RuleID int, AssetTypeID int)";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//merge into the asset table 
					sqlToExecute = @"
							merge   [dbo].[ResponsibilityRuleResultAsset] as T
							using	(
									select	T.ID as AssetTypeID,		
											R.ID as RuleID
									from	AssetType T
											inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID							                
										where 
												R.ID = @ruleId
									) as S
							on		S.RuleID = T.RuleID and S.AssetTypeID = T.AssetTypeID
							when	matched then
									update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
							when	not matched by target then
									insert (RuleID, AssetTypeID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetTypeID,getutcdate(),0)
							when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
									delete
							output  $action as ActionType, 
									iif($action = 'DELETE', deleted.RuleID, inserted.RuleID), 
									iif($action = 'DELETE', deleted.AssetTypeID, inserted.AssetTypeID)
							into #changes;";
					await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID }, transaction: transaction);

					//merge into the resource table
					sqlToExecute = $@"
							merge   [dbo].[ResponsibilityRuleResultSecurityAsset] as T
							using	(
									{thenSql}
									) as S
							on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
							when	matched then
									update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
							when	not matched by target then
									insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
							when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
									delete;";
					await Connection.ExecuteAsync(sqlToExecute, new { ruleId = rule.ID }, transaction: transaction);

					sqlToExecute = @"
							select  A.Uid as AssetUid, 
									M.AllocationUid,
									M.Uid as MetricAssetUid,
									V.Uid as MetricAssetVersionUid
							from    #changes C 
									inner join AssetType T on T.ID = C.AssetTypeID 
									inner join Asset A on A.AssetTypeID = T.ID and C.ActionType in ('DELETE', 'INSERT') 
									inner join ResponsibilityTypeRelationRule R on R.ID = C.RuleID 
									inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
									inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
									inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
									inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
										and ( 
											(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
											(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
											) 
										and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
										and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
										and V.Definition <> '{}'";
					IEnumerable<ResponsibilityAssetMeasureProcessedResult> ruleResults =
						await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(sqlToExecute, new { today = DateTime.UtcNow.Date }, transaction: transaction);

					results.AddRange(ruleResults);

					//drop impacted assets temporary table.
					sqlToExecute = "drop table if exists #changes";
					await Connection.ExecuteAsync(sqlToExecute, transaction: transaction);

					//First time a rule runs, queue the asset type for search re-indexing
					if (rule.LastRunOn == null)
					{
						sqlToExecute = "select * from [dbo].[AssetType] at WHERE at.[Object] = @Object and at.ObjectID = @ObjectID";
						AssetType assetType = Connection.Query<AssetType>(
							sqlToExecute,
							new { rule.Object, rule.ObjectID },
							transaction: transaction
						).SingleOrDefault();
						Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
						{
							CompanyID = CurrentCompanyID,
							AssetTypeUid = assetType.uid,
							Origin = "ProcessRuleForAssetType, rule: " + rule.ID.ToString() + ", " + rule.Name
						});
					}

					await MarkResponsibilityRuleAsRan(rule.ID, transaction);

					transaction.Commit();
				}
				catch (Exception ex)
				{
					try
					{
						if (transaction != null)
						{
							transaction.Rollback();
						}
					}
					catch
					{
						// ignore invalid rules
					}

					throw new ApplicationException($"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n");
				}
			}
		}

		/// <summary>
		/// Does the current rule id need to be run?
		/// </summary>
		/// <param name="cnn">DB connection</param>
		/// <param name="ruleId">RUle ID to look at</param>
		/// <returns></returns>
		private async Task<bool> ShouldRuleRun(int ruleId)
		{
			return await Connection.QueryFirstAsync<bool>("exec ResponsibilityRuleShouldRun @id", new { id = ruleId });
		}

		/// <summary>
		/// Return all responsibility rules that should be rerun
		/// </summary>
		/// <param name="cnn">DB connection</param>
		/// <returns></returns>
		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesForRerun(int? ruleId)
		{
			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>("exec GetRulesForRerun @id", new { id = ruleId });
		}

		/// <summary>
		/// Load the Responsibility Rules that the rebuild process should run
		/// </summary>
		/// <param name="ruleID">Specific responsibilty rule id to go after</param>
		/// <returns></returns>
		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(int? ruleID)
		{
			if (ruleID.HasValue)
			{
				return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value });
			}

			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule");
		}

		private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(Guid executionId, int beginItemNumber, int endItemNumber)
		{
			return await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"
					select	R.* 
					from	ResponsibilityTypeRelationRule R 
							inner join api.executionresponsibilityrule E on E.uid = R.uid 
								and E.Success != 0
								and E.ItemNumber between @beginItemNumber and @endItemNumber
								and E.ExecutionID = @executionId"
			, new { executionId, beginItemNumber, endItemNumber });
		}

		public async Task<ResponsibilityWhenQueryData> GetWhenResultsSql(ResponsibilityTypeRelationRule rule, SqlTransaction transaction, bool includeName = true, bool includeUid = true)
		{
			var whenSql = new StringBuilder();
			var whenWhereConditions = new List<string>();
			var whenTempTables = new StringBuilder();
			Dictionary<string, object> dbArgs = new Dictionary<string, object>();

			whenSql.Append("select distinct A.ID as AssetID ");
			if (includeName)
			{
				whenSql.Append(", ADV.DisplayValue as Name ");
			}

			if (includeUid)
			{
				whenSql.Append(", A.uid, P.DisplayPath as Path ");
			}

			whenSql.Append($@"
from	Asset A 
		inner join AssetPath P on P.ID = A.ID
		inner join AssetDisplayValue ADV on ADV.AssetID = A.ID
		inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID} ");

			int fCount = 1;
			int rCount = 1;


			if (rule.StructuredDefinition != null && rule.StructuredDefinition.When != null)
			{
				foreach (ResponsibilityRuleDefinitionWhen w in rule.StructuredDefinition.When)
				{
					if (w.CheckType == "F")
					{
						if (w.FieldTypeID > 0)
						{
							string fieldWhere = "";
							var whenFieldType = Connection.QueryFirstOrDefaultAsync<FieldType>("select * from FieldType where ID = @FieldTypeID", new { w.FieldTypeID }, transaction: transaction).Result;

							if (whenFieldType != null)
							{
								string dbParameterName = $"@filter_{fCount}";
								string value = w.Value;
								if (whenFieldType.AllowMultipleValues)// multiselect list
								{
									fieldWhere =
										$" where FT.ID = {w.FieldTypeID} and {dbParameterName} in (select value from string_split(coalesce(F.Value, FT.DefaultValue),','))";
								}
								else if (whenFieldType.Type == "Text")
								{
									switch (w.Operator)
									{
										case Operator.NotEquals:
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) != {dbParameterName}";
											break;
										case Operator.Contains:
											value = $"%{w.Value.Trim()}%";
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) LIKE {dbParameterName}";
											break;
										case Operator.NotContains:
											value = $"%{w.Value.Trim()}%";
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) NOT LIKE {dbParameterName}";
											break;
										case Operator.StartsWith:
											value = $"{w.Value}%";
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) LIKE {dbParameterName}";
											break;
										case Operator.EndsWith:
											value = $"%{w.Value}";
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) LIKE {dbParameterName}";
											break;
										case Operator.Populated:
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and (coalesce(F.Value, F.FormattedValue, FT.DefaultValue) is not null or LEN(coalesce(F.Value, F.FormattedValue, FT.DefaultValue))>0)";  // all field types plus single select list
											break;
										case Operator.NotPopulated:
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and (coalesce(F.Value, F.FormattedValue, FT.DefaultValue) is null or LEN(coalesce(F.Value, F.FormattedValue, FT.DefaultValue))=0)";  // all field types plus single select list
											break;
										default:
											fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) = {dbParameterName}";  // all field types plus single select list
											break;
									}

								}
								else // all other field types including single select list
								{
									fieldWhere = $" where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) = '{w.Value}'";  // all field types plus single select list
								}

								dbArgs.Add(dbParameterName, value);
								//load filtered field data into temp table
								whenTempTables.Append($@"
									drop table if exists #filtered_field_{fCount}

									select f.AssetID
									into #filtered_field_{fCount}
									from Field f
									inner join FieldType ft on ft.ID = f.FieldTypeID 
									{fieldWhere}");

								//filter by using inner join 
								whenSql.AppendLine($"inner join #filtered_field_{fCount} ftf{fCount} on ftf{fCount}.AssetId = A.Id");


							}
							else // invalid field type ID so the when is always not going to return anything
							{
								whenSql.Append($" where 1 =0 ");
							}
						}
						fCount++;
					}

					if (w.CheckType == "R")
					{
						whenWhereConditions.Add(
							$@"( 
								{(w.Operator == Operator.NotIn ? "Not" : "")} exists(
										SELECT TargetAsset.Uid as TargetAssetId
										FROM  
											[Intersect] I
											inner join [Asset] TargetAsset on TargetAsset.Object = '{w.TargetObject}' and TargetAsset.ObjectID = {w.TargetObjectID} and
											(I.ObjectAssetId = TargetAsset.Id or I.SubjectAssetId = TargetAsset.Id)
										WHERE 
											I.IntersectTypeID = {w.IntersectTypeID}			   								   
											and 
											((I.SubjectAssetId = A.Id and I.ObjectAssetId = TargetAsset.Id) 
											or 
											(I.ObjectAssetId = A.Id and I.SubjectAssetId = TargetAsset.Id))
										)
							)"
							);
						rCount++;
					}
				}
			}

			if (whenWhereConditions.Count > 0)
			{
				whenSql.Append(" where ");
				whenSql.Append(string.Join(" and ", whenWhereConditions));
			}

			return new ResponsibilityWhenQueryData
			{
				SqlQuery = whenSql.ToString(),
				TempTableQuery = whenTempTables.ToString(),
				DbParameters = dbArgs
			};
		}

		private string ThenSqlConnector(ResponsibilityRuleDefinitionThen then)
		{
			return then.MatchType == ResponsibilityMatchType.And ? "and" : "or";
		}

		public string GetThenResultsSql(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction transaction, bool includeName = true, string assetIDColumn = "", bool includeUid = true)
		{
			StringBuilder thenSql = new StringBuilder();

			string obj = "";
			string uniqueIdField = "ID";

			if ((rule.StructuredDefinition != null) && (rule.StructuredDefinition.Then != null) && (rule.StructuredDefinition.Then.Object != null || (rule.StructuredDefinition.Then.Conditions != null && rule.StructuredDefinition.Then.Conditions.All(c => c.Object != null))))
			{
				if (rule.StructuredDefinition.Then.Conditions != null && rule.StructuredDefinition.Then.Conditions.Count > 0)
				{
					var rulegroups = rule.StructuredDefinition.Then.Conditions.GroupBy(c => c.Object);
					foreach (var rulegroup in rulegroups)
					{
						//As it was discussed here https://infogix.slack.com/archives/GCYCRNR54/p1663685002231019
						//we can not be inside this loop whitout that key (https://infogix.slack.com/archives/GCYCRNR54/p1663751303398119?thread_ts=1663685002.231019&cid=GCYCRNR54)
						var rulegroupKey = rulegroup.Key ?? rule.StructuredDefinition.Then.Object;

						Dictionary<string, string> objectIds = new Dictionary<string, string>()
						{
							{ "Resource", "RO" },
							{ "Group", "OG" },
							{ "DefaultValue", "OG"}
						};
						StringBuilder rulegroupSql = new StringBuilder();
						StringBuilder whenSuffix = new StringBuilder();
						obj = "DefaultValue";
						uniqueIdField = "ID";

						rulegroupSql.Append($@"select distinct {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}");

						if (rulegroupKey == "GroupType")
						{
							obj = "Group";
							rulegroupSql.Append($"'G' as SecurityAsset, OG.ID as SecurityAssetID{(includeName ? ", OG.Name" : "")} {(includeUid ? ", OG.Name as Path, Z.uid " : "")} from	[Group] OG {(includeUid ? " inner join Asset Z on Z.ObjectID=OG.ID and Z.Object='Group' " : "")}");
						}

						if (rulegroupKey == "ResourceType")
						{
							obj = "Resource";
							uniqueIdField = "ResourceID";
							rulegroupSql.Append($@"'R' as SecurityAsset, RO.ResourceID as SecurityAssetID{(includeName ? ", RO.FirstName + ' ' + RO.LastName as Name" : "")} {(includeUid ? ", RO.FirstName + ' ' + RO.LastName as Path, RO.uid " : "")} from reporting.Global_Resource RO ");
						}

						foreach (var rc in rulegroup)
						{
							var sqlEscapedValue = rc.Value == null ? "" : rc.Value.Replace("'", "''");

							if (rc.FieldTypeID > 0)
							{

								var thenFieldType = Connection.Query<FieldType>("select * from FieldType where ID = @FieldTypeID", new { rc.FieldTypeID }, transaction: transaction).SingleOrDefault();
								whenSuffix.Append(whenSuffix.Length == 0 ? $" where ( " : $" {ThenSqlConnector(rule.StructuredDefinition.Then)} ");

								var fieldDetailInitialSql = $"select 1 from FieldDetail where Object = '{obj}' and ObjectID = {objectIds[obj]}.{uniqueIdField}";

								if (thenFieldType != null)
								{
									if (thenFieldType.AllowMultipleValues)// multiselect list
									{
										whenSuffix.Append(
											$"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and '{sqlEscapedValue}' in (select value from string_split([Value],',')) ) ");
									}
									else if (thenFieldType.Type == "Text")
									{
										switch (rc.Operator)
										{
											case Operator.NotEquals:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue = '{sqlEscapedValue}' )  ");
												break;
											case Operator.Contains:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}%' )  ");
												break;
											case Operator.NotContains:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}%' )  ");
												break;
											case Operator.StartsWith:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '{sqlEscapedValue}%' )  ");
												break;
											case Operator.EndsWith:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue LIKE '%{sqlEscapedValue}' )  ");
												break;
											case Operator.Populated:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and (FormattedValue is not null or LEN(FormattedValue)>0) ) ");
												break;
											case Operator.NotPopulated:
												whenSuffix.Append($"not exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and (FormattedValue is not null or LEN(FormattedValue)>0) ) ");
												break;
											default:
												whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and FormattedValue = '{sqlEscapedValue}' )  ");
												break;
										}

									}
									else // all other field types including single select list
									{
										whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and [Value] = '{sqlEscapedValue}' )  ");  // all field types plus single select list
									}
								}
								else
								{
									whenSuffix.Append($"exists({fieldDetailInitialSql} and FieldTypeID = {rc.FieldTypeID} and [Value] = '{sqlEscapedValue}' )  ");
								}
							}
							else
							{
								if (!string.IsNullOrEmpty(rc.FieldTypeName) && !string.IsNullOrEmpty(rc.Value))
								{
									if (rc.FieldTypeName == "Name")
									{
										whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"{objectIds[obj]}.{uniqueIdField} = {rc.Value}");
									}
									else
									{
										whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"{objectIds[obj]}.{rc.FieldTypeName} = '{sqlEscapedValue}'");
									}
								}
							}
						}

						if (rulegroupKey == "ResourceType")
						{
							whenSuffix.Append((whenSuffix.Length == 0 ? $" where ( " : " and ") + $"RO.[State] = 1");
							if (IsHideData3SixtyUsers)
							{
								whenSuffix.Append(" and (RO.Email not like '%@data3sixty.com' and RO.Email not like '%@infogix.com' and RO.Email not like '%@precisely.com')");
							}
						}

						if (whenSuffix.Length > 0)
						{
							whenSuffix.Append(" ) ");
						}

						if (rulegroupSql.Length > 0 || whenSuffix.Length > 0)
						{
							rulegroupSql.Append(" {0} " + whenSuffix);
						}

						thenSql.Append($"{(thenSql.Length > 0 ? " UNION " : "")}{rulegroupSql}");
					}
				}
				else
				{
					thenSql.Append($@"select distinct {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}");

					if (rule.StructuredDefinition.Then.Object == "GroupType")
					{
						thenSql.Append($"'G' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")} {(includeUid ? ", O.Name as Path, Z.uid " : "")} from	[Group] O {(includeUid ? " inner join Asset Z on Z.ObjectID=O.ID and Z.Object='Group' " : "")}");
					}

					if (rule.StructuredDefinition.Then.Object == "ResourceType")
					{
						thenSql.Append($@"'R' as SecurityAsset, O.ResourceID as SecurityAssetID{(includeName ? ", O.FirstName + ' ' + O.LastName as Name" : "")} {(includeUid ? ", O.FirstName + ' ' + O.LastName as Path, O.uid " : "")} from reporting.Global_Resource O ");
					}
				}
			}

			return thenSql.ToString();
		}

		#endregion

		public void ClearInvalidRelationRuleResults()
		{
			Connection.Execute("delete [dbo].[ResponsibilityRuleResultAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
			Connection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
		}

		#endregion

	}
}
