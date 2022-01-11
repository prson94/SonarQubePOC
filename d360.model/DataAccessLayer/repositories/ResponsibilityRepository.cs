using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ResponsibilityRepository : BaseRepository, IResponsibilityRepository
    {
        ICompanyContext Company;
        internal IStorageProvider StorageProvider;
        internal IQueueSource QueueSource;

        public ResponsibilityRepository(ICompanyContext companyContext, IStorageProvider storageProvider, IQueueSource queueSource)
            : base(companyContext)
        {
            this.Company = companyContext;
            this.StorageProvider = storageProvider;
            this.QueueSource = queueSource;
        }

        public async Task<AssetResponsibilitiesApiModel> GetResponsibilities(IEnumerable<KeyValuePair<string, string>> queryParams, Guid responsibilityUidFilter, Guid assigneeUidFilter, Guid assetUidFilter, Guid assetTypeUidFilter, int pageSize, int pageNum, int timeout)
        {
            //get the assetids based on the input parameters
            var res = await getOwnershipAssets(queryParams, assetUidFilter, assetTypeUidFilter, responsibilityUidFilter, assigneeUidFilter, pageSize, pageNum, timeout);

            var assetIDList = res.items.Select(x => x.AssetID);
            //get the responsibilities that apply to these assets this should be for <= 250 asset ids only
            var responsibilities = await getOwnershipForGivenAssets(assetIDList, responsibilityUidFilter, assigneeUidFilter, timeout);

            var assetDictionary = res.items.ToDictionary(t => t.AssetID, t => t);

            //stitch the two result sets together assets list will be smaller worst case since it is paged. Use a dictionary O(k) lookup time and loop through
            // the responsibilities O(n) time.  Worse case O(kn)
            foreach (var responsibility in responsibilities)
            {
                AssetResponsibilityItemModel model = null;
                if (assetDictionary.TryGetValue(responsibility.AssetID, out model))
                {
                    if (model.Responsibilities == null) model.Responsibilities = new List<ResponsibilityApiModel>();

                    model.Responsibilities.Add(responsibility);
                }
            }

            return res;
        }

        public async Task<IEnumerable<OwnershipApiModel>> GetOwnership(Guid assetUid)
        {
            var res = new AssetResponsibilitiesApiModel();

            var asset = Company.Assets.Where(x => x.uid == assetUid).FirstOrDefault();

            var sql = $@"
                select 
                      R.ResponsibilityTypeName as Responsibility, 
                      RT.uid as ResponsibilityUid,
                      R.ResourceName as Resource,
                      R.SecurityAssetUid as ResourceUid,
                      CASE R.SecurityAsset
						WHEN 'G' THEN R.SecurityAssetUid
						WHEN 'O' THEN R.SecurityAssetUid
						ELSE Null
						END as GroupResourceUid,
                      R.Context as 'Description',
                      G.Name as 'Group',
                      CASE
                        WHEN R.RuleID = 0 THEN 'User'
	                    ELSE 'Rule'
	                    END AS AssignedBy,
                      IsVisible,	  
                      CASE R.SecurityAsset
	                    WHEN 'R' THEN 'User'
	                    WHEN 'O' THEN 'Organization'
	                    WHEN 'G' THEN 'Group'
	                    ELSE ''
	                    END as ResourceType
                      from [dbo].[ResponsibilityDetail] R
                      inner join [dbo].[ResponsibilityType] RT on RT.ID = R.[ResponsibilityTypeID]
                      left outer join [dbo].[Group] G on G.ID = R.SecurityAssetID and R.SecurityAsset = 'G'
                      left outer join [dbo].[Organization] O on O.ID = R.SecurityAssetID and R.SecurityAsset = 'O'
                where R.AssetID = @id or (R.AssetID = 0 and R.AssetTypeId = @typeId)";

            return (await Company.Database.Connection.QueryAsync<OwnershipApiModel>(sql, new { id = asset.ID, typeId = asset.AssetTypeID }));
        }

        public async Task<bool> HasOwnership(Guid assetUid)
        {
            var asset = Company.Assets.Where(x => x.uid == assetUid).FirstOrDefault();

            var sql = $@"
                select  CASE WHEN EXISTS (
                        select  1 
                        from    [dbo].[ResponsibilityDetail] R
                        where   R.AssetID = @id or (R.AssetID = 0 and R.AssetTypeId = @typeId)) THEN 1
                        ELSE 0 END";

            return (await Company.Database.Connection.QueryFirstAsync<bool>(sql, new { id = asset.ID, typeId = asset.AssetTypeID }));
        }

        public async Task<ResponsibilityTypeRuleStatsViewModel> GetResponsibilityRuleStats(Guid responsibilityTypeRuleUid)
        {
            var responsibilityTypeRuleStats = new ResponsibilityTypeRuleStatsViewModel();
            responsibilityTypeRuleStats.AssignedUsers = await Company.Database.Connection.QueryFirstOrDefaultAsync<int>(@"                            
                                    select sum(a.cnt) from 
                                    (select 
	                                    count(1) as cnt
                                    from
	                                    [dbo].[ResponsibilityTypeRelationRule] rtr
	                                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on (rsa.RuleID = rtr.id)
	                                    inner join [reporting].Global_Resource r on (r.resourceid = rsa.securityassetid and rsa.securityasset = 'R')
                                    where rtr.[uid] = @uid
                                    union all
                                    select 
	                                    count(1) as cnt
                                    from
	                                    [dbo].[ResponsibilityTypeRelationRule] rtr
	                                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on (rsa.RuleID = rtr.id)
	                                    inner join [dbo].ResourceGroup gr on (gr.groupid = rsa.securityassetid and rsa.securityasset = 'G')
                                    where rtr.[uid] = @uid
                                    union all
                                    select 
	                                    count(1) as cnt
                                    from
	                                    [dbo].[ResponsibilityTypeRelationRule] rtr
	                                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on (rsa.RuleID = rtr.id)		
	                                    inner join [dbo].OrganizationResource og on (og.OrganizationID = rsa.SecurityAssetID and rsa.SecurityAsset = 'O')
                                    where rtr.[uid] = @uid
                                    ) a
                            ", new { uid = responsibilityTypeRuleUid.ToString() }, commandTimeout: ApiTimeout);

            responsibilityTypeRuleStats.AssignedAssets = await Company.Database.Connection.QueryFirstOrDefaultAsync<int>(@"                            
                                    select sum(a.cnt) from 
                                    (
	                                    select
		                                    count(1) as cnt
	                                    from [dbo].[ResponsibilityTypeRelationRule] rtr
		                                    inner join [dbo].ResponsibilityRuleResultAsset ra on (rtr.id = ra.RuleID)	
	                                    where
		                                    rtr.ApplyToType = 0 and rtr.[uid] = @uid
	                                    union all
	                                    select
		                                    count(1) as cnt
	                                    from [dbo].[ResponsibilityTypeRelationRule] rtr
		                                    inner join [dbo].ResponsibilityRuleResultAsset ra on (rtr.id = ra.RuleID)	
		                                    inner join [dbo].asset a on(ra.AssetTypeID = a.AssetTypeID)
	                                    where
		                                    rtr.ApplyToType = 1 and rtr.[uid] = @uid
                                    ) a                                    
                            ", new { uid = responsibilityTypeRuleUid.ToString() }, commandTimeout: ApiTimeout);
            return responsibilityTypeRuleStats;
        }

        public async Task<IEnumerable<ResponsibilityTypeRuleViewModel>> GetResponsibilityRules(Guid responsibilityTypeUid)
        {
            var results = await Company.QueryAsync<ResponsibilityTypeRuleViewModel>(@"
                            select
                                rtr.[uid]
	                            ,rtr.[name]
	                            ,rtr.Context
	                            ,rtr.IsVisible
	                            ,rtr.ApplyToType
	                            ,rtr.LastRunOn
	                            ,rtr.[Definition] as [DefinitionRaw]
	                            ,att.[uid] as AssetTypeUid
	                            ,att.[Name] as AssetTypeName
	                            ,att.Class	 
                            from [dbo].[responsibilitytyperelationrule] rtr
	                            inner join [dbo].ResponsibilityType r on (rtr.responsibilitytypeid = r.id)
	                            inner join [dbo].[AssetType] att on (rtr.[Object] = att.[Object] and rtr.ObjectID = att.ObjectID)
                            where 
	                            r.[uid] = @uid 
                            ", new { uid = responsibilityTypeUid.ToString() }, ApiTimeout);

            results.ToList().ForEach((res) =>
            {
                var definition = res.Definition;
                definition?.When?.FindAll((d) => d.IntersectTypeID > 0)?.ForEach((when) =>
                {
                    when.IntersectTypeUID = Company.IntersectTypes.FirstOrDefault(x => x.ID == when.IntersectTypeID).uid;
                });

                definition?.When?.FindAll((d) => d.TargetObjectID > 0 && d.TargetObject != null)?.ForEach((asset) =>
                {
                    asset.AssetUID = Company.Assets.FirstOrDefault(x => x.Object == asset.TargetObject && x.ObjectID == asset.TargetObjectID).uid;
                });

                definition?.Then?.Conditions?.FindAll((d) => d.IntersectTypeID > 0)?.ForEach((when) =>
                {
                    when.IntersectTypeUID = Company.IntersectTypes.FirstOrDefault(x => x.ID == when.IntersectTypeID).uid;
                });

                definition?.Then?.Conditions?.FindAll((d) => d.TargetObjectID > 0 && d.TargetObject != null)?.ForEach((asset) =>
                {
                    asset.AssetUID = Company.Assets.FirstOrDefault(x => x.Object == asset.TargetObject && x.ObjectID == asset.TargetObjectID).uid;
                });

                res.DefinitionRaw = JsonConvert.SerializeObject(definition);
            });
            return results;
        }

        public async Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocations(Guid responsibilityTypeUid)
        {
            return await FetchResponsibilityTypeAllocations(responsibilityTypeUid).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocationsByAsset(Guid assetTypeUid)
        {
            return await FetchResponsibilityTypeAllocations(assetTypeUid, "A").ConfigureAwait(false);
        }

        private async Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> FetchResponsibilityTypeAllocations(Guid uid, string type = "R")
        {
            string sql = @"
                            select
                                rt.[Name] as ResponsibilityTypeName, 
	                            rt.[uid] as ResponsibilityTypeUid,
	                            att.Class as AssetClass,
	                            att.[Name] as AssetTypeName,
	                            P.[Path] as AssetTypePath,
	                            att.[uid] as AssetTypeUid,
	                            rtr.PermissionsBitMask as PermissionsMask
                            from 
	                            [dbo].responsibilitytype rt
	                            inner join [dbo].responsibilitytyperelation rtr on (rt.id = rtr.ResponsibilityTypeID)
	                            inner join [dbo].assettype att on(att.[Object] = rtr.ObjectType and att.ObjectID = rtr.ObjectID)
                                cross apply dbo.GetAssetTypeTextPathById(att.ID, ' / ') P
                            where ";
            sql += (type == "A") ? "att.[uid] = @uid"  : "rt.[uid] = @uid";
            return await Company.QueryAsync<ResponsibilityTypeAllocationViewModel>(sql, new { uid = uid.ToString() }, ApiTimeout);
        }

        public async Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypesByAssetUid(Guid assetTypeUid)
        {
            return await Company.QueryAsync<ResponsibilityTypeViewModel>(@"
                            select 
	                            rt.[Name], 
	                            rt.[Description], 
	                            rt.[uid], 
	                            rt.[UpdatedOn]
                            from [dbo].[responsibilitytype] rt
	                            inner join [dbo].[ResponsibilityTypeRelation] rtr on (rt.id = rtr.ResponsibilityTypeID)
	                            inner join [dbo].[AssetType] att on (att.[Object] = rtr.ObjectType and att.ObjectID = rtr.ObjectID)
                            where
	                            att.[uid] = @uid
                            order by [Name] asc
                            ", new { uid = assetTypeUid }, ApiTimeout);
        }

        public async Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypes()
        {
            return await Company.QueryAsync<ResponsibilityTypeViewModel>(@"
                            select [Name], [Description], [uid], [UpdatedOn] from [dbo].[responsibilitytype] order by [Name] asc
                            ", ApiTimeout);
        }

        public async Task<dynamic> GetResponsibilityType(Guid uid)
        {
            return await Company.QueryFirstOrDefaultAsync<dynamic>($@"
                            select [ID], [Name], [Description], [uid], [UpdatedOn] from [dbo].[responsibilitytype] WHERE [uid] = '{uid.ToString()}'
                            ", ApiTimeout);
        }

        private async Task<IEnumerable<ResponsibilityApiModel>> getOwnershipForGivenAssets(IEnumerable<long> assetIDList, Guid responsibilityUidFilter, Guid assigneeUidFilter, int timeout = 300)
        {
            if (assetIDList == null) return null;
            var responsibilityFilterCriteria = "";
            var assigneeFilterCriteria = "";
            var overrideAssigneeFilterCriteria = "";
            var permissionsCriteria = "";
            DynamicParameters dbArgs = new DynamicParameters();

            if (assigneeUidFilter != Guid.Empty)
            {
                assigneeFilterCriteria = $" and s.[uid] = @assigneeUidFilter";
                overrideAssigneeFilterCriteria = $" and a.[uid] = @assigneeUidFilter";
                dbArgs.Add("assigneeUidFilter", assigneeUidFilter);
            }

            if (responsibilityUidFilter != Guid.Empty)
            {
                responsibilityFilterCriteria = $" and rt.[uid] = @responsibilityTypeUid";
                dbArgs.Add("responsibilityTypeUid", responsibilityUidFilter);
            }

            if (!Company.CurrentResourceIsAdmin)
            {
                permissionsCriteria = $" and exists(select 1 from UserAssetPermissions(@r,a.AssetTypeID) u where u.PermissionsBitMask & @p = @p and (u.AssetID = a.ID or (u.AssetID = 0 and u.AssetTypeID = a.AssetTypeID)))";
                dbArgs.Add("r", Company.CurrentResourceID);
                dbArgs.Add("p", (int)Permission.ReadResponsibilities);
            }

            StringBuilder assetIDSQL = new StringBuilder();
            assetIDSQL.Append($@"drop table if exists #assetIds
                                create table #assetIds (
	                                id bigint
                                )
                                CREATE CLUSTERED INDEX ix_assetIds ON #assetIds ([id]);");

            
            for (int i = 0; i < assetIDList.Count(); i ++)
            {
                if(i % 1000 == 0)
                {
                    assetIDSQL.Append($@"
                                insert into #assetIds (id) VALUES ({assetIDList.ElementAt(i)})");                    
                }
                else
                {
                    assetIDSQL.Append($",({assetIDList.ElementAt(i)})");
                }
                              
            }

            var sql = $@"                                                
                        {assetIDSQL}                        

                       select 
                        a.id as 'AssetID',
	                    'rule' as 'AssigneeMethod',
	                    rsa.SecurityAsset,
	                    rsa.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    1 as 'AssignedToType',
                        s.[uid] as 'AssigneeUid',
                        s.[Name] as 'AssigneeName'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationRule] rr on rr.ResponsibilityTypeID = rt.id
	                    inner join [dbo].[ResponsibilityTypeRelation] rtr on rtr.ObjectID = rr.ObjectID and rtr.ObjectType = rr.[Object]
	                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on rsa.RuleID = rr.id
	                    inner join [dbo].[assettype] att on att.[object] = rr.[object] and att.objectid = rr.objectid                        
	                    inner join [dbo].asset a on a.AssetTypeID = att.id
                        inner join #assetIds assetIDs on a.id = assetIDs.id
                        cross apply [dbo].[GetSecurityAssetUid](rsa.SecurityAsset,rsa.SecurityAssetID) s
                    where
	                    rr.applytotype = 1
                        {responsibilityFilterCriteria} {assigneeFilterCriteria} {permissionsCriteria}
                    union
                    select 
                        ra.assetid as 'AssetID',
	                    'rule' as 'AssigneeMethod',
	                    rsa.SecurityAsset,
	                    rsa.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    0 as 'AssignedToType',
                        s.[uid] as 'AssigneeUid',
                        s.[Name] as 'AssigneeName'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationRule] rr on rr.ResponsibilityTypeID = rt.id
	                    inner join [dbo].[ResponsibilityTypeRelation] rtr on rtr.ObjectID = rr.ObjectID and rtr.ObjectType = rr.[Object]
	                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on rsa.RuleID = rr.id
	                    inner join [dbo].[ResponsibilityRuleResultAsset] ra on ra.RuleID = rr.id	
                        inner join #assetIds assetIDs on ra.assetid = assetIDs.id
                        inner join [dbo].[asset] a on a.id = assetIDs.id
                        cross apply [dbo].[GetSecurityAssetUid](rsa.SecurityAsset,rsa.SecurityAssetID) s
                    where
	                    rr.applytotype = 0
                        {responsibilityFilterCriteria} {assigneeFilterCriteria} {permissionsCriteria}
                    union
                    select 
                        oride.assetid as 'AssetID',
	                    'direct' as 'AssigneeMethod',
	                    oride.SecurityAsset,
	                    oride.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    0 as 'AssignedToType',
                        s.[uid] as 'AssigneeUid',
                        s.[Name] as 'AssigneeName'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationOverrideItem] oride on oride.ResponsibilityTypeID = rt.id	  
                        inner join #assetIds assetIDs on oride.assetid = assetIDs.id
                        inner join [dbo].[asset] a on a.id = assetIDs.id
                        cross apply [dbo].[GetSecurityAssetUid](oride.SecurityAsset,oride.SecurityAssetID) s                        
                    where
	                    1=1 {responsibilityFilterCriteria} {overrideAssigneeFilterCriteria} {permissionsCriteria}";

            return (await Company.Database.Connection.QueryAsync<ResponsibilityApiModel>(sql, dbArgs, null, timeout));
        }

        private async Task<AssetResponsibilitiesApiModel> getOwnershipAssets(IEnumerable<KeyValuePair<string, string>> queryParams, Guid assetUid, Guid assetTypeUid, Guid responsibilityUidFilter, Guid assigneeUidFilter, int pageSize, int pageNum, int timeout = 300)
        {
            var res = new AssetResponsibilitiesApiModel();
            DynamicParameters dbArgs = new DynamicParameters();
            var orderBySql = "order by A.ID";
            var offsetSql = "";
            var assetQueryFilterSql = "";
            var responsibilityQueryFilterSql = "";
            var responsibilityQueryAdditionalJoins = "";
            var permissionsFilter = "";
            List<string> assetQueryFilters = new List<string>();
            List<string> responsibilityQueryFilters = new List<string>();

            if (assetUid != Guid.Empty)
            {
                assetQueryFilters.Add($"a.uid = @assetUid");
                dbArgs.Add("@assetUid", assetUid);
            }

            if (assetTypeUid != Guid.Empty)
            {
                assetQueryFilters.Add($"att.uid = @assettypeUid");
                dbArgs.Add("@assettypeUid", assetTypeUid);
            }

            if (responsibilityUidFilter != Guid.Empty)
            {
                responsibilityQueryFilters.Add($"rt.uid = @respUid");
                dbArgs.Add("@respUid", responsibilityUidFilter);
            }

            if (assigneeUidFilter != Guid.Empty)
            {
                var assigneeSql = "select a.[Object] as Obj, a.[Objectid] from asset a where a.uid = @assigneeUid";
                var detail = Company.Database.Connection.QueryFirstOrDefault<dynamic>(assigneeSql, new { assigneeUid = assigneeUidFilter });
                var securityAsset = "";
                if (detail != null)
                {
                    switch (((detail.Obj) ?? "").ToUpper())
                    {
                        case "GROUP":
                            securityAsset = "G";
                            break;
                        case "ORGANIZATION":
                            securityAsset = "O";
                            break;
                        default:
                            securityAsset = "R";
                            break;
                    }

                    responsibilityQueryAdditionalJoins = " inner join ResponsibilityRuleResultSecurityAsset rsa on (rsa.ruleid = rr.id)  ";

                    dbArgs.Add("@securityAsset", securityAsset);
                    dbArgs.Add("@securityAssetID", detail.Objectid);
                    responsibilityQueryFilters.Add($"rsa.securityasset = @securityAsset");
                    responsibilityQueryFilters.Add($"rsa.securityassetid = @securityAssetID");
                }
            }

            if (pageSize < 1) pageSize = 1;
            if (pageNum < 1) pageNum = 1;

            res.pageNum = pageNum;
            res.pageSize = pageSize;

            if (assetQueryFilters.Any())
            {
                assetQueryFilterSql = " and " + String.Join(" and ", assetQueryFilters);
            }

            if (responsibilityQueryFilters.Any())
            {
                responsibilityQueryFilterSql = " and " + String.Join(" and ", responsibilityQueryFilters);
            }

            permissionsFilter = $" and not exists(select 1 from AssetTypesUserCantRead({ Company.CurrentResourceID}) u where u.AssetTypeID = a.AssetTypeID) and not exists(select 1 from AssetsByTypeUserCantRead({ Company.CurrentResourceID}, a.AssetTypeID) u where u.AssetID = a.ID)";


            var countSql = $@"        select
	                                        count(1)
                                        from 
	                                        asset a
	                                        inner join assetType att on a.AssetTypeID = att.id
                                        where 
	                                        (exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) {responsibilityQueryAdditionalJoins} where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql} )		
							                    or 
						                    exists (select 1 from ResponsibilityTypeRelationOverrideItem rsa inner join ResponsibilityType rt on(rsa.ResponsibilityTypeID = rt.id) where rsa.AssetID = a.ID {responsibilityQueryFilterSql} )
							                    or
						                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) {responsibilityQueryAdditionalJoins} where rd.AssetTypeID = a.assettypeid and rr.applytotype = 1 {responsibilityQueryFilterSql} )		
						                    )
                                             {assetQueryFilterSql}
                                             {permissionsFilter}            ";

            //run the count query if count is zero bail no point in continuing
            res.total = (await Company.Database.Connection.QuerySingleOrDefaultAsync<int>(countSql, dbArgs, null, timeout));

            if (res.total <= 0)
            {
                res.items = new List<AssetResponsibilityItemModel>();

            }
            else
            {
                offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";

                var sql = $@"
with rs as (
    select distinct a.assetId
    from (
        select rd.assetId from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) inner join asset a on (rd.assetid = a.id) {responsibilityQueryAdditionalJoins} where rr.applytotype = 0 {responsibilityQueryFilterSql}
        union all
        select rsa.assetId from ResponsibilityTypeRelationOverrideItem rsa inner join ResponsibilityType rt on(rsa.ResponsibilityTypeID = rt.id) inner join asset a on (rsa.assetId = a.id)  where 1=1 {responsibilityQueryFilterSql}
        union all
        select a.id as assetId from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) inner join asset a on (rd.AssetTypeID = a.assettypeid) {responsibilityQueryAdditionalJoins} where rr.applytotype = 1 {responsibilityQueryFilterSql} 
    ) a
)
select
	a.id AS AssetId,
	a.AssetTypeID as assettypeid,
	a.uid AS AssetUid,
	att.uid AS AssetTypeUid,
	att.name AS AssetTypeName
from		rs
			inner join asset a on rs.assetId =  a.id
			inner join assetType att ON a.assetTypeID = att.id
where 1=1
                        {assetQueryFilterSql}
                        {permissionsFilter}   	                    
                        {orderBySql} {offsetSql} 
                    ";

                res.items = (await Company.Database.Connection.QueryAsync<AssetResponsibilityItemModel>(sql, dbArgs, null, timeout)).ToList();

            }
            return res;
        }


        public List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypeUpserts, ApiExecution execution)
        {
            Company.Add(execution);

            List<ResponsibilityTypeUpsertResult> results = null;
            try
            {
                results = Company.UpsertResponsibilityTypes(execution, responsibilityTypeUpserts);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public ResponsibilityTypeDeleteResult DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel model)
        {
            var result = new ResponsibilityTypeDeleteResult();
            result.Uid = model.Uid;
            result.Success = false;

            if (result.Uid == null || result.Uid == Guid.Empty)
            {
                result.Message = "Invalid Uid";
                return result;
            }

            var resType = Company.ResponsibilityTypes.FirstOrDefault(x => x.UID == result.Uid);
            if (resType == null)
            {
                result.Message = $"Responsibility type with uid {result.Uid} not found";
                return result;
            }

            IQueryable<ResponsibilityTypeRelation> relations = Company.ResponsibilityTypeRelations.Where(x => x.ResponsibilityTypeID == resType.ID);

            if (relations.Count() > 0 && model.Cascade != true)
            {
                result.Message = $"Responsibility type has asset assignments and cannot be deleted. Use cascade=true to delete all assignments and rules";
                return result;
            }

            var impactedMeasureVersions = Company.GetImpactedMeasureVersionsBy(MetricGovernanceCheckType.Owner, resType.ID);

            var deleteSQL = @"  delete RRRSA from ResponsibilityRuleResultSecurityAsset RRRSA
	                                    inner join ResponsibilityTypeRelationRule RTRR ON RRRSA.RuleID = RTRR.ID
	                                    inner join ResponsibilityType RT on RT.ID = RTRR.ResponsibilityTypeID
                                    where RT.uid = @ResponsibilityTypeUid

                                    delete RRRA from ResponsibilityRuleResultAsset RRRA
	                                    inner join ResponsibilityTypeRelationRule RTRR ON RRRA.RuleID = RTRR.ID
	                                    inner join ResponsibilityType RT on RT.ID = RTRR.ResponsibilityTypeID
                                    where RT.uid = @ResponsibilityTypeUid

                                    delete RTRR from ResponsibilityTypeRelationRule RTRR
	                                    inner join ResponsibilityType RT on RT.ID = RTRR.ResponsibilityTypeID
                                    where RT.uid = @ResponsibilityTypeUid

                                    delete RTR from ResponsibilityTypeRelation RTR
	                                    inner join ResponsibilityType RT on RT.ID = RTR.ResponsibilityTypeID
                                    where RT.uid = @ResponsibilityTypeUid

                                    delete ResponsibilityType 
                                    where uid = @ResponsibilityTypeUid";

            Company.Query<int>(deleteSQL, new { ResponsibilityTypeUid = model.Uid }).ToList();
            result.Success = true;

            if (impactedMeasureVersions.Count > 0)
            {
                Company.CreateCheckDependencyRemovedNotificationExecution(impactedMeasureVersions);
            }

            return result;
        }

        public Task<IEnumerable<ClaimsViewModel>> GetClaims()
        {
            var permissions = Permission.ReadResponsibilities.GetList();
            var claims = permissions.Select(x => new ClaimsViewModel()
            {
                ID = (int)x.ID,
                Name = x.Name,
                Category = x.Category,
                Description = x.Description
            }).ToList();
            return Task.FromResult<IEnumerable<ClaimsViewModel>>(claims);
        }


        public ResponsibilityTypeAllocationResponseModel AddAllocation(ResponsibilityType responsibiltyType, AssetType assetType, IEnumerable<int> permissionsBitMask)
        {
            try
            {
                var rtr = new ResponsibilityTypeRelation()
                {
                    PermissionsBitMask = permissionsBitMask.Sum(i => i),
                    ObjectID = assetType.ObjectID,
                    ObjectType = assetType.Object,
                    ResponsibilityType = responsibiltyType,
                    ResponsibilityTypeID = responsibiltyType.ID,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                };
                Company.ResponsibilityTypeRelations.Add(rtr);
                Company.SaveChanges();
                return new ResponsibilityTypeAllocationResponseModel()
                {
                    AssetTypeUid = assetType.uid,
                    Message = $"Allocation added.",
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new ResponsibilityTypeAllocationResponseModel()
                {
                    AssetTypeUid = assetType.uid,
                    Message = e.InnerException != null ? e.InnerException.Message : e.Message,
                    Success = false
                };
            }

        }


        public ResponsibilityTypeAllocationResponseModel EditAllocation(ResponsibilityType responsibility, AssetType assetType, List<int> permissions)
        {
            try
            {
                var rtr = Company.Filter<ResponsibilityTypeRelation>(x => x.ObjectID == assetType.ObjectID && x.ObjectType == assetType.Object && x.ResponsibilityTypeID == responsibility.ID).FirstOrDefault();
                rtr.PermissionsBitMask = permissions.Sum(i => i);
                rtr.UpdatedBy = Company.CurrentResourceID;
                rtr.UpdatedOn = DateTime.UtcNow;
                Company.SaveChanges();
                return new ResponsibilityTypeAllocationResponseModel()
                {
                    AssetTypeUid = assetType.uid,
                    Message = $"Allocation edited.",
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new ResponsibilityTypeAllocationResponseModel()
                {
                    AssetTypeUid = assetType.uid,
                    Message = e.InnerException != null ? e.InnerException.Message : e.Message,
                    Success = false
                };
            }
        }

        public async Task<ResponsibilityTypeAllocationResponseModel> DeleteAllocation(ResponsibilityType responsibility, AssetType assetType, bool cascade)
        {
            try
            {
                //find the responsibility type
                var rtr = Company.Filter<ResponsibilityTypeRelation>(x => x.ObjectID == assetType.ObjectID && x.ObjectType == assetType.Object && x.ResponsibilityTypeID == responsibility.ID).FirstOrDefault();
                
                // Scoring - get asset measures that are impacted
                var structuredMeasures = Company.GetMeasureModelsBasedOnResponsibilityAllocation(assetType, responsibility);

                //check is there responsibility rules for this responsibility type
                var ruleUids = Company.Filter<ResponsibilityTypeRelationRule>(i => i.ResponsibilityTypeID == responsibility.ID && i.Object == assetType.Object && i.ObjectID == assetType.ObjectID).Select(i => i.UID.Value).ToList();
                if (ruleUids.Any())
                {
                    //if it has rules and cascade id false the error this response
                    if (cascade)
                    {
                        //delete rules
                        await DeleteResponsibilityRulesAsync(responsibility.UID, ruleUids);

                        Company.Execute(
                            "delete T from ResponsibilityTypeRelationOverrideItem T inner join Asset A on A.AssetTypeID = @AssetTypeID and A.ID = T.AssetID and T.ResponsibilityTypeID = @ResponsibilityTypeID",
                            new { AssetTypeID = assetType.ID, ResponsibilityTypeID = responsibility.ID }
                        );

                        Company.Delete(rtr);

                        // If you made it this far, then send to scoring engine.
                        Company.CreateMeasureChangedResultExecution(structuredMeasures);

                        return new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = assetType.uid,
                            Message = $"Allocation and {ruleUids.Count()} Responsibility Rule(s) deleted.",
                            Success = true
                        };
                    }
                    else
                    {
                        return new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = assetType.uid,
                            Message = $"Cannot remove Allocation. Allocation has Responsibility Rules defined and Cascade was set to false.",
                            Success = false
                        };
                    }
                }
                else
                {
                    Company.ResponsibilityTypeRelations.Remove(rtr);
                    Company.SaveChanges();

                    // If you made it this far, then send to scoring engine.
                    Company.CreateMeasureChangedResultExecution(structuredMeasures);

                    return new ResponsibilityTypeAllocationResponseModel()
                    {
                        AssetTypeUid = assetType.uid,
                        Message = $"Allocation deleted.",
                        Success = true
                    };
                }
            }
            catch (Exception e)
            {
                return new ResponsibilityTypeAllocationResponseModel()
                {
                    AssetTypeUid = assetType.uid,
                    Message = e.InnerException != null ? e.InnerException.Message : e.Message,
                    Success = false
                };
            }
        }

        public string GetResponsibilityTypeUsedInOwnershipLookupMessage(ResponsibilityType responsibility, AssetType assetType)
        {
            string errorMessage = "";
            List<string> usedByOwnershipFields = Company.Query<string>($@"SELECT ft.FriendlyName
                    FROM [dbo].[AssetType] at
                    INNER JOIN [dbo].[FieldType] ft on at.id = ft.AssetTypeID
                    INNER JOIN [dbo].[fieldTypeLookup] ftl on ft.id = ftl.FieldTypeId
                    WHERE ft.Type = '{DataType.OwnershipLookup}'
                    AND at.id = @assetTypeId
                    AND TRY_CAST(JSON_VALUE(FTL.Definition, '$.ResponsibilityType') AS int) = @responsibilityTypeId", new { responsibilityTypeId = responsibility.ID, assetTypeId = assetType.ID }).ToList();

            if (usedByOwnershipFields.Any())
            {
                string fieldsMultiple = usedByOwnershipFields.Count() > 1 ? "s" : "";
                string fields = "'" + string.Join("', '", usedByOwnershipFields.ToArray()) + "'";
                errorMessage = $"This asset assignment is used in the field definition{fieldsMultiple} {fields}. Please update or delete this prior to deleting the asset assignment.";
            }
            return errorMessage;
        }

        public ResponsibilityType GetResponsibilityTypeByUID(Guid uid)
        {
            return Company.ResponsibilityTypes.FirstOrDefault(x => x.UID == uid);
        }

        public bool IsValidResponsibilityForAsset(Guid responsibilityUid, Guid assetUid)
        {
            return Company.Query<bool>(@"select 
                            case when count(*) > 0
                             then 1
                             else 0
                            end as isValid
                              from ResponsibilityType rt
                              inner join asset a on a.uid = @assetUid
                              inner join assettype at on a.assettypeid = at.id
                              inner join responsibilitytyperelation rtr on rtr.responsibilitytypeid = rt.id
                              where rt.uid = @responsibilityUID and at.object = rtr.ObjectType and at.ObjectID = rtr.ObjectID", new { assetUid, responsibilityUid }).FirstOrDefault();
        }

        public IEnumerable<SecurityAssetModel> GetSecurityAssetModelsForResources(List<Guid> resourceUids, Guid assetUid, Guid responsibilityUid)
        {
            return Company.Query<SecurityAssetModel>(@"select 
                    A.uid,
                    A.ObjectId as SecurityAssetId, 
                    case A.Object 
	                    when 'Group' then 'G'
                        when 'Organization' then 'O'
                        when 'Resource' then 'R'
	                    else NULL
                    end as SecurityAsset,
                    case 
                       when RTOG.Id is not null then 1
                       when RTOO.Id is not null then 1
                       when RTOR.Id is not null then 1
                       else 0
                    end as 'Exists'
                    from asset A
                    inner join ResponsibilityType RT on rt.uid = @responsibilityUid
                    inner join Asset MainAsset on MainAsset.uid = @assetUid
                    left join ResponsibilityTypeRelationOverrideItem RTOG ON RTOG.ResponsibilityTypeId = RT.Id and RTOG.AssetId = mainasset.id and RTOG.securityassetid = a.objectid and A.object = 'Group' and RTOG.SecurityAsset ='G'
                    left join ResponsibilityTypeRelationOverrideItem RTOO ON RTOO.ResponsibilityTypeId = RT.Id and RTOO.AssetId = mainasset.id and RTOO.securityassetid = a.objectid and A.object = 'Organization' and RTOO.SecurityAsset ='O'
                    left join ResponsibilityTypeRelationOverrideItem RTOR ON RTOR.ResponsibilityTypeId = RT.Id and RTOR.AssetId = mainasset.id and RTOR.securityassetid = a.objectid and A.object = 'Resource' and RTOR.SecurityAsset ='R'
                    where A.uid in @resourceUids", new { resourceUids, assetUid, responsibilityUid }, ApiTimeout).ToList();
        }

        private void sendAssetMeasureQueueForOverrides(ResponsibilityType responsibilityType, Asset asset)
        {
            var today = DateTime.UtcNow.Date;
            var measureResults = Company.Query<ResponsibilityAssetMeasureProcessedResult>(@"
    select  A.Uid as AssetUid, 
            M.Uid as MetricAssetUid,
            V.Uid as MetricAssetVersionUid,
            M.AllocationUid
    from    Asset A 
            inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @ID
            inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
            inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
            inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
                and ( 
                    (@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
                    (@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
                    ) 
                and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
                and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = @ResponsibilityTypeUid
		        and V.Definition <> '{}'", new { asset.ID, ResponsibilityTypeUid = responsibilityType.UID, today });

            var structuredMeasures = measureResults.GroupBy(m => new { m.AssetUid })
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
            Company.CreateMeasureChangedResultExecution(structuredMeasures);
        }

        public void InsertResponsibilityOverrides(ResponsibilityType responsibilityType, Asset asset, List<SecurityAssetModel> resources, string context)
        {
            if (responsibilityType == null)
                throw new ArgumentNullException("Responsibility Type cannot be null.");

            if (asset == null)
                throw new ArgumentNullException("Asset cannot be null.");

            if (resources.Count == 0)
                throw new ArgumentNullException("Resources cannot be empty.");

            List<ResponsibilityTypeRelationOverrideItem> items = new List<ResponsibilityTypeRelationOverrideItem>();            

            resources.Where(x => x.SecurityAsset == "R" || x.SecurityAsset == "G" || x.SecurityAsset == "O").ToList()
                .ForEach(x =>
            {
                items.Add(new ResponsibilityTypeRelationOverrideItem()
                {
                    AssetID = asset.ID,
                    Context = context,
                    ResponsibilityTypeID = responsibilityType.ID,
                    SecurityAsset = x.SecurityAsset,
                    SecurityAssetID = x.SecurityAssetId,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                });
            });

            Company.ResponsibilityTypeRelationOverrideItems.AddRange(items);
            Company.SaveChanges();

            sendAssetMeasureQueueForOverrides(responsibilityType, asset);
        }
        
        public void DeleteResponsibilityOverrides(ResponsibilityType responsibilityType, Asset asset, List<SecurityAssetModel> resources)
        {
            if (responsibilityType == null)
                throw new ArgumentNullException("Responsibility Type cannot be null.");

            if (asset == null)
                throw new ArgumentNullException("Asset cannot be null.");

            if (resources.Count == 0)
                throw new ArgumentNullException("Resources cannot be empty.");

            List<string> securityAssetHash = resources.Where(x => x.SecurityAsset == "G" || x.SecurityAsset == "R" || x.SecurityAsset == "O").Select(x => x.SecurityAsset + x.SecurityAssetId).ToList();

            var overrides = Company.ResponsibilityTypeRelationOverrideItems
                .Where(x => x.ResponsibilityTypeID == responsibilityType.ID
                && x.AssetID == asset.ID
                && securityAssetHash.Contains(x.SecurityAsset + x.SecurityAssetID));

            Company.ResponsibilityTypeRelationOverrideItems.RemoveRange(overrides);
            Company.SaveChanges();

            sendAssetMeasureQueueForOverrides(responsibilityType, asset);
        }

        public List<ResponsibilityRuleUpsertResponseModel> UpsertResponsibilityRules(Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> responsibilityRules, ApiExecution execution)
        {
            Company.Add(execution);

            List<ResponsibilityRuleUpsertResponseModel> results = null;
            try
            {
                results = Company.UpsertResponsibilityRules(execution, responsibilityTypeUid, responsibilityRules);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public async Task<IReadOnlyList<ResponsibilityRuleDeleteResponse>> DeleteResponsibilityRulesAsync(Guid responsibilityTypeUid, IReadOnlyList<Guid> rulesForDeletion)
        {
            if (Company.Connection.State != ConnectionState.Open)
                Company.Connection.Open();

            List<ResponsibilityRuleDeleteResponse> returnResults = null;

            using (var trans = Company.Connection.BeginTransaction("DeleteResponsibilityRules")) 
            {
                try
                {
                    // Setup and initial validation.
                    await Company.Connection.ExecuteAsync(@"
create table #results
(
    Uid uniqueidentifier, 
    Message nvarchar(max),
    Success bit
);

create table #measureResults
(
    AssetUid uniqueidentifier, 
    AllocationUid uniqueidentifier, 
    MetricAssetUid uniqueidentifier, 
    MetricAssetVersionUid uniqueidentifier 
);", transaction: trans);

                    // Setup and initial validation.
                    await Company.Connection.ExecuteAsync(@"
insert into #results (Uid)
    select cast(value as uniqueidentifier) from string_split(@rulesUids,',');

update  #results
set     Message = 'Responsibility rule does not exist.',
        Success = 0 
from    #results dr
        left join responsibilitytyperelationrule rtrr on rtrr.uid = dr.uid
where   rtrr.id is null;

update  #results
set     Message = 'Responsibility rule not valid for Responsibility Type.',
        Success = 0 
from    #results dr
        left join responsibilitytyperelationrule rtrr on rtrr.uid = dr.uid
        left join responsibilitytype rt on rt.uid = @responsibilityTypeUid
where   rtrr.responsibilitytypeid <> rt.id;", 
                        new { responsibilityTypeUid, rulesUids = string.Join(",", rulesForDeletion.Select(x => x.ToString())) },
                        transaction: trans
                    );

                    // Now load the asset/measure combinations that will be impacted by these deletions.
                    await Company.Connection.ExecuteAsync(@"
insert into #measureResults
    select  A.AssetUid, 
            M.AllocationUid,
            M.Uid as MetricAssetUid,
            V.Uid as MetricAssetVersionUid
    from    #results Ru
            inner join ResponsibilityTypeRelationRule R on R.Uid = Ru.Uid and Ru.Success is null
            cross apply (
                        select  A.Uid as AssetUid, T.Uid as AssetTypeUid
                        from    Asset A inner join AssetType T on T.ID = A.AssetTypeID 
                                inner join ResponsibilityRuleResultAsset RA on RA.RuleID = R.ID and RA.AssetID = A.ID and RA.AssetTypeID = 0
                        union 
                        select  A.Uid as AssetUid, T.Uid as AssetTypeUid
                        from    Asset A inner join AssetType T on T.ID = A.AssetTypeID 
                                inner join ResponsibilityRuleResultAsset RA on RA.RuleID = R.ID and RA.AssetTypeID = T.ID and RA.AssetTypeID <> 0
                        ) A 
            inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
            inner join metrics.Allocation Al on Al.AssetTypeUid = A.AssetTypeUid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
            inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
            inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
                and ( 
                    (@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
                    (@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
                    ) 
                and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
                and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
		        and V.Definition <> '{}'", new { today = DateTime.UtcNow.Date }, transaction: trans);

                    // Perform deletes on impacted tables and save results to temporary table.
                    await Company.Connection.ExecuteAsync(@"
delete T from ResponsibilityRuleResultSecurityAsset T inner join ResponsibilityTypeRelationRule R on R.ID = T.RuleID inner join #results D on D.Uid = R.Uid and D.Success is null;
delete T from ResponsibilityRuleResultAsset T inner join ResponsibilityTypeRelationRule R on R.ID = T.RuleID inner join #results D on D.Uid = R.Uid and D.Success is null;
delete T from ResponsibilityTypeRelationRule T inner join #results D on D.Uid = T.Uid and D.Success is null

update  #results
set     Message = 'Responsibility rule successfully deleted.',
        Success = 1
where   Success is null", transaction: trans);

                    var queryResults = await Company.Connection.QueryMultipleAsync(@"select * from #results; select * from #measureResults", transaction: trans);

                    returnResults = queryResults.Read<ResponsibilityRuleDeleteResponse>().ToList();
                    var measureResults = queryResults.Read<ResponsibilityAssetMeasureProcessedResult>();
                    var today = DateTime.UtcNow.Date;
                    var structuredMeasures = measureResults.GroupBy(m => new { m.AssetUid })
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

                    trans.Commit();

                    Company.CreateMeasureChangedResultExecution(structuredMeasures);
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
                    }
                }   
            }
            
            return returnResults;
        }

        public async Task<ApiExecutionInfo> PostBatchResponsibilityOverride(List<BulkResponsibilityOverridePostModel> models, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = Company.CurrentCompanyID,
                CompanyDomainPrefix = Company.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.PostResponsibilityOverride
            };

            return await CreateApiBatchJob(executionInfo, execution, models, StorageProvider, QueueSource).ConfigureAwait(false);
        }

        public async Task<ResponsibilityRuleTestResponseModel> GetResponsibilityRuleTestResults(ResponsibilityRuleUpsertModel test, bool hideD3SUsers, bool includeThen, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int pageSize, pageNum;
            bool includeTotal;
            string direction;

            string errorMessage = null;
            ResponsibilityTypeRelationRule testModel = new ResponsibilityTypeRelationRule();
            var executionId = Guid.NewGuid();
            var sourceTable = "#ResponsibilityRuleTest";

            #region Parse Query Params

            string queryValue;

            queryValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value ?? "asc";
            direction = queryValue;
            
            queryValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value ?? "false";
            bool.TryParse(queryValue, out includeTotal);

            queryValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value ?? "200";
            int.TryParse(queryValue, out pageSize);

            queryValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value ?? "1";
            int.TryParse(queryValue, out pageNum);

            #endregion

            if (Company.Connection.State != ConnectionState.Open)
                Company.Connection.Open();

            using (var trans = Company.Connection.BeginTransaction())
            {
                try
                {
                    Company.Connection.Execute($@"drop table if exists {sourceTable}
                    create table {sourceTable}
                    (
	                    ExecutionID uniqueidentifier,
	                    ItemNumber int,
	                    AssetTypeUid uniqueidentifier,
	                    [Definition] nvarchar(max),
	                    DefinitionConverted nvarchar(max),
	                    [Message] nvarchar(2500),
	                    Success bit
                    )"
                    , transaction: trans, commandTimeout: 3600);

                    Company.Connection.Execute($@"insert into {sourceTable} (ExecutionID, ItemNumber, AssetTypeUid, [Definition]) values (@executionId, 1, @AssetTypeUid, @Definition)"
                   , new { executionId, test.AssetTypeUid, Definition = JsonConvert.SerializeObject(test.Definition) }, transaction: trans, commandTimeout: 3600);

                    Company.ParseResponsibilityRuleModel(executionId, trans, 3600, sourceTable);

                    var result = Company.Connection.QueryFirst<dynamic>($"select s.*, t.Object, t.ObjectID from {sourceTable} s left join AssetType t on t.uid = s.AssetTypeUid", transaction: trans);
                    errorMessage = result.Success == false ? result.Message : null;
                    
                    testModel.ApplyToType = test.ApplyToType;
                    testModel.Object = result.Object;
                    testModel.ObjectID = result.ObjectID;
                    testModel.StructuredDefinition = JsonConvert.DeserializeObject<ResponsibilityRuleDefinition>(result.DefinitionConverted);
                    testModel.SetRawFromDefinition();
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
                }
            };

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return new ResponsibilityRuleTestResponseModel
                {
                    Success = false,
                    Message = errorMessage
                };
            }

            int? total = null;
            string resultsSql;
            var orderSql = $" order by [path] {direction} ";
            var pagingSql = " OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY ";

            if (includeThen)
            {
                resultsSql = Company.GetThenResultsSql(testModel, hideD3SUsers, null);
                resultsSql = resultsSql.Replace(" {0} ", "");
            }
            else
            {
                resultsSql = await Company.GetWhenResultsSql(testModel, null);
            }

            if (string.IsNullOrWhiteSpace(resultsSql))
            {
                return new ResponsibilityRuleTestResponseModel
                {
                    pageNum = pageNum,
                    pageSize = pageSize,
                    total = includeTotal ? 0 : (int?)null,
                    items = new List<ResponsibilityRuleTestResultModel>()
                };
            }

            if (includeTotal)
            {
                total = await Company.QueryFirstOrDefaultAsync<int>($"select count(*) from ({resultsSql})x");
            }
            
            var items = await Company.QueryAsync<ResponsibilityRuleTestResultModel>(resultsSql + orderSql + pagingSql, new { offset = (pageSize * (pageNum - 1)), rows = pageSize });

            return new ResponsibilityRuleTestResponseModel
            {
                pageNum = pageNum,
                pageSize = pageSize,
                total = total,
                items = items
            };
        }
    }
}
