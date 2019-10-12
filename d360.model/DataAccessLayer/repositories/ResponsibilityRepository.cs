using d360.core;
using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ResponsibilityRepository : IResponsibilityRepository
    {
        ICompanyContext Company;
        public ResponsibilityRepository(ICompanyContext companyContext)
        {
            this.Company = companyContext;
        }

        public async Task<AssetResponsibilitiesApiModel> GetResponsibilities(IEnumerable<KeyValuePair<string, string>> queryParams, string responsibilityUidFilter, string assigneeUidFilter, string assetUidFilter, string assetTypeUidFilter, int pageSize, int pageNum, int timeout)
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
                            ", new { uid = responsibilityTypeRuleUid.ToString() });

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
                            ", new { uid = responsibilityTypeRuleUid.ToString() });
            return responsibilityTypeRuleStats;
        }

        public async Task<IEnumerable<ResponsibilityTypeRuleViewModel>> GetResponsibilityRules(Guid responsibilityTypeUid)
        {
            return await Company.QueryAsync<ResponsibilityTypeRuleViewModel>(@"
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
                            ", new { uid = responsibilityTypeUid.ToString() });
        }

        public async Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocations(Guid responsibilityTypeUid)
        {
            return await Company.QueryAsync<ResponsibilityTypeAllocationViewModel>(@"
                            select 
	                            att.Class as AssetClass,
	                            att.[Name] as AssetTypeName,
	                            att.[uid] as AssetTypeUid,
	                            rtr.PermissionsBitMask as PermissionsMask
                            from 
	                            [dbo].responsibilitytype rt
	                            inner join [dbo].responsibilitytyperelation rtr on (rt.id = rtr.ResponsibilityTypeID)
	                            inner join [dbo].assettype att on(att.[Object] = rtr.ObjectType and att.ObjectID = rtr.ObjectID)
                            where
	                            rt.[uid] = @uid
                            ", new { uid = responsibilityTypeUid.ToString() });
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
                            ", new { uid = assetTypeUid });
        }

        public async Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypes()
        {
            return await Company.QueryAsync<ResponsibilityTypeViewModel>(@"
                            select [Name], [Description], [uid], [UpdatedOn] from [dbo].[responsibilitytype] order by [Name] asc
                            ");
        }

        private async Task<IEnumerable<ResponsibilityApiModel>> getOwnershipForGivenAssets(IEnumerable<long> assetIDList, string responsibilityUidFilter, string assigneeUidFilter, int timeout = 300)
        {
            if (assetIDList == null) return null;
            var responsibilityFilterCriteria = "";
            var assigneeFilterCriteria = "";
            var overrideAssigneeFilterCriteria = "";
            var permissionsCriteria = "";
            DynamicParameters dbArgs = new DynamicParameters();

            dbArgs.Add("assetIds", assetIDList);

            if (!string.IsNullOrEmpty(assigneeUidFilter))
            {
                assigneeFilterCriteria = $" and s.[uid] = @assigneeUidFilter";
                overrideAssigneeFilterCriteria = $" and a.[uid] = @assigneeUidFilter";
                dbArgs.Add("assigneeUidFilter", assigneeUidFilter);
            }

            if (!string.IsNullOrEmpty(responsibilityUidFilter))
            {
                responsibilityFilterCriteria = $" and rt.[uid] = @responsibilityTypeUid";
                dbArgs.Add("responsibilityTypeUid", responsibilityUidFilter);
            }

            if (!Company.CurrentResourceIsAdmin)
            {
                permissionsCriteria = $" and exists(select 1 from UserAssetPermissions(@r,a.AssetTypeID) u where u.PermissionsBitMask & 64 = 64 and (u.AssetID = a.ID or (u.AssetID = 0 and u.AssetTypeID = a.AssetTypeID)))";
                dbArgs.Add("r", Company.CurrentResourceID);
            }

            var sql = $@"select 
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
                        cross apply [dbo].[GetSecurityAssetUid](rsa.SecurityAsset,rsa.SecurityAssetID) s
                    where
	                    rr.applytotype = 1 and a.id in @assetIds
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
                        inner join [dbo].[asset] a on ra.assetid = a.id
                        cross apply [dbo].[GetSecurityAssetUid](rsa.SecurityAsset,rsa.SecurityAssetID) s
                    where
	                    rr.applytotype = 0 and ra.assetid in @assetIds
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
                        inner join [dbo].[asset] a on a.id = oride.assetid
                        cross apply [dbo].[GetSecurityAssetUid](oride.SecurityAsset,oride.SecurityAssetID) s                        
                    where
	                    oride.assetid in @assetIds {responsibilityFilterCriteria} {overrideAssigneeFilterCriteria} {permissionsCriteria}";

            return (await Company.Database.Connection.QueryAsync<ResponsibilityApiModel>(sql, dbArgs, null, timeout));
        }

        private async Task<AssetResponsibilitiesApiModel> getOwnershipAssets(IEnumerable<KeyValuePair<string, string>> queryParams, string assetUid, string assetTypeUid, string responsibilityUidFilter, string assigneeUidFilter, int pageSize, int pageNum, int timeout = 300)
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

            if (!string.IsNullOrEmpty(assetUid))
            {
                assetQueryFilters.Add($"a.uid = @assetUid");
                dbArgs.Add("@assetUid", assetUid);
            }

            if (!string.IsNullOrEmpty(assetTypeUid))
            {
                assetQueryFilters.Add($"att.uid = @assettypeUid");
                dbArgs.Add("@assettypeUid", assetTypeUid);
            }

            if (!string.IsNullOrEmpty(responsibilityUidFilter))
            {
                responsibilityQueryFilters.Add($"rt.uid = @respUid");
                dbArgs.Add("@respUid", responsibilityUidFilter);
            }

            if (!string.IsNullOrEmpty(assigneeUidFilter))
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
                execution.ErrorMessage = ex.GetFullExceptionData(false);
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

            return result;
        }

    }
}
