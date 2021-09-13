using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.queue;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model
{
    partial class CompanyContext: BaseContext
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
            catch
            {
                throw;
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
            var permissions = Permission.DeleteAsset.GetList();
            
            var responsibilityAssignments = Filter<ResponsibilityDetail>(i => 
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
            var permissions = Permission.DeleteAsset.GetList();

            var responsibilityAssignments = Query<int>(@"select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = 0
                                                        union select PermissionsBitMask from UserAssetPermissions(@r,@assetTypeId) where AssetID = @assetId", new { r = CurrentResourceID, assetTypeId, assetId });

            permissions.ForEach(p =>
            {
                p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
            });

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
                var assetTypeID = Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();
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
            var permission = Permission.ReadAsset;

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
                var asset = Assets.Single(a => a.ID == assetId);
                return HasUserReadPermission(asset.Object, asset.ObjectID, assetTypeId, CurrentResourceID);
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
                var assetTypeID = Query<int>("select AssetTypeID from Asset where ID = @id", new { id }).Single();                
                hasPermission = HasPermission(id, assetTypeID, permission);
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
                    var assetTypeID = Query<int>("select ID from AssetType where [Object] = @type and [ObjectID] = @id", new { id, type }).Single();
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
            var assetType = Query<AssetType>("select * from AssetType where ID = @id", new { id = assetTypeId }).Single();
            return HasAssetTypePermission(assetType.Object, assetTypeId, permission);
        }

        public void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation)
        {
            List<AssetMeasureModel> structuredMeasures = null;
            
            try
            {
                var assetType = Filter<AssetType>(a => a.Object == relation.ObjectType && a.ObjectID == relation.ObjectID).SingleOrDefault();
                var responsibility = Filter<ResponsibilityType>(a => a.ID == relation.ResponsibilityTypeID).SingleOrDefault();
                
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
            using (var trans = Database.BeginTransaction())
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
        /// <param name="cnn">The SQL connection object</param>
        /// <param name="ruleID">Optionall pass a specific rule by its ID.</param>
        public async Task ProcessResponsibilityRelationRules(int? ruleID = null, int timeout = 7200)
        {
            var results = new List<ResponsibilityAssetMeasureProcessedResult>();

            if (Connection.State != System.Data.ConnectionState.Open)
            {
                Connection.Open();
            }

            IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesToRun(ruleID).ConfigureAwait(false);

            var rulesRequiringRun = new List<int>();

            var ruleExceptionMessages = "";

            foreach (var rule in rules)
            {
                try
                {
                    if (await ShouldRuleRun(rule.ID).ConfigureAwait(false))
                    {
                        rulesRequiringRun.Add(rule.ID);
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
                }
                catch (ApplicationException ex)
                {
                    ruleExceptionMessages += ex.Message;
                }
                catch
                {
                    throw;
                }
            }

            // Send measure results to score engine.
            var today = DateTime.UtcNow.Date;
            var structuredMeasures = results.GroupBy(m => new { m.AssetUid })
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

            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    var thenSql = GetThenResultsSql(rule, false, transaction, false);
                    var whenSql = await GetWhenResultsSql(rule, transaction, false).ConfigureAwait(false);

                    thenSql = string.Format(thenSql, "");

                    //create impacted assets temporary table.
                    await Connection.ExecuteAsync("create table #changes (ActionType varchar(50), RuleID int, AssetID bigint)", transaction: transaction);

                    //merge into the asset table 
                    await Connection.ExecuteAsync($@"
                            merge [dbo].[ResponsibilityRuleResultAsset] as T
			                        using	(
					                            {whenSql}
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
                                    into #changes;", new { ruleId = rule.ID }, transaction: transaction);

                    //merge into the resource table
                    await Connection.ExecuteAsync($@"
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
                                            delete;
                        ", new { ruleId = rule.ID, appliesToType = rule.ApplyToType }, transaction: transaction);

                    var ruleResults = await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(@"
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
	and V.Definition <> '{}'",
                        new { today = DateTime.UtcNow.Date }, transaction: transaction
                    );

                    results.AddRange(ruleResults);

                    //drop impacted assets temporary table.
                    await Connection.ExecuteAsync("drop table if exists #changes", transaction: transaction);

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

            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    var thenSql = GetThenResultsSql(rule, false, transaction, false);
                    thenSql = string.Format(thenSql, "");

                    //create impacted assets temporary table.
                    await Connection.ExecuteAsync("create table #changes (ActionType varchar(50), RuleID int, AssetTypeID int)", transaction: transaction);

                    //merge into the asset table 
                    await Connection.ExecuteAsync(@"
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
                            into #changes;", new { ruleId = rule.ID }, transaction: transaction);

                    //merge into the resource table
                    await Connection.ExecuteAsync($@"
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
                                    delete;
                ", new { ruleId = rule.ID }, transaction: transaction);

                    var ruleResults = await Connection.QueryAsync<ResponsibilityAssetMeasureProcessedResult>(@"
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
		    and V.Definition <> '{}'",
                        new { today = DateTime.UtcNow.Date }, transaction: transaction
                    );

                    results.AddRange(ruleResults);

                    //drop impacted assets temporary table.
                    await Connection.ExecuteAsync("drop table if exists #changes", transaction: transaction);

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
        /// Load the Responsibility Rules that the rebuild process should run
        /// </summary>
        /// <param name="cnn">DB connection</param>
        /// <param name="ruleID">Specific responsibilty rule id to go after</param>
        /// <returns></returns>
        private async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(int? ruleID)
        {
            if (ruleID.HasValue)
            {
                return (await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value }));
            }

            return (await Connection.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule"));
        }


        private async Task<string> GetWhenResultsSql(ResponsibilityTypeRelationRule rule, SqlTransaction transaction, bool includeName = true)
        {
            string whenSql = "";

            whenSql += "select distinct A.ID as AssetID ";
            if (includeName)
            {
                whenSql += ", utility.GetAssetDisplayValueWrapper(A.ID) as Name ";
            }

            whenSql += $"from Asset A inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID} ";

            var fCount = 1;
            var rCount = 1;


            if (rule.StructuredDefinition != null && rule.StructuredDefinition.When != null)
            {
                foreach (var w in rule.StructuredDefinition.When)
                {
                    if (w.CheckType == "F")
                    {
                        if (w.FieldTypeID > 0)
                        {
                            var whenFieldType = await (Connection.QueryFirstOrDefaultAsync<dynamic>("select ID,AllowMultipleValues,Type from FieldType where ID = @FieldTypeID", new { w.FieldTypeID }, transaction: transaction));
                            
                            if (whenFieldType != null)
                            {
                                whenSql += $" cross apply (select coalesce(FT.DefaultValue, F.Value) as [Value] from FieldType FT left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = A.Object and F.ObjectID = A.ObjectID ";

                                whenSql += (whenFieldType.AllowMultipleValues) ?
                                    $"where FT.ID = {w.FieldTypeID} and '{w.Value}' in (select value from string_split(coalesce(F.Value, FT.DefaultValue),',')) ) FV{fCount}" : // multiselect list
                                    $"where FT.ID = {w.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) = '{w.Value}' ) FV{fCount}";  // all field types plus single select list
                            }
                            else // invalid field type ID so the when is always not going to return anything
                            {
                                whenSql += $"where 1 =0 ";
                            }
                        }
                        fCount++;
                    }
                    if (w.CheckType == "R")
                    {
                        whenSql += $@"inner join [Intersect] I{rCount} on 
        I{rCount}.IntersectTypeID = {w.IntersectTypeID} and 
        ( 
        (I{rCount}.Subject = A.Object and I{rCount}.SubjectID = A.ObjectID and I{rCount}.Object = '{w.TargetObject}' and I{rCount}.ObjectID = {w.TargetObjectID}) OR 
        (I{rCount}.Object = A.Object and I{rCount}.ObjectID = A.ObjectID and I{rCount}.Subject = '{w.TargetObject}' and I{rCount}.SubjectID = {w.TargetObjectID}) 
        ) ";
                        rCount++;
                    }
                }
            }

            return whenSql;
        }

        private string ThenSqlConnector(ResponsibilityRuleDefinitionThen then)
        {
            return then.MatchType == ResponsibilityMatchType.And ? "and" : "or";
        }

        private string GetThenResultsSql(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction transaction, bool includeName = true, string assetIDColumn = "")
        {            
            StringBuilder thenSql = new StringBuilder();
            
            int tCount = 1;
            string whenSuffix = "";
            string obj = "";
            string uniqueIdField = "ID";

            if ((rule.StructuredDefinition != null) && (rule.StructuredDefinition.Then != null) && (rule.StructuredDefinition.Then.Object != null))
            {
                thenSql.Append($@"select distinct {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}");

                if (rule.StructuredDefinition.Then.Object == "OrganizationType")
                {
                    obj = "Organization";
                    thenSql.Append($"'O' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")} from Organization O ");
                }

                if (rule.StructuredDefinition.Then.Object == "GroupType")
                {
                    obj = "Group";
                    thenSql.Append($"'G' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")}  from	[Group] O ");
                }

                if (rule.StructuredDefinition.Then.Object == "ResourceType")
                {
                    obj = "Resource";
                    uniqueIdField = "ResourceID";
                    thenSql.Append($@"'R' as SecurityAsset, O.ResourceID as SecurityAssetID{(includeName ? ", O.FirstName + ' ' + O.LastName as Name" : "")} from reporting.Global_Resource O ");
                }

                if (rule.StructuredDefinition.Then.Conditions != null)
                {
                    rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                    {
                        if (rc.FieldTypeID > 0)
                        {
                            var thenFieldType = Connection.Query<FieldType>("select * from FieldType where ID = @FieldTypeID", new { rc.FieldTypeID }, transaction: transaction).SingleOrDefault();                            
                            whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"exists(select 1 from FieldType FT left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = '{obj}' and F.ObjectID = O.{uniqueIdField} ";
                            if (thenFieldType != null)
                            {
                                whenSuffix += ((thenFieldType.AllowMultipleValues) ?
                                    $"where FT.ID = {rc.FieldTypeID} and '{rc.Value}' in (select value from string_split(coalesce(F.Value, FT.DefaultValue),',')) ) " :
                                    $"where FT.ID = {rc.FieldTypeID} and coalesce(F.Value, F.FormattedValue, FT.DefaultValue) = '{rc.Value}' )  ");
                            }
                            else
                            {
                                whenSuffix += ($"where FT.ID = {rc.FieldTypeID} and coalesce(F.Value, FT.DefaultValue) = '{rc.Value}' )  ");                                
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(rc.FieldTypeName) && !string.IsNullOrEmpty(rc.Value))
                            {
                                if (rc.FieldTypeName == "Name")
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"O.{uniqueIdField} = {rc.Value}";
                                }
                                else
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where ( " : $" {this.ThenSqlConnector(rule.StructuredDefinition.Then)} ") + $"O.{rc.FieldTypeName} = '{rc.Value}'";
                                }
                            }
                        }

                        tCount++;
                    });

                    if (!string.IsNullOrEmpty(whenSuffix))
                    {
                        whenSuffix += " ) ";
                    }
                }

                if (obj == "Resource")
                {
                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.[State] = 1";
                    if (IsHideData3SixtyUsers)
                    {
                        whenSuffix += " and (O.Email not like '%@data3sixty.com' and O.Email not like '%@infogix.com')";
                    }
                }
            }

            if (thenSql.Length > 0 || !string.IsNullOrEmpty(whenSuffix))
            {
                thenSql.Append(" {0} " + whenSuffix);
            }

            return thenSql.ToString();
        }

        #endregion

        public void ClearInvalidRelationRuleResults()
        {
            Connection.Execute("delete [dbo].[ResponsibilityRuleResultAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
            Connection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
        }

        public async Task<IEnumerable<ObjectResult>> GetWhenResults(ResponsibilityTypeRelationRule rule, SqlTransaction trans = null)
        {
            string sql = await GetWhenResultsSql(rule, trans).ConfigureAwait(false);
            return await Connection.QueryAsync<ObjectResult>(sql, transaction: trans, commandTimeout: 7200);
        }

        public IEnumerable<SecurityResult> GetThenResults(ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction trans = null)
        {
            string sql = GetThenResultsSql(rule, IsHideData3SixtyUsers, trans);
            return (string.IsNullOrEmpty(sql)) ?
                new List<SecurityResult>().AsEnumerable() :
                Connection.Query<SecurityResult>(sql.Replace(" {0} ", ""), transaction: trans, commandTimeout: 7200);
        }

        #endregion

    }
}
