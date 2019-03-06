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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<ResponsibilityTypeViewModel>))
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<ResponsibilityTypeViewModel>))
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<ResponsibilityTypeViewModel>))
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<ResponsibilityTypeRuleViewModel>))
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
	                            rtr.[name]
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

    }
}
