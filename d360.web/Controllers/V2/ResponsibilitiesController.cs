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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
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
        /// Retrieves a list of responsibility ownership of assets based on the provided parameters.  Administrators can see all ownership.  Regular users ownership results are filtered based on items they can see the ownership for.
        /// </summary>        
        /// <returns>Returns a list of assets and there corresponding ownership information.</returns>
        [
            HttpGet,
            Route("assignments"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(AssetResponsibilityItemModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The Uid of a asset to return ownership for. If specified the results will include ownership of this asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "The Uid of a asset type to return ownership for. If specified the results will include ownership of this asset type only.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_responsibilityTypeUid", "The Uid of a responsibility type to return ownership for. If specified the results will include ownership of assets that include this responsibility type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assigneeUid", "The Uid of an assignee to return ownership for. If specified the results will include ownership of this assignee only.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> GetResponsibilities()
        {
            var prefix = "Responsibilities.GetResponsibilities => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                //get the assetids based on the input parameters
                var res = await getOwnershipAssets(queryParams);

                var assetIDList = res.items.Select(x => x.AssetID);
                //get the responsibilities that apply to these assets this should be for <= 250 asset ids only
                var responsibilities = await getOwnershipForGivenAssets(assetIDList);
            
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

        private async Task<IEnumerable<dynamic>> getOwnershipForGivenAssets(IEnumerable<long> assetIDList, int timeout = 300)
        {
            if (assetIDList == null) return null;

            var sql = @"select 
                        a.assetid,
	                    'rule' as 'AssigneeMethod',
	                    rsa.SecurityAsset,
	                    rsa.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    1 as 'AssignedToType'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationRule] rr on rr.ResponsibilityTypeID = rt.id
	                    inner join [dbo].[ResponsibilityTypeRelation] rtr on rtr.ObjectID = rr.ObjectID and rtr.ObjectType = rr.[Object]
	                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on rsa.RuleID = rr.id
	                    inner join [dbo].[assettype] att on att.[object] = rr.[object] and att.objectid = rr.objectid
	                    inner join [dbo].asset a on a.AssetTypeID = att.id
                    where
	                    rr.applytotype = 1 and a.assetid in @assetIds
                    union
                    select 
                        a.assetid,
	                    'rule' as 'AssigneeMethod',
	                    rsa.SecurityAsset,
	                    rsa.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    0 as 'AssignedToType'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationRule] rr on rr.ResponsibilityTypeID = rt.id
	                    inner join [dbo].[ResponsibilityTypeRelation] rtr on rtr.ObjectID = rr.ObjectID and rtr.ObjectType = rr.[Object]
	                    inner join [dbo].[ResponsibilityRuleResultSecurityAsset] rsa on rsa.RuleID = rr.id
	                    inner join [dbo].[ResponsibilityRuleResultAsset] ra on ra.RuleID = rr.id		
                    where
	                    rr.applytotype = 0 and ra.assetid in @assetIds
                    union
                    select 
                        a.assetid,
	                    'direct' as 'AssigneeMethod',
	                    oride.SecurityAsset,
	                    oride.SecurityAssetID,
	                    rt.[uid] as 'ResponsibilityTypeUid',
	                    rt.[name] as 'ResponsibilityTypeName',
	                    0 as 'AssignedToType'
                    from
	                    [dbo].[ResponsibilityType] rt
	                    inner join [dbo].[ResponsibilityTypeRelationOverrideItem] oride on oride.ResponsibilityTypeID = rt.id	
                    where
	                    oride.assetid in @assetIds";


            var responsibilities = (await Company.Database.Connection.QueryAsync<AssetResponsibilityItemModel>(sql, new { assetIds = assetIDList }, null, timeout)).ToList();

            return null;
        }

        private async Task<AssetResponsibilitiesApiModel> getOwnershipAssets(IEnumerable<KeyValuePair<string, string>> queryParams, int timeout = 300 )
        {
            var res = new AssetResponsibilitiesApiModel();
            DynamicParameters dbArgs = new DynamicParameters();
            var orderBySql = "order by A.ID";
            var offsetSql = "";
            var pageNum = -1;
            var pageSize = 250;
            var assetQueryFilterSql = "";
            var responsibilityQueryFilterSql = "";
            List<string> assetQueryFilters = new List<string>();
            List<string> responsibilityQueryFilters = new List<string>();


            queryParams.ToList().ForEach(q =>
                    {
                        var key = q.Key.ToLower();

                        if (key.StartsWith("_"))
                        {
                            switch (key)
                            {
                                case "_pagesize":
                                    break;
                                case "_pagenum":
                                    break;
                                case "_assetuid":
                                    assetQueryFilters.Add($"a.uid = @assetUid");
                                    dbArgs.Add($"@assetUid", q.Value);
                                    break;
                                case "_assettypeuid":
                                    assetQueryFilters.Add($"att.uid = @assettypeUid");
                                    dbArgs.Add($"@assettypeUid", q.Value);
                                    break;
                                case "_responsibilityTypeUid":
                                    responsibilityQueryFilters.Add($"rr.uid = @respUid");
                                    dbArgs.Add($"@respUid", q.Value);
                                    break;
                                default:
                                    break;
                            }
                        }
                    });

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

            var countSql = $@"select sum(A.cnt) from (
                    select
	                    count(1) as cnt
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql})		
                        {assetQueryFilterSql}
                    union
                    select
	                    count(1) as cnt
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) where rd.assettypeid = a.assettypeid and rr.applytotype = 1  {responsibilityQueryFilterSql})
                        {assetQueryFilterSql}
                    union
                    select
	                    count(1) as cnt
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityTypeRelationOverrideItem oride inner join ResponsibilityType rt on(oride.ResponsibilityTypeID = rt.id) where oride.AssetID = a.ID {responsibilityQueryFilterSql})
                        {assetQueryFilterSql}
	                    )   A                   
            ";
            
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
                    (select
	                    a.id as AssetId,
	                    a.uid as AssetUid,
	                    att.uid as AssetTypeUid,
	                    att.name as AssetTypeName
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) where rd.assetid = a.id and rr.applytotype = 0 {responsibilityQueryFilterSql})		
                        {assetQueryFilterSql}
                    union
                    select
	                    a.id as AssetId,
	                    a.uid as AssetUid,
	                    att.uid as AssetTypeUid,
	                    att.name as AssetTypeName
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityRuleResultAsset rd inner join ResponsibilityTypeRelationRule rr on (rr.id = rd.RuleID) inner join ResponsibilityType rt on (rr.responsibilitytypeid = rt.id) where rd.assettypeid = a.assettypeid and rr.applytotype = 1  {responsibilityQueryFilterSql})
                        {assetQueryFilterSql}
                    union
                    select
	                    a.id as AssetId,
	                    a.uid as AssetUid,
	                    att.uid as AssetTypeUid,
	                    att.name as AssetTypeName
                    from 
	                    asset a
	                    inner join assetType att on a.AssetTypeID = att.id
                    where 
	                    exists (select 1 from ResponsibilityTypeRelationOverrideItem oride inner join ResponsibilityType rt on(oride.ResponsibilityTypeID = rt.id) where oride.AssetID = a.ID {responsibilityQueryFilterSql})
                        {assetQueryFilterSql}
	                    )  
                    {orderBySql} {offsetSql}
            ";

                res.items = (await Company.Database.Connection.QueryAsync<AssetResponsibilityItemModel>(sql, dbArgs, null, timeout)).ToList();

            }
            return res;
        }
    }
}
