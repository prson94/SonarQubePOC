using d360.core.entities;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Dapper;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/responsibilities"),
        Authorize
    ]
    public class ResponsibilitiesController : BaseApiController
    {
        public ResponsibilitiesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {            
        }

        /// <summary>
        /// Retrieves a list of all responsibility types.
        /// </summary>
        /// <returns>Returns a list of responsibility types.</returns>
        [
            HttpGet,
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility types.", typeof(List<ResponsibilityTypeViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetResponsibilityTypesAsync()
        {
            var prefix = "Responsibilities.GetResponsibilityTypesAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var responsibilityTypes = await Company.QueryAsync<ResponsibilityTypeViewModel>(@"
                            select [Name], [Description], [uid], [UpdatedOn] from [dbo].[responsibilitytype] order by [Name] asc
                            ");

                return Request.CreateResponse(HttpStatusCode.OK, responsibilityTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves a list of responsibility types that are applicable for the specified AssetTypeUid.
        /// </summary>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <returns>Returns a list of responsibility types.</returns>
        [
            HttpGet,
            Route("types/{assetTypeUid:guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility types.", typeof(List<ResponsibilityTypeViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetResponsibilityTypesByAssetTypeAsync(Guid assetTypeUid)
        {
            var prefix = "Responsibilities.GetResponsibilityTypesAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var responsibilityTypes = await Company.QueryAsync<ResponsibilityTypeViewModel>(@"
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

                return Request.CreateResponse(HttpStatusCode.OK, responsibilityTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves a list of all allocations for the specified responsibility type.
        /// </summary>
        /// <param name="responsibilityTypeUid">The unique identifier of the responsibility type to get allocations for.</param>
        /// <returns>Returns a list of asset types a responsibility rule is allocated to.</returns>
        [
            HttpGet,
            Route("types/{responsibilityTypeUid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type allocations for the given responsibility type uid.", typeof(List<ResponsibilityTypeAllocationViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetResponsibilityTypeAllocationsAsync(Guid responsibilityTypeUid)
        {
            var prefix = "Responsibilities.GetResponsibilityTypeAllocationsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var responsibilityTypeAllocations = await Company.QueryAsync<ResponsibilityTypeAllocationViewModel>(@"
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

                return Request.CreateResponse(HttpStatusCode.OK, responsibilityTypeAllocations);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves a list of responsibility type ownership rules for the specified responsibility type.
        /// </summary>
        /// <param name="responsibilityTypeUid">The unique identifier of the responsibility type to get responsibility type ownership rules for.</param>
        /// <returns>Returns a list of responsibility type ownership rules.</returns>
        [
            HttpGet,
            Route("types/{responsibilityTypeUid:Guid}/ownershiprules"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility type ownership rules for the given responsibility type uid.", typeof(List<ResponsibilityTypeRuleViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetResponsibilityRulesForTypeAsync(Guid responsibilityTypeUid)
        {
            var prefix = "Responsibilities.GetResponsibilityRulesForTypeAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var responsibilityTypeRules = await Company.QueryAsync<ResponsibilityTypeRuleViewModel>(@"
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

                return Request.CreateResponse(HttpStatusCode.OK, responsibilityTypeRules);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }


        /// <summary>
        /// Retrieves a list of responsibility type ownership rules for the specified responsibility type.  Rules applied to groups and organizations are enumerated to the actual count of users contained therein.  Rules applying to a type are enumerated down to the count of assets within the given type.
        /// </summary>
        /// <param name="responsibilityTypeRuleUid">The unique identifier of the responsibility type ownership rule to get stats for.</param>
        /// <returns>Returns a stats for the specified responsibility type ownership rules.</returns>
        [
            HttpGet,
            Route("rules/{responsibilityTypeRuleUid:Guid}/stats"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(ResponsibilityTypeRuleStatsViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetResponsibilityRulesStats(Guid responsibilityTypeRuleUid)
        {
            var prefix = "Responsibilities.GetResponsibilityRulesStats => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
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

                return Request.CreateResponse(HttpStatusCode.OK, responsibilityTypeRuleStats);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }


        /// <summary>
        /// Retrieves a list of responsibility ownership of assets based on the provided parameters.  Assets and ownership results reflect the users permissions to see the assets and the ownership details for them.  No filters applied will return all items which have at least one owner.
        /// </summary>        
        /// <returns>Returns a list of assets and there corresponding ownership information.</returns>
        [
            HttpGet,
            Route("assignments"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(AssetResponsibilityItemModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The Uid of a asset to return ownership for. If specified the results will include ownership of this asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "The Uid of a asset type to return ownership for. If specified the results will include ownership of this asset type only.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_responsibilityTypeUid", "The Uid of a responsibility type to return ownership for. If specified the results will include ownership of assets that include this responsibility type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assigneeUid", "The Uid of an assignee to return ownership for. If specified the results will include assets for which the specified user is an owner.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> GetResponsibilities()
        {
            var prefix = "Responsibilities.GetResponsibilities => ";
            var errorMessage = "";

            try
            {
                var responsibilityUidFilter="";
                var assigneeUidFilter="";
                var assetUidFilter = "";
                var assetTypeUidFilter = "";
                var pageSize = 250;
                var pageNum = -1;
                var timeout = 300;

                var queryParams = Request.GetQueryNameValuePairs();

                queryParams.ToList().ForEach(q =>
                {
                    var key = q.Key.ToLower();

                    if (key.StartsWith("_"))
                    {
                        switch (key)
                        {
                            case "_pagesize":
                                if (int.TryParse(q.Value, out pageSize))
                                {
                                    if (pageSize < 1) pageSize = 1;
                                }
                                if (pageSize > 250) pageSize = 250; // max page size is 250 people.
                                break;
                            case "_pagenum":
                                if (int.TryParse(q.Value, out pageNum))
                                {
                                    if (pageNum < 1) pageNum = 1;
                                }
                                break;
                            case "_responsibilitytypeuid":
                                responsibilityUidFilter = q.Value;
                                break;
                            case "_assigneeuid":
                                assigneeUidFilter = q.Value;
                                break;
                            case "_assettypeuid":
                                assetTypeUidFilter = q.Value;
                                break;
                            case "_assetuid":
                                assetUidFilter = q.Value;
                                break;
                            case "_timeout":
                                if (int.TryParse(q.Value, out timeout))
                                {
                                    if (timeout < 1) timeout = 30; // min timeout
                                }
                                break;
                        }
                    }
                });

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
                    if(assetDictionary.TryGetValue(responsibility.AssetID, out model))
                    {
                        if (model.Responsibilities == null) model.Responsibilities = new List<ResponsibilityApiModel>();

                        model.Responsibilities.Add(responsibility);
                    }                    
                }
            
                return Request.CreateResponse(HttpStatusCode.OK, res);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
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
                        s.[uid] as 'AssigneeUid'
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
                        s.[uid] as 'AssigneeUid'
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
                        a.[uid] as 'AssigneeUid'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationOverrideItem] oride on oride.ResponsibilityTypeID = rt.id	
                        inner join [dbo].[asset] a on oride.securityassetid = a.objectid and a.[object] = 'Resource'
                    where
	                    oride.assetid in @assetIds {responsibilityFilterCriteria} {overrideAssigneeFilterCriteria} {permissionsCriteria}";


            return (await Company.Database.Connection.QueryAsync<ResponsibilityApiModel>(sql, dbArgs, null, timeout));

        }

        private async Task<AssetResponsibilitiesApiModel> getOwnershipAssets(IEnumerable<KeyValuePair<string, string>> queryParams, string assetUid, string assetTypeUid, string responsibilityUidFilter, string assigneeUidFilter,  int pageSize, int pageNum, int timeout = 300 )
        {
            var res = new AssetResponsibilitiesApiModel();
            DynamicParameters dbArgs = new DynamicParameters();
            var orderBySql = "order by A.ID";
            var offsetSql = "";            
            var assetQueryFilterSql = "";
            var responsibilityQueryFilterSql = "";
            var responsibilityQueryAdditionalJoins = "";
            var responsibilityOverrideQueryAdditionalJoins = "";
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
                responsibilityOverrideQueryAdditionalJoins = " cross apply [dbo].[GetSecurityAssetUid](rd.SecurityAsset,rd.SecurityAssetId) s ";
                responsibilityQueryAdditionalJoins = " inner join ResponsibilityRuleResultSecurityAsset rsa on (rsa.ruleid = rr.id) cross apply [dbo].[GetSecurityAssetUid](rsa.SecurityAsset,rsa.SecurityAssetId) s ";
                responsibilityQueryFilters.Add($"s.uid = @assigneeUid");
                dbArgs.Add("@assigneeUid", assigneeUidFilter);
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
						                    exists (select 1 from ResponsibilityTypeRelationOverrideItem rd inner join ResponsibilityType rt on(rd.ResponsibilityTypeID = rt.id) {responsibilityOverrideQueryAdditionalJoins} where rd.AssetID = a.ID {responsibilityQueryFilterSql} )
							                    or
						                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) {responsibilityQueryAdditionalJoins} where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql} )		
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
                    select
	                    a.id as AssetId,
	                    a.uid as AssetUid,
	                    att.uid as AssetTypeUid,
	                    att.name as AssetTypeName
                    from 
	                   asset a
	                   inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    (exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) {responsibilityQueryAdditionalJoins} where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql} )		
						    or 
                        exists (select 1 from ResponsibilityTypeRelationOverrideItem rd inner join ResponsibilityType rt on(rd.ResponsibilityTypeID = rt.id) {responsibilityOverrideQueryAdditionalJoins} where rd.AssetID = a.ID {responsibilityQueryFilterSql} )
						    or
						exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) {responsibilityQueryAdditionalJoins} where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql} )		
						)
                        {assetQueryFilterSql}
                        {permissionsFilter}   	                    
                        {orderBySql} {offsetSql} 
                    ";

                res.items = (await Company.Database.Connection.QueryAsync<AssetResponsibilityItemModel>(sql, dbArgs, null, timeout)).ToList();

            }
            return res;
        }
    }
}
