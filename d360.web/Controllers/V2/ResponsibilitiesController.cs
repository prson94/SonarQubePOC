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
using Resources;
using System.Diagnostics;
using System.Web.Http.Description;
using d360.core.enums;

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
        /// Retrieves a responsibility type.
        /// </summary>
        /// <returns>Returns a responsibility type.</returns>
        [
            HttpGet,
            Route("type/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A responsibility type.", typeof(List<ResponsibilityTypeViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetResponsibilityTypeAsync(Guid uid)
        {
            var prefix = "Responsibilities.GetResponsibilityTypesAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                dynamic responsibilityTypes = await ResponsibilityRepository.GetResponsibilityType(uid);

                return Request.CreateResponse(HttpStatusCode.OK, new { data = responsibilityTypes });
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
        /// Get a list of all claims that are available for assignment.
        /// </summary>
        /// <returns>Returns a list of claims for assignment</returns>
        [
            HttpGet,
            Route("claims"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of claims for assignment.", typeof(List<ClaimsViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetClaimsAsync()
        {
            var prefix = "Responsibilities.GetClaimsAsync => ";
            var errorMessage = "";

            
            try
            {
                var claims = await ResponsibilityRepository.GetClaims();
                return Request.CreateResponse(HttpStatusCode.OK, claims);
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
        /// Adds a list of all allocations for the specified Asset.
        /// </summary>
        /// <param name="uid">The Uid of the Responsibilty type.</param>
        /// <param name="model">A list of AssetTypeUid and Permissions to add allocations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostResponsibilityTypeAllocationsAsync(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
        {
            var prefix = "Responsibilities.PostResponsibilityTypeAllocationsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));


            try
            {
                List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();
                
                //valdiate the responsibilitytype uid passed in
                ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();
                if(responsibility == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid ResponsibilityType uid for this request."));

                foreach (var allocation in model)
                {
                    AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();
                    if (assetType == null)
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeUid uid privided.",
                            Success = false
                        });
                        continue;
                    }
                    List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
                    {
                        AssetTypeClass.BusinessAsset,
                        AssetTypeClass.TechnicalAsset,
                        AssetTypeClass.FusionAttribute,
                        AssetTypeClass.Model,
                        AssetTypeClass.Rule,
                        AssetTypeClass.Policy,
                        AssetTypeClass.ReferenceItemType
                    };
                    if (!allowedClasses.Contains(assetType.Class)) 
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeClass. [{assetType.Class.ToString()}] is not valid.",
                            Success = false
                        });
                        continue;
                    } 

                    var validValues = Permission.DeleteAsset.GetList().Select(x => x.Value);
                    if (allocation.Permissions.Any(x => !validValues.Contains(x)))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid Permission privided. [{string.Join(",",allocation.Permissions.Where(x => !validValues.Contains(x)).ToArray())}]",
                            Success = false
                        });
                        continue;
                    }

                    if (Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Allocation already exists.",
                            Success = false
                        });
                        continue;
                    }

                    results.Add(ResponsibilityRepository.AddAllocation(responsibility, assetType, allocation.Permissions));
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Edits a list of all allocations for the specified Asset.
        /// </summary>
        /// <param name="uid">The Uid of the Responsibilty type.</param>
        /// <param name="model">A list of AssetTypeUid and Permissions to edits allocations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to edit responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutResponsibilityTypeAllocationsAsync(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
        {
            var prefix = "Responsibilities.PutResponsibilityTypeAllocationsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));


            try
            {
                List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();

                ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();
                if (responsibility == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid ResponsibilityType uid for this request."));

                foreach (var allocation in model)
                {
                    AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();
                    if (assetType == null)
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeUid uid privided.",
                            Success = false
                        });
                        continue;
                    }

                    List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
                    {
                        AssetTypeClass.BusinessAsset,
                        AssetTypeClass.TechnicalAsset,
                        AssetTypeClass.FusionAttribute,
                        AssetTypeClass.Model,
                        AssetTypeClass.Rule,
                        AssetTypeClass.Policy,
                        AssetTypeClass.ReferenceItemType
                    };
                    if (!allowedClasses.Contains(assetType.Class))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeClass. [{assetType.Class.ToString()}] is not valid.",
                            Success = false
                        });
                        continue;
                    }

                    var validValues = Permission.DeleteAsset.GetList().Select(x => x.Value);
                    if (allocation.Permissions.Any(x => !validValues.Contains(x)))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid Permission privided. [{string.Join(",", allocation.Permissions.Where(x => !validValues.Contains(x)).ToArray())}]",
                            Success = false
                        });
                        continue;
                    }

                    if (!Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Allocation not found.",
                            Success = false
                        });
                        continue;
                    }

                    results.Add(ResponsibilityRepository.EditAllocation(responsibility, assetType, allocation.Permissions));
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Deletes a list of all allocations for the specified Asset.
        /// </summary>
        /// <param name="uid">The Uid of the Responsibilty type.</param>
        /// <param name="model">A list of AssetTypeUids to delete allocations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteResponsibilityTypeAllocationsAsync(Guid uid, ResponsibilityTypeAllocationDeleteModel model)
        {
            var prefix = "Responsibilities.DeleteResponsibilityTypeAllocationsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));


            try
            {
                List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();

                ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();
                if (responsibility == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid ResponsibilityType uid for this request."));

                foreach (var allocation in model.Items)
                {
                    AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();
                    if (assetType == null)
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeUid uid privided.",
                            Success = false
                        });
                        continue;
                    }

                    List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
                    {
                        AssetTypeClass.BusinessAsset,
                        AssetTypeClass.TechnicalAsset,
                        AssetTypeClass.FusionAttribute,
                        AssetTypeClass.Model,
                        AssetTypeClass.Rule,
                        AssetTypeClass.Policy,
                        AssetTypeClass.ReferenceItemType
                    };
                    if (!allowedClasses.Contains(assetType.Class))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Invalid AssetTypeClass. [{assetType.Class.ToString()}] is not valid.",
                            Success = false
                        });
                        continue;
                    }

                    if (!Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
                    {
                        results.Add(new ResponsibilityTypeAllocationResponseModel()
                        {
                            AssetTypeUid = allocation.AssetTypeUid,
                            Message = $"Allocation not found.",
                            Success = false
                        });
                        continue;
                    }
                    results.Add(await ResponsibilityRepository.DeleteAllocation(responsibility, assetType, model.Cascade));
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
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
        /// <permission>Admin or Ownership read required</permission>
        /// <returns>Returns a list of assets and there corresponding ownership information.</returns>
        [
            HttpGet,
            Route("assignments"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(AssetResponsibilityItemModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
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
                string pageSize = "5";
                string pageNum = "1";
                int _pageSize;
                int _pageNum;
                var timeout = 300;


                queryParams.ToList().ForEach(q =>
                {
                    var key = q.Key.ToLower();

                    if (key.StartsWith("_"))
                    {
                        switch (key)
                        {
                            case "_pagesize":
                                pageSize = q.Value;
                                break;
                            case "_pagenum":
                                pageNum = q.Value;
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

                Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
                string isValid = isPageSizeAndNumValid(pageParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, isValid);
                }

                //validation dont allow assigneeuid filter across entire universe

                if (!string.IsNullOrEmpty(assigneeUidFilter) && string.IsNullOrEmpty(assetTypeUidFilter) && string.IsNullOrEmpty(assetUidFilter))
                {
                    return ReturnApiError(HttpStatusCode.InternalServerError, "In order to use the _assigneeuid filter the _assetTypeUid or _assetUid filter must also be specified.");
                }

                int.TryParse(pageSize, out _pageSize);
                int.TryParse(pageNum, out _pageNum);

                AssetResponsibilitiesApiModel res = await ResponsibilityRepository.GetResponsibilities(queryParams, responsibilityUidFilter, assigneeUidFilter, assetUidFilter, assetTypeUidFilter, _pageSize, _pageNum, timeout);

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

        /// <summary>
        /// Inserts responsibility types of a given responsibility types list.
        /// </summary>
        /// <param name="responsibilityTypes">The list of responsibility types for insertion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeUpsertResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> InsertResponsibilityTypes(List<ResponsibilityTypeInsertModel> responsibilityTypes)
        {
            var prefix = "Responsibilities.InsertResponsibilityTypes => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (responsibilityTypes == null)
                    responsibilityTypes = readRequestJsonContent<List<ResponsibilityTypeInsertModel>>(Request, true).Result;

                if (responsibilityTypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (responsibilityTypes.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided any predicates to process in this request."));

                if (responsibilityTypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} predicates in this request."));

                var execution = getApiExecution(responsibilityTypes.Count);

                var upserts = new List<ResponsibilityTypeUpsertModel>();
                upserts = responsibilityTypes.ConvertAll(x => new ResponsibilityTypeUpsertModel() {
                    Name = x.Name, Description = x.Description, Uid = null
                });

                List<ResponsibilityTypeUpsertResult> results = ResponsibilityRepository.UpsertResponsibilityTypes(upserts, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }


        /// <summary>
        /// Updates responsibility types of a given responsibility types list.
        /// </summary>
        /// <param name="responsibilityTypes">The list of responsibility types for update.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the PUT request.", typeof(List<ResponsibilityTypeUpsertResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypes)
        {
            var prefix = "Responsibilities.UpdateResponsibilityTypes => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (responsibilityTypes == null)
                    responsibilityTypes = readRequestJsonContent<List<ResponsibilityTypeUpsertModel>>(Request, true).Result;

                if (responsibilityTypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (responsibilityTypes.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided any predicates to process in this request."));

                if (responsibilityTypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} predicates in this request."));

                var execution = getApiExecution(responsibilityTypes.Count);

                List<ResponsibilityTypeUpsertResult> results = ResponsibilityRepository.UpsertResponsibilityTypes(responsibilityTypes, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Deletes responsibility type.
        /// </summary>
        /// <param name="responsibilityTypes">Responsibility type Uid for deletion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ResponsibilityTypeDeleteResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel responsibilityTypes)
        {
            var prefix = "Responsibilities.DeleteResponsibilityTypes => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (responsibilityTypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                ResponsibilityTypeDeleteResult results = ResponsibilityRepository.DeleteResponsibilityTypes(responsibilityTypes);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

    }
}