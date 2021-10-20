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
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling metrics and scoring for assets throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/scoring"),
        Authorize
    ]
    public class ScoringController : BaseV2ApiController
    {
        #region DI

        IAssetRepository AssetRepository;
        IMetricsRepository MetricsRepository;
        IScoringRepository ScoringRepository;
        public ScoringController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IScoringRepository scoringRepository, IAssetRepository assetRepository, IMetricsRepository metricsRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            this.AssetRepository = assetRepository;
            this.MetricsRepository = metricsRepository;
            this.ScoringRepository = scoringRepository;
        }

        #endregion

        #region Allocations

        /// <summary>
        /// Gets a list of score definitions set up in Administration / Scoring.
        /// </summary>
        /// <param name="Class">Allows for filtering the allocations by asset type class. The Fusion, FusionAttribute, Organization, User, Group, FusionQuery, Reference, Diagram, Generic and ReferenceItemType class types are not applicable for scoring.</param>
        /// <returns>The allocation.</returns>
        [
            HttpGet,
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerParameter("allocationUid", "Returns allocation whose uid meets the value provided.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("assetUid", "Returns allocations that the provided asset uid contains scores for.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("assetTypeUid", "Returns allocations whose asset type's uid meets the value provided.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_state", "Returns allocations whose state is one of two possible values: Active, or Deleted. When using this parameter you must provide one of these two values.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("assetClassName", "Returns allocations whose asset type class falls within the specified value provided. You must provide part or all of the Name property from the api/v2/assets/classes endpoint.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("assetTypePath", "Returns allocations whose asset type's path contains the value provided.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("scoreType", "Returns allocations whose score type is either Governance or Data Quality.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("isExternallyCalculated", "Returns allocations whose scores are externally calculated. When providing this parameter use one of the following values: external; internal.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by asset type path.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of allocations.", typeof(List<AllocationApiGetModel>)),            
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAllocations(AssetTypeClass? Class = null)
        {
            const string ERROR_HEADING = "Error retrieving allocations";

            try
            {

                var queryParams = Request.GetQueryNameValuePairs();

                string errorMessage = string.Empty;

                List<AllocationApiGetModel> allocations = ScoringRepository.GetAllocations(queryParams, out errorMessage, Class);

                if (!string.IsNullOrEmpty(errorMessage))
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, errorMessage);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Creates a score definition.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPost,
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.Created, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to insert this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostAllocation(AllocationApiUpsertModel model)
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
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your allocation was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to update this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PutAllocation(Guid allocationUid, AllocationApiUpsertModel model)
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
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteAllocation(Guid allocationUid)
        {
            const string ERROR_HEADING = "Error deleting allocation";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, ERROR_HEADING, string.Format(ScoreApiMessages.AllocationNotExists, allocationUid.ToString()));

                var hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                if (hasActiveMeasures)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ERROR_HEADING, ScoreApiMessages.RestrictDeleteScore);
                }

                ScoringRepository.DeleteAllocation(alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ConfirmResponse { message = ScoreApiMessages.AllocationDeleteMessage }));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Exports a list of score definitions.
        /// </summary>
        /// <returns>A excel file containing score definitions.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            ApiExplorerSettings(IgnoreApi = true),
            Route("export"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported realtionship types to Excel.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult ExportAllocationsToExcel()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            queryParams = queryParams.Union(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("_state", "1") });
            string error = string.Empty;
            var models = ScoringRepository.GetAllocations(queryParams, out error);
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Asset Class");
            document.SetCellValue(1, index++, "Asset Type");
            document.SetCellValue(1, index++, "Score Type");
            document.SetCellValue(1, index++, "Score Calculation");
            document.SetCellValue(1, index++, "Threshold - Poor");
            document.SetCellValue(1, index++, "Threshold - Average");
            document.SetCellValue(1, index++, "Threshold - Good");
            document.SetCellValue(1, index++, "Asset Type UID");
            document.SetCellValue(1, index++, "Score UID");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;

                document.SetCellValue(rowNumber, index++, row.assetClassName.GetDisplayName());
                document.SetCellValue(rowNumber, index++, row.assetTypePath);
                document.SetCellValue(rowNumber, index++, row.scoreType.GetDisplayName());
                document.SetCellValue(rowNumber, index++, row.isExternallyCalculated ? "External" : "Internal");
                document.SetCellValue(rowNumber, index++, $"0-{row.lowerThreshold}");
                document.SetCellValue(rowNumber, index++, $">{row.lowerThreshold}-{row.upperThreshold}");
                document.SetCellValue(rowNumber, index++, $">{row.upperThreshold}-100");
                document.SetCellValue(rowNumber, index++, row.assetTypeUid.ToString());
                document.SetCellValue(rowNumber, index++, row.uid.ToString());
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.GetBuffer())
            };
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = string.Format("Relationship Types {0}.xlsx", System.DateTime.Now.ToShortDateString())
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }

        #endregion

        #region Evidence Endpoints

        /// <summary>
        /// Returns the rule results used to determine the data quality score for this score item based on a defined measure.
        /// </summary>
        /// <param name="scoreItemUid">The Uid of the score item result. This is the ScoreItemUid property value which may be found via the following endpoint: api/v2/metrics/{allocationUid}/assets/{assetUid}/pointbreakdown</param>
        /// <remarks>
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
        /// </remarks>
        /// <returns>The object containing rule results.</returns>
        [
            HttpGet,
            Route("{scoreItemUid:Guid}/quality/evidence"),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", SIMPLE_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by OwningAssetDisplayPath.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_sort", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the rule results used to determine the data quality score for this score item based on a defined measure.", typeof(DataQualityScoreItemEvidenceViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, CONFLICT_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetEvidenceForDataQualityScoreItem(Guid scoreItemUid)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var model = await ScoringRepository.GetEvidenceForDataQualityScoreItem(scoreItemUid, queryParams);
                
                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

                if (isStreamResponse)
                {
                    var document = new SLDocument();

                    // Select the first worksheet as the active one.
                    var firstSheet = document.GetWorksheetNames()[0];
                    document.SelectWorksheet(firstSheet);

                    document.SetCellValue(1, 1, "Rule");
                    document.SetCellValue(1, 2, "Asset");
                    document.SetCellValue(1, 3, "Effective Date");
                    document.SetCellValue(1, 4, "Pass Fraction");

                    int ix = 2;
                    model.items.ForEach(row =>
                    {
                        document.SetCellValue(ix, 1, row.OwningAssetDisplayPath);
                        document.SetCellValue(ix, 2, row.EvaluatedAssetDisplayPath);
                        document.SetCellValue(ix, 3, row.EffectiveDate?.ToString("yyyy-MM-dd"));
                        document.SetCellValue(ix, 4, row.PassFraction.ToString());
                        ix++;
                    });

                    document.AutoFitColumn(1);
                    document.AutoFitColumn(2);
                    document.AutoFitColumn(3);

                    var stream = new MemoryStream();
                    document.SaveAs(stream);

                    byte[] bytes = stream.ToArray();

                    return ResponseMessage(createFileResponseMessage(HttpStatusCode.OK, $"Rule Results.xlsx", bytes));
                }
                else
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
                }
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Conflict, ErrorMessage = string.Format(ScoreApiMessages.ScoreNotDataQualityMeasure, scoreItemUid.ToString()) },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ScoreApiMessages.RestrictReadAssetScoreITem },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(ScoreApiMessages.ScoreNotExists, scoreItemUid.ToString()) }
                };
                return DetermineUnhandledException(
                    ex, 
                    ScoreApiMessages.ErrorGetDataQualityScore, 
                    messages, 
                    new Dictionary<string, string> { { "Method Name", "GetEvidenceForDataQualityScoreItem" } } 
                );
            }
        }


        #endregion

        /// <summary>
        /// Gets a list of measures for the score definition UID provided.
        /// </summary>
        /// <param name="allocationUid">The Uid of the score allocation.</param>
        /// <returns>An array of measures for the specified score definition.</returns>
        [
            HttpGet,
            Route("allocations/{allocationUid:Guid}/structure"),
            SwaggerParameter("_includeDisabled", "Parameter to include disabled measures or not.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerProduces("application/json")
        ]
        public IHttpActionResult GetMetricStructureByAllocation(Guid allocationUid)
        {
            var prefix = "Metrics.GetMetricStructureByAssetType => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                bool includeDisabled = false;
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "_includedisabled"))
                {
                    var includeDisabledString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_includedisabled").Value;
                    if (!bool.TryParse(includeDisabledString, out includeDisabled))
                    {
                        throw new ArgumentException($"Invalid value [{includeDisabledString}] provided in the request", "_includedisabled");
                    }
                }
                List<State> states = new List<State>() { State.Active };
                if (includeDisabled)
                    states.Add(State.Deleted);

                var models = MetricsRepository.GetMetricStructureByAllocation(allocationUid, states);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage));
            }
        }


        /// <summary>
        /// Get a list of asset types that have not been allocated to the provided score type.
        /// </summary>
        /// <param name="scoreType">The score type to get asset types with no allocations.</param>
        /// <returns>List of asset types that have not been allocated to the provided score type.</returns>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            Route("unallocatedAssetTypes/{scoreType}"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns a list of asset types that are not yet allocated to the score type provided.", typeof(List<AllocationApiGetUnallocatedAssetTypeModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)), 
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetUnallocatedAssetTypesForScoreType(string scoreType)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new StatusCodeException(HttpStatusCode.Unauthorized);
                }

                if (!Enum.TryParse(scoreType, true, out ScoreType sc))
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest);
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, await ScoringRepository.GetUnallocatedAssetTypes(sc)));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = ApiMessages.InvalidScoreType },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Unauthorized, ErrorMessage = ApiMessages.EndpointNotAuthorizedMessage }
                };
                return DetermineUnhandledException(
                    ex,
                    ScoreApiMessages.ErrorUnallocatedAssetType,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetUnallocatedAssetTypesForScoreType" } }
                );
            }
        }

        /// <summary>
        /// Post externally calculated scores and measure results based on score type.
        /// </summary>
        /// <param name="model">The externally calculated score results to load.</param>
        /// <param name="scoreType">
        /// The score type of the score results. Valid values for scoreType are: 
        ///     - [1] Governance 
        ///     - [2] DataQuality
        /// Either the numerical value or string value can be supplied.
        /// </param>
        /// <returns>List of results.</returns>
        [
            HttpPost,
            Route("{scoreType}/externalresults"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved.", typeof(List<ExternalScoreResultApiResponseModel>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostExternalResultsByScoreType(string scoreType, List<ExternalScoreResultApiRequestModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new StatusCodeException(HttpStatusCode.Forbidden);
                }

                if (!Enum.TryParse(scoreType, true, out ScoreType scoreTypeEnum))
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest);
                }

                var execution = getApiExecution(model.Count);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, ScoringRepository.PostExternalResults(scoreTypeEnum, model, execution)));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = ApiMessages.InvalidScoreType },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ApiMessages.EndpointNotAuthorizedMessage }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.ErrorAddingScoreResultsHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "PostExternalResultsByScoreType" } }
                );
            }
        }


        /// <summary>
        /// Post measure results by score type to calculate a score internally.
        /// </summary>
        /// <param name="model">The score results to load.</param>
        /// <param name="scoreType">
        /// The score type of the score results. Valid values for scoreType are: 
        ///     - [1] Governance
        /// Either the numerical value or string value can be supplied.
        /// </param>
        /// <returns>The results.</returns>
        [
            HttpPost,
            Route("{scoreType}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<InternalScoreResultApiResponseModel>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostScoreResultsByScoreType(string scoreType, List<InternalScoreResultApiRequestModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new StatusCodeException(HttpStatusCode.Forbidden);
                }

                if (!Enum.TryParse(scoreType, true, out ScoreType scoreTypeEnum))
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest);
                }

                if (model == null || model.Count < 1)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
                }

                var execution = getApiExecution(model.Count);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, ScoringRepository.PostScoreResults(scoreTypeEnum, execution, model)));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = ApiMessages.InvalidScoreType },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ApiMessages.EndpointNotAuthorizedMessage }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.ErrorAddingScoreResultsHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "PostScoreResultsByScoreType" } }
                );
            }
        }

        /// <summary>
        /// Post externally calculated scores and measure results based on score definition UID.
        /// </summary>
        /// <param name="model">The externally calculated score results to load.</param>
        /// <param name="allocationUid">The unique identifier of the score definition.</param>
        /// <returns>List of results.</returns>
        [
            HttpPost,
            Route("{allocationUid:Guid}/externalresults"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved.", typeof(List<ExternalScoreResultApiResponseModel>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostExternalResultsByAllocation(Guid allocationUid, List<ExternalScoreResultApiRequestModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new StatusCodeException(HttpStatusCode.Forbidden);
                }

                if (model == null || model.Count < 1)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
                }

               
                foreach(var m in model)
                {
                    var isDistinct = m.measures.GroupBy(i => i.measureUid).Select(g => g.Key).ToList();
                    if(isDistinct.Count() != m.measures.Count())
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, ScoreApiMessages.DuplicateMesaureUid);
                    }
                }

                var allocation = Company.GetByUid<MetricAllocation>(allocationUid);

                if (allocation == null)
                {
                    throw new StatusCodeException(HttpStatusCode.NotFound);
                }

                if (!allocation.IsExternallyCalculated)
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest);
                }

                var execution = getApiExecution(model.Count);

                return ResponseMessage(
                    Request.CreateResponse(
                        HttpStatusCode.OK, 
                        ScoringRepository.PostExternalResults(allocation, model, execution)
                    )
                );
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(ScoreApiMessages.ScoreDefinitionNotFound, allocationUid.ToString()) },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = string.Format(ScoreApiMessages.ScoreNotExternalCalculation, allocationUid.ToString()) },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ApiMessages.EndpointNotAuthorizedMessage }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.ErrorAddingScoreResultsHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "PostExternalResultsByAllocation" } }
                );
            }
        }

        /// <summary>
        /// Post measure results by the score definition UID  to calculate a score internally.
        /// </summary>
        /// <param name="model">The score results to load.</param>
        /// <param name="allocationUid">The unique identifier of the score definition.</param>
        /// <returns>The results.</returns>
        [
            HttpPost,
            Route("{allocationUid:Guid}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<InternalScoreResultApiResponseModel>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostScoreResultsByAllocation(Guid allocationUid, List<InternalScoreResultApiRequestModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new StatusCodeException(HttpStatusCode.Forbidden);
                }

                if (model == null || model.Count < 1)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
                }

                var allocation = Company.GetByUid<MetricAllocation>(allocationUid);

                if (allocation == null)
                {
                    throw new StatusCodeException(HttpStatusCode.NotFound);
                }

                if (allocation.IsExternallyCalculated)
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest);
                }

                var execution = getApiExecution(model.Count);
                
                return ResponseMessage(
                    Request.CreateResponse(
                        HttpStatusCode.OK, 
                        ScoringRepository.PostScoreResults(allocation, execution, model)
                    )
                );
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(ScoreApiMessages.ScoreDefinitionNotFound, allocationUid.ToString()) },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = string.Format(ScoreApiMessages.ScoreNotExternalCalculation, allocationUid.ToString()) },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ApiMessages.EndpointNotAuthorizedMessage }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.ErrorAddingScoreResultsHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "PostScoreResultsByAllocation" } }
                );
            }
        }

        /// <summary>
        /// Get the Measure Version history.
        /// </summary>
        /// <param name="measureUid">The unique identifier for the measure.</param>
        /// <returns>The history for a given an measure.</returns>
        [
            HttpGet,
            Route("history/measure/{measureUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the version history the given measure.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetMeasureHistory(Guid measureUid)
        {
            try
            {
                var models = MetricsRepository.GetMetricVersionHistory(measureUid);
                
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models.OrderByDescending(x => x.Version)));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Unauthorized, ErrorMessage = ScoreApiMessages.RestrictReadVersionHistory }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.EndpointGettingMeasureHistoryHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetMeasureHistory" } }
                );
            }
        }


        /// <summary>
        /// Get the score history by allocation and asset.
        /// </summary>
        /// <param name="assetUid">The public identifier for the asset.</param>
        /// <param name="allocationUid">The allocation identifier of score to return.</param>
        /// <param name="effectiveDate">The date which you want to retrieve a score for. If not provided, the entire score history will be returned. If provided, the date must be today or earlier.</param>
        /// <returns>The score history for a given an asset type Uid and score type.</returns>
        [
            HttpGet,
            Route("history/{allocationUid}/{assetUid}/scores"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the score history given an asset and allocation.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult GetScoreHistoryByAllocationAndAsset(string allocationUid, string assetUid, DateTime? effectiveDate = null)
        {
            Guid _allocationUid;
            Guid _assetUid;

            var allocationStatus = validateScoreAllocation(allocationUid, out _allocationUid);
            if (allocationStatus.StatusCode != HttpStatusCode.OK)
            {
                return ResponseMessage(Request.CreateErrorResponse(allocationStatus.StatusCode, allocationStatus.Message));
            }

            var assetStatus = validateAsset(assetUid, Permission.ReadAsset, out _assetUid);
            if (assetStatus.StatusCode != HttpStatusCode.OK)
            {
                return ResponseMessage(Request.CreateErrorResponse(assetStatus.StatusCode, assetStatus.Message));
            }

            if (effectiveDate.HasValue)
            {
                if (effectiveDate.Value.ToUniversalTime().Date > DateTime.UtcNow.Date)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ScoreApiMessages.EffectiveDateNotGTToday));
                }
            }

            string sql = "";
            object parameters;
            if (effectiveDate.HasValue)
            {
                parameters = new { allocationUid = _allocationUid, assetUid = _assetUid, effectiveDate = effectiveDate.Value.ToUniversalTime().Date };
                sql = @"
	select	S.EffectiveDate as [EffectiveDate],
			S.[EndDate] as [EndDate],
			cast(S.Value * 100 as decimal(18,1)) as Score
	from	metrics.Score S
			inner join metrics.Allocation A on A.Uid = S.AllocationUid and A.Uid = @allocationUid and S.AssetUid = @assetUid and S.EffectiveDate = @effectiveDate";

            }
            else 
            {
                parameters = new { allocationUid = _allocationUid, assetUid = _assetUid };
                sql = @"
	declare @date date = getutcdate()

	select	S.EffectiveDate as [EffectiveDate],
			S.[EndDate] as [EndDate],
			cast(S.Value * 100 as decimal(18,1)) as Score
	from	metrics.Score S
			inner join metrics.Allocation A on A.Uid = S.AllocationUid and A.Uid = @allocationUid and S.AssetUid = @assetUid and S.EffectiveDate <= @date
	union
	select	cast(@date as date) as [EffectiveDate],
			null as [EndDate],
			cast(S.Value * 100 as decimal(18,1)) as Score
	from	metrics.Score S
			inner join metrics.Allocation A on A.Uid = S.AllocationUid and A.Uid = @allocationUid and S.AssetUid = @assetUid and S.EffectiveDate <= @date and S.EndDate is null";
            }

            var model = Company.Query<dynamic>(sql, parameters, ApiTimeout);

            return ResponseMessage(Request.CreateResponse<dynamic>(HttpStatusCode.OK, model));
        }


        /// <summary>
        /// Forces a recalculation of score item results associated with a specific measure.
        /// </summary>
        /// <param name="allocationUid">The unique identifier for the allocation the measure belongs to.</param>
        /// <param name="measureUid">The unique identifier for the measure.</param>
        /// <returns>Http Status OK</returns>
        [
            HttpPut,
            Route("{allocationUid:Guid}/measures/{measureUid:Guid}/recalculations"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Now Recalculating score item results for this measure."),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, CONFLICT_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult RecalculateMeasureScoreItems(Guid allocationUid, Guid measureUid)
        {
            try
            {
                var executionUid = MetricsRepository.RecalculateMeasureScoreItems(allocationUid, measureUid);
                return ResponseMessage(
                    Request.CreateResponse(
                        HttpStatusCode.OK, 
                        new ApiExecutionRecievedResponse
                        {
                            ExecutionID = executionUid,
                            Message = ApiMessages.ExecutionIDStatus,
                            Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/scoring/executions/{executionUid}/status"
                        }
                    )
                );
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = ScoreApiMessages.RestrictRecalculateScore }
                };
                return DetermineUnhandledException(
                    ex,
                    ApiMessages.EndpointRecalculatingMeasureScoreItemsHeading,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "RecalculateMeasureScoreItems" } }
                );
            }
        }


        /// <summary>
        /// GETs all score execution records.
        /// </summary>
        /// <remarks>
        /// Depending on the size, each execution may take a significant amount of time to complete. You can check the PercentComplete property to see how far along the execution has progressed. 
        /// Once complete, the execution should have a valid CompletedOn date. Execution tasks that have not yet started, or are not currently being processed, will have an empty ProcessingStartedOn 
        /// date and the Processing flag will be false. It is possible that an execution will have finished yet still have a PercentComplete value less than 1 if the Failures count is greater than 0.
        /// </remarks>
        /// <param name="_pageNum">The page to return in results.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 200.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution statuses.", typeof(List<ScoreExecution>))
        ]
        public async Task<IHttpActionResult> GetExecutions(int _pageSize = 200, int _pageNum = 1)
        {
            var executions = ScoringRepository.GetExecutions(_pageSize, _pageNum);
            return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            executions
                        )
                    )
                ).ConfigureAwait(false);
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <remarks>
        /// Depending on the size, each execution may take a significant amount of time to complete. You can check the PercentComplete property to see how far along the execution has progressed. 
        /// Once complete, the execution should have a valid CompletedOn date. Execution tasks that have not yet started, or are not currently being processed, will have an empty ProcessingStartedOn 
        /// date and the Processing flag will be false. It is possible that an execution will have finished yet still have a PercentComplete value less than 1 if the Failures count is greater than 0.
        /// </remarks>
        /// <param name="uid">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{uid:Guid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A scoring execution status.", typeof(ScoreExecution)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid uid)
        {
            try
            {
                var res = ScoringRepository.GetExecutionById(uid);
                if (res == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
                }
                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            res
                        )
                    )
                ).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", "Scoring.GetExecutionStatus => " },
                    { "ExecutionID", uid.ToString() },
                    { "ExecutionUid", uid.ToString() }, //left to prevent a breaking change
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// GETs all score execution items for a given execution.
        /// </summary>
        /// <param name="uid">The unique identifier (Uid) of the score execution.</param>
        /// <param name="_pageNum">The page to return in results.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 200.</param>
        /// <param name="changeType">The type of change you want to filter by. If left blank, then all changes under this execution will be returned..</param>
        /// <returns>A paged list of execution items</returns>
        [
            HttpGet,
            Route("executions/{uid:Guid}/items"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution items.", typeof(List<ScoreExecutionItemViewModel>))
        ]
        public async Task<IHttpActionResult> GetExecutionItems(Guid uid, int _pageSize = 200, int _pageNum = 1, core.queue.ScoreQueueChangeType? changeType = null)
        {
            var res = ScoringRepository.GetExecutionById(uid);
            if (res == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound,ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
            }
            var items = ScoringRepository.GetExecutionItems(res.ID, _pageSize, _pageNum, changeType);
            return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            items
                        )
                    )
                ).ConfigureAwait(false);
        }

    }
}
