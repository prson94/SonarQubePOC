using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resources;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling semantics throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/semantics"),
        Authorize
    ]
    public class SemanticsController : BaseV2ApiController
    {
        #region DI

        ISemanticsRepository SemanticsRepository;
        public SemanticsController(ICommunityContext community, ICompanyContext company, ISemanticsRepository semanticsRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            this.SemanticsRepository = semanticsRepository;
        }

        #endregion


        /// <summary>
        /// Gets a list of semantics for use in data profiling.
        /// </summary>
        [
            HttpGet,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantics are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of a semantic. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("asOfEffectiveDate", "Assumed to be current UTC date if left empty, otherwise, gets semantics as of the specified effective date, and nothing later. This is the parameter used to get prior versions.", DataType = "datetime", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantics.", typeof(List<Semantic>)),            
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemantics(CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var apiModels = await SemanticsRepository.GetSemanticsAsync(queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch
            {
                return errorMessageResponse(
                    HttpStatusCode.InternalServerError, 
                    "Error retrieving semantics", 
                    ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Gets a list of semantics for use in data profiling.
        /// </summary>
        [
            HttpGet,
            Route("{qualifier}/versions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantics are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantics.", typeof(List<Semantic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticVersions(string qualifier, CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var apiModels = await SemanticsRepository.GetSemanticVersionsByQualifierAsync(qualifier, queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving semantics", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Selectively updates one or more semantics based on the fields provided. 
        /// If certain fields that make up a semantic are missing, then those fields will not be updated.
        /// </summary>
        /// <returns>The updated semantics.</returns>
        [
            HttpPatch,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your allocation was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to update this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PatchSemantics(List<PatchSemantic> semantics)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var apiModels = SemanticsRepository.PatchSemanticsAsync(semantics);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error updating semantics", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Creates a score definition.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPost,
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Created, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to insert this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostSemantics(List<PostSemantic> semantics)
        {
            const string ERROR_HEADING = "Error adding allocation";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ActionApiMessages.InvalidAssetTypeUid);

                List<ScoreType> scoreTypes = new List<ScoreType>() { ScoreType.DataQuality, ScoreType.Governance };

                if (!scoreTypes.Contains(model.scoreType))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ApiMessages.InvalidScoreType);
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid);

                List<AssetTypeClass> allowedClasses = ScoringRepository.AllowedClassesForScoreType();
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, ERROR_HEADING, string.Format(ActionApiMessages.AssetTypeNotFound, model.assetTypeUid.ToString()));

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ActionApiMessages.AssettypeInvalidClass);

                MetricAllocation alloc = ScoringRepository.GetAllocationByModel(model);

                if (alloc != null && alloc.State == State.Active)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.ScoreExists);
                }

                if (model.lowerThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.LowerThreshold);
                }
                if (model.upperThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.UpperThreshold);
                }
                if (model.lowerThreshold >= model.upperThreshold)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.UpperGtLower);
                }
                if (model.lowerThreshold <= 0 || model.upperThreshold <= 0 || model.upperThreshold > 100)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.RangeLimitThreshold);
                }

                AllocationApiGetModel allocation = ScoringRepository.PostAllocation(model, ref alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, allocation));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Updates a score definition.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your allocation was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to update this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PutSemantics(List<PutSemantic> semantics)
        {
            const string ERROR_HEADING = "Error updating allocation";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                MetricAllocation alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, ERROR_HEADING, ScoreApiMessages.AllocationNotExists);

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ActionApiMessages.EmptyAllocationRequest);

                List<ScoreType> scoreTypes = new List<ScoreType>() { ScoreType.DataQuality, ScoreType.Governance };

                if (!scoreTypes.Contains(model.scoreType))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ApiMessages.InvalidScoreType);
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid);

                List<AssetTypeClass> allowedClasses = ScoringRepository.AllowedClassesForScoreType();
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, ERROR_HEADING, string.Format(ActionApiMessages.AssetTypeNotFound, model.assetTypeUid.ToString()));

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ActionApiMessages.AssettypeInvalidClass);

                bool alreadyExists = ScoringRepository.DoesAllocationExist(allocationUid, model);

                if (alreadyExists)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.ScoreExists);
                }

                bool hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                bool canBeEdited = (model.assetTypeUid == alloc.AssetTypeUid
                                   && model.scoreType == alloc.ScoreType
                                   && model.isExternallyCalculated == alloc.IsExternallyCalculated)
                                   || !hasActiveMeasures;

                if (!canBeEdited)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.RestrictUpdateScoreField);
                }

                if (model.lowerThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.LowerThreshold);
                }
                if (model.upperThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.UpperThreshold);
                }
                if (model.lowerThreshold >= model.upperThreshold)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.UpperGtLower);
                }
                if (model.lowerThreshold <= 0 || model.upperThreshold <= 0 || model.upperThreshold > 100)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.RangeLimitThreshold);
                }


                AllocationApiGetModel allocation = ScoringRepository.UpdateAllocation(model, alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocation));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Deletes a score definition.
        /// </summary>
        /// <returns>OK status with message.</returns>
        [
            HttpDelete,
            Route("{qualifier}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteSemantic(string qualifier)
        {
            const string ERROR_HEADING = "Error deleting allocation";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }



                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ConfirmResponse { message = ScoreApiMessages.AllocationDeleteMessage }));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }
    }
}
