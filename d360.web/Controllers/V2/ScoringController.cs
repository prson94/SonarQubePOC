using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core;
using d360.web.Filters;
using d360.core.exceptions;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.core.entities.Scoring;
using Dapper;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling metrics and scoring for assets throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/scoring"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class ScoringController : BaseV2ApiController
    {
        #region DI

        IAssetRepository AssetRepository;
        IScoringRepository ScoringRepository;
        public ScoringController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IScoringRepository scoringRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            this.AssetRepository = assetRepository;
            this.ScoringRepository = scoringRepository;
        }

        #endregion



        /// <summary>
        /// Get a list of allocations.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpGet,
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of allocations.", typeof(List<AllocationApiGetModel>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult GetAllocations()
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error retrieving allocations", "You are not authorized to perform this action.");
                }

                var queryParams = Request.GetQueryNameValuePairs();
                List<AllocationApiGetModel> allocations = ScoringRepository.GetAllocations(queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }


        /// <summary>
        /// Creates allocation based on provided asset type uid and score type.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPost,
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to insert this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostAllocation(AllocationApiUpsertModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding allocation", "You are not authorized to perform this action.");
                }

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"You have not provided valid assetTypeUid.");

                if (model.scoreType == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"You have not provided valid scoreType.");
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid.Value);

                List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule };
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error adding allocation", $"AssetType with uid {model.assetTypeUid} does not exists.");

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"Asset type has invalid class.");

                Allocation alloc = ScoringRepository.GetAllocationByModel(model);

                if (alloc != null && alloc.State == State.Active)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"Active allocation with same configuration already exists.");
                }

                AllocationApiGetModel allocation = ScoringRepository.PostAllocation(model, ref alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocation));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }

        /// <summary>
        /// Updates an existing allocation.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPut,
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your allocation was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to update this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PutAllocation(Guid allocationUid, AllocationApiUpsertModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error updating allocation", "You are not authorized to perform this action.");
                }

                Allocation alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error updating allocation", $"Allocation with uid {allocationUid} does not exists");

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"You have not provided valid assetTypeUid");

                if (model.scoreType == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"You have not provided valid scoreType");
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid.Value);

                List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule };
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error updating allocation", $"AssetType with uid {model.assetTypeUid} does not exists");

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Asset type has invalid class");

                bool alreadyExists = ScoringRepository.DoesAllocationExists(allocationUid, model);

                if (alreadyExists)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Allocation with same configuration already exists");
                }

                bool hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                if (hasActiveMeasures)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Allocation have active measures");
                }
                AllocationApiGetModel allocation = ScoringRepository.UpdateAllocation(model, alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocation));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error updating allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }

        /// <summary>
        /// Gets allocations.
        /// </summary>
        /// <returns>The metric.</returns>
        [
            HttpDelete,
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteAllocation(Guid allocationUid)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error deleting allocation", "You are not authorized to perform this action.");
                }

                var alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error deleting allocation", $"Allocation with uid {allocationUid} does not exists");

                var hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                if (hasActiveMeasures)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error deleting allocation", $"Allocation have active measures");
                }

                ScoringRepository.DeleteAllocation(alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ConfirmResponse()));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error deleting allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }
    }
}
