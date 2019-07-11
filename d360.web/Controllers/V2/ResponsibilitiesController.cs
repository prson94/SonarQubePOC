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
using d360.model.DataAccessLayer;

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
    public class ResponsibilitiesController : BaseV2ApiController
    {
        IResponsibilityRepository ResponsibilityRepository;
        public ResponsibilitiesController(ICommunityContext community, ICompanyContext company, IResponsibilityRepository responsibilityRepository)
            : base(community, company)
        {
            this.ResponsibilityRepository = responsibilityRepository;
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
                IEnumerable<ResponsibilityTypeViewModel> responsibilityTypes = await ResponsibilityRepository.GetResponsibilityTypes();

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
                IEnumerable<ResponsibilityTypeViewModel> responsibilityTypes = await ResponsibilityRepository.GetResponsibilityTypesByAssetUid(assetTypeUid);

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
                IEnumerable<ResponsibilityTypeAllocationViewModel> responsibilityTypeAllocations = await ResponsibilityRepository.GetResponsibilityTypeAllocations(responsibilityTypeUid);

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
                IEnumerable<ResponsibilityTypeRuleViewModel> responsibilityTypeRules = await ResponsibilityRepository.GetResponsibilityRules(responsibilityTypeUid);

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
                ResponsibilityTypeRuleStatsViewModel responsibilityTypeRuleStats = await ResponsibilityRepository.GetResponsibilityRuleStats(responsibilityTypeRuleUid);

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
        /// Retrieves a list of assets with ownership based on the provided parameters.  Assets and ownership results reflect the users permissions to see the assets and the ownership details for them.  If a user doesnt have access to see an asset then they will not be able to see the asset or its ownership.  If a user does have access to see an asset but doesn't have access to see the assets ownership, the asset will be returned without any ownership details.  No filters applied will return all items which have at least one owner.  Only assets with ownership are returned by this API.  By default 5 assets are returned at a time the max page size is 250 assets.  Please keep in mind that assets with lots of owners will impact response time / size.
        /// </summary>   
        /// <permission cref="">Admin or Ownership read required</permission>
        /// <returns>Returns a list of assets and there corresponding ownership information.</returns>
        [
            HttpGet,
            Route("assignments"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(AssetResponsibilityItemModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 assets per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The Uid of a asset to return ownership for. If specified the results will include ownership of this asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "The Uid of a asset type to return ownership for. If specified the results will include ownership of this asset type only.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_responsibilityTypeUid", "The Uid of a responsibility type to return ownership for. If specified the results will include ownership of assets that include this responsibility type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assigneeUid", "The Uid of an assignee to return ownership for. If specified the results will include assets for which the specified user is an owner.  In order to use this filter you must specify in addition the _assetTypeUid or _assetUid filter as well.", DataType = "string", ParameterType = "query", Required = false),            
        ]
        public async Task<HttpResponseMessage> GetResponsibilities()
        {
            var prefix = "Responsibilities.GetResponsibilities => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();


                var responsibilityUidFilter = "";
                var assigneeUidFilter = "";
                var assetUidFilter = "";
                var assetTypeUidFilter = "";
                var pageSize = 5;
                var pageNum = -1;
                var timeout = 300;


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

                //validation dont allow assigneeuid filter across entire universe

                if (!string.IsNullOrEmpty(assigneeUidFilter) && string.IsNullOrEmpty(assetTypeUidFilter) && string.IsNullOrEmpty(assetUidFilter))
                {
                    return ReturnApiError(HttpStatusCode.InternalServerError, "In order to use the _assigneeuid filter the _assetTypeUid or _assetUid filter must also be specified.");
                }

                AssetResponsibilitiesApiModel res = await ResponsibilityRepository.GetResponsibilities(queryParams, responsibilityUidFilter, assigneeUidFilter, assetUidFilter, assetTypeUidFilter, pageSize, pageNum, timeout);

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

      }
}