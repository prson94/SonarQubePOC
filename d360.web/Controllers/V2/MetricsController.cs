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
using System.ComponentModel.DataAnnotations;
using Resources;
using SpreadsheetLight;
using d360.core.resources;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling metrics and scoring for assets throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/metrics"),
        Authorize
    ]
    public class MetricsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IAssetRepository AssetRepository;
        IMetricsRepository MetricsRepository;

        public MetricsController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IMetricsRepository metricsRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            QueueSource = queueSource;
            this.MetricsRepository = metricsRepository;
            this.AssetRepository = assetRepository;
        }

        #endregion



        /// <summary>
        /// Gets a metric by its Uid.
        /// </summary>
        /// <param name="uid">The public identifier for the metric.</param>
        /// <returns>The metric.</returns>
        [
            HttpGet,
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(MetricAsset)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult GetAssetById(Guid uid)
        {
            try
            {
                MetricAsset metricAsset = MetricsRepository.GetMetricByUid(uid);

                if (metricAsset == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error locating metric", $"Metric with Uid of {uid.ToString()} not found.");
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, metricAsset));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving metric", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }


        /// <summary>
        /// Add or updates a metric.
        /// </summary>
        /// <param name="model">The definition of the metric itself. If updating an existing metric, ensure that you populate the Uid property.</param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the ADD request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the UPDATE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not autheorized to make this change.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate what was incorrect about your request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that either your metric or parent metric was not found.", typeof(ErrorResponse))
        ]
        public IHttpActionResult UpsertAsset(MetricAssetViewModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, "Error updating metric", "You are not allowed to update this metric.");
            }

            if (model == null)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You are have provided a null metric.");
            }

            if (model.Uid != Guid.Empty)
            {
                var metric = MetricsRepository.GetMetricByUid(model.Uid);
                if (metric == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", $"Metric with UID {model.Uid} does not exist.");
                }

                if (metric.ParentUid.HasValue && model.IsGroup)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "Maximum number of levels for measures is 2.");
                }

            }

            List<ScoreType> allowedScoreTypes = new List<ScoreType>() { ScoreType.Governance, ScoreType.DataQuality };

            if (model.ScoreType != null && !allowedScoreTypes.Contains(model.ScoreType.Value))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You have not provided valid Score Type.");
            }

            if (model.ScoreType == null)
            {
                model.ScoreType = ScoreType.Governance;
            }

            var allocation = MetricsRepository.GetAllocationByMetricModel(model);

            if (allocation == null)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "There is no allocation for specified Asset Type UID and Score Type.");
            }


            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = true;

            isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
            if (!isValid)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, $"Error updating metric", validationResults.First().ErrorMessage);
            }


            if (allocation.IsExternallyCalculated == false)
            {
                if (model.Weight <= 0 || model.Weight > 1)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, $"Error updating metric", "Weight must be a value between 0 and 1");
                }
                else if (decimal.Round(model.Weight, 2) != model.Weight)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, $"Error updating metric", "Weight can have a maximum of 2 decimal places.");
                }

            }

            if (model.IsGroup && model.Conditions.Count > 0)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "Groups should not have conditions.");
            }

            foreach (var cond in model.Conditions)
            {
                if (cond.FieldTypeID.HasValue && !string.IsNullOrEmpty(cond.FieldName))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You cannot use both FieldTypeID and FieldName as a Field identifier in condition.");
                }

                bool hasFieldDefinition = cond.FieldTypeID.HasValue || !string.IsNullOrEmpty(cond.FieldName);

                if (!hasFieldDefinition)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "FieldTypeId or FieldName definition missing from condition.");
                }

                if (cond.FieldTypeID.HasValue && cond.FieldTypeID <= 0)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "FieldTypeID must be greater than 0.");
                }
            }

            if (model.ParentUid != null && model.ParentUid != Guid.Empty)
            {
                var parent = MetricsRepository.GetMetricByUid(model.ParentUid.Value);

                if (parent == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error updating metric", "Parent metric not found.");
                }

                if (!parent.IsGroup)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "Parent metric must have 'IsGroup' value set to True.");
                }

                if (model.IsGroup || parent.ParentUid != null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "Maximum number of levels for measures is 2.");
                }
            }



            var isNew = true;
            model.EffectiveDate = model.EffectiveDate.Date;

            var result = MetricsRepository.AddOrUpdateMetrics(model, out isNew);
            if (result.StatusCode != HttpStatusCode.OK)
                return errorMessageResponse(result.StatusCode, result.Error, result.Message);

            Company.SaveChanges();
            return successMessageResponse(
                    isNew ? HttpStatusCode.Created : HttpStatusCode.OK,
                    $"Metric {(isNew ? "added" : "updated")}.",
                    $"The specified metric was successfully {(isNew ? "added" : "updated")}."
            );
        }



        /// <summary>
        /// Allows you to remove a metric based on its Uid.
        /// </summary>
        /// <param name="uid">The public identifier for the metric.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteById(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this metric."));

            MetricAsset model = MetricsRepository.GetActiveMetric(uid);

            if (model == null)
                return errorMessageResponse(HttpStatusCode.NotFound, "Error removing metric", "Metric not found.");

            MetricsRepository.DeleteMetric(model);

            return successMessageResponse(HttpStatusCode.OK, "Metric removed.", "Metric successfully removed.");
        }


        /// <summary>
        /// Gets a hierarchical structure of metrics and conditions associated with the asset type Uid provided.
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past or future effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/definition"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset type based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metrics and conditions.", typeof(MetricAssetTypeHierarchyModels)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetTypeAsync(Guid assetTypeUid, DateTime? effectiveDate = null)
        {
            var prefix = "Metrics.GetMetricHierarchyByAssetTypeAsync => ";

            try
            {
                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                var result = MetricsRepository.GetMetricDefinitionHierarchyByAssetType(assetTypeUid, effectiveDate);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown Error", errorMessage));
            }
        }

        /// <summary>
        /// Gets a hierarchical structure of metrics associated with the asset Uid provided, for a given effective date. If no effective date is provided, today's date is used.
        /// </summary>
        /// <param name="assetUid">The Uid of the asset.</param>
        /// <param name="scoreType">The scoreType to be returned.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{scoreType}/{assetUid:Guid}/pointbreakdown"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(MetricAssetHierarchyModels)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse)),
            SwaggerParameter("effectiveDate", "The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past effective date.", DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetAsync(ScoreType scoreType, Guid assetUid)
        {
            var prefix = "Metrics.GetMetricHierarchyByAssetAsync => ";

            try
            {
                DateTime effectiveDate = DateTime.MinValue;
                var param = Request.GetQueryNameValuePairs();
                if (param.Any(x => x.Key.ToLower() == "effectivedate"))
                {
                    var value = param.FirstOrDefault(x => x.Key.ToLower() == "effectivedate").Value;
                    if (!DateTime.TryParse(value, out effectiveDate))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", $"Invalid Effective date provided!"));
                    }
                }
                else
                {
                    effectiveDate = DateTime.UtcNow;
                }


                var asset = AssetRepository.GetAssetByUID(assetUid);

                if (asset == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {assetUid} could not be found."));

                var result = MetricsRepository.GetMetricHierarchyByAsset(assetUid, effectiveDate, scoreType);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Gets a administrative hierarchical structure of metrics associated with the asset type Uid provided.
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("structure/{assetTypeUid:Guid}"),
            SwaggerParameter("_scoreType", "Filter results by score type. By default results are filtered by Governance Score type", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetMetricStructureByAssetType(Guid assetTypeUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to retrieve the metric heirarchy for this asset type."));

            var prefix = "Metrics.GetMetricStructureByAssetType => ";
            var errorMessage = "";

            try
            {
                List<MetricAssetViewModel> models = null;

                ScoreType filterScoreTypes = ScoreType.Governance;

                var queryParams = Request.GetQueryNameValuePairs();

                foreach (var qp in queryParams.ToList())
                {
                    switch (qp.Key.ToLower())
                    {
                        case "_scoretype":
                            Enum.TryParse(qp.Value, true, out filterScoreTypes);

                            List<ScoreType> scoreTypes = new List<ScoreType>() { ScoreType.DataQuality, ScoreType.Governance, ScoreType.Perceptional };

                            if (!scoreTypes.Contains(filterScoreTypes) || string.IsNullOrEmpty(qp.Value))
                            {
                                return errorMessageResponse(HttpStatusCode.BadRequest, "Error retrieve the metric heirarchy", $"You have not provided valid scoreType.");
                            }
                            break;
                    }
                }

                List<string> fragments = MetricsRepository.GetMetricStructureFragments(assetTypeUid, filterScoreTypes);

                models = JsonConvert.DeserializeObject<List<MetricAssetViewModel>>(string.Join("", fragments));
                if (models == null)
                    models = new List<MetricAssetViewModel>();
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage));
            }
        }


        /// <summary>
        /// Gets a administrative hierarchical structure of metrics associated with the asset type Uid provided.
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("fields/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetMetricFieldsByAssetType(Guid assetTypeUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to retrieve the fields for this asset type."));

            var prefix = "Metrics.GetMetricFieldsByAssetType => ";
            var errorMessage = "";

            try
            {
                List<MetricFieldTypeViewModel> models = null;
                List<string> fragments = MetricsRepository.GetMetricFieldFragments(assetTypeUid);

                models = JsonConvert.DeserializeObject<List<MetricFieldTypeViewModel>>(string.Join("", fragments));

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models ?? new List<MetricFieldTypeViewModel>()));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage));
            }
        }


        /// <summary>
        /// Post measure results to calculate a score internally.
        /// </summary>
        /// <remarks>If you do not provide an effective date for a metric result, the current date (UTC) will be used.</remarks>
        /// <param name="model">The list of raw metrics to save for processing.</param>
        /// <returns>The list of staging results.</returns>
        [
            HttpPost,
            Route("results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<BulkMetricTemporaryTableModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostBulkMetricsToStagingAsync(BulkMetricsImport model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships of this type."));

            if (model == null)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You have submitted an invalid or empty data set. Please check your request and submit again."));

            var prefix = "Metrics.PostBulkMetricsToStagingAsync => ";
            var errorMessage = "";

            try
            {
                var execution = getApiExecution(model.Count);
                List<BulkMetricTemporaryTableModel> results = MetricsRepository.BulkMetricsImport(model, execution);

                return ResponseMessage(Request.CreateResponse<List<BulkMetricTemporaryTableModel>>(HttpStatusCode.OK, results));
            }
            catch (GenericException ex)
            {
                return errorMessageResponse(ex.StatusCode, ex.StatusMessage, ex.StatusDescription);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return errorMessageResponse(HttpStatusCode.InternalServerError, "Server Error", errorMessage);
            }
        }



        /// <summary>
        /// Gets a calculated score by asset type Uid
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type.</param>
        /// <remarks><p>In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue.</p>    
        /// </remarks>
        /// <returns>Calculated scores.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/scores"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding calculated scores.", typeof(MetricScoreApiModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric score is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateStart", "Effective start date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateEnd", "Effective end date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The specific Uid of the asset you want the score for.", DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetMetricScores(Guid assetTypeUid)
        {
            var prefix = "Metrics.GetMetricScores => ";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);
                if (assetType == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset type with Uid of {assetTypeUid.ToString()} not found.");
                }

                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
                }

                (var result, string errorMessage) = MetricsRepository.GetMetricScore(assetType, queryParams);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", errorMessage);
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }


        }

        /// <summary>
        /// Gets a administrative hierarchical structure of metrics associated with the asset Uid provided.
        /// </summary>
        /// <param name="uid">The Uid of the asset.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past or future effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{uid}/definitionFromAsset"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetUidAsync(Guid uid, DateTime? effectiveDate = null)
        {
            var asset = Company.Assets.FirstOrDefault(x => x.uid == uid);
            if (asset == null)
                return errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid of {asset.uid.ToString()} not found.");
            var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == asset.AssetTypeID);
            if (assetType == null)
                return errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset type with Uid of {assetType.uid.ToString()} not found.");
            return await GetMetricHierarchyByAssetTypeAsync(assetType.uid, effectiveDate);
        }


        /// <summary>
        /// Get the score history.
        /// </summary>
        /// <param name="assetUid">The public identifier for the asset.</param>
        /// <param name="scoreType">The type of score to return.</param>
        /// <returns>The score history for a given an asset type Uid and score type.</returns>
        [
            HttpGet,
            Route("history/{scoreType}/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the score history given an asset type Uid and score type .", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetHistory(ScoreType scoreType, Guid assetUid)
        {
            int type = (int)scoreType;
            var model = Company.Query<dynamic>(@"EXEC GetScoreHistoryByObject @assetUid, @type", new { assetUid, type });
            return ResponseMessage(Request.CreateResponse<dynamic>(HttpStatusCode.OK, model));
        }


        /// <summary>
        /// Get the score history.
        /// </summary>
        /// <param name="assetUid">The public identifier for the asset.</param>
        /// <returns>The score types for a given an asset Uid.</returns>
        [
            HttpGet,
            Route("getScoreTypes/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the score types given an asset Uid.", typeof(ConfirmResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetScoreTypes(Guid assetUid)
        {
            var model = MetricsRepository.GetScoreTypesForAsset(assetUid);
            return ResponseMessage(Request.CreateResponse<dynamic>(HttpStatusCode.OK, model));
        }

        /// <summary>
        /// Gets the data quality results for an asset
        /// </summary>        
        /// <param name="_owningAssetUid">The unique identifier of a rule.</param>
        /// <param name="_evaluatedAssetUid">The unique identifier of an asset</param>
        /// <param name="_pageSize">The size of the page if there are many results. [Defaults to 250]</param>
        /// <param name="_pageNum">The page number to page through results. [Defaults to 1]</param>
        /// <param name="_order">The name of the field to order results by.</param>
        /// <param name="_direction">The direction in which to order the results (asc/desc). Used in conjunction with _order. [Default asc]</param>
        /// <param name="_effectiveDateStart">Return results with effective date after this date</param>
        /// <param name="_effectiveDateEnd">Return results with effective date before this date</param>
        /// <returns>List of data quality results</returns>
        [
            HttpGet,
            Route("quality/results/"),
            SwaggerParameter("_owningAssetUid", "The unique identifier of a rule.", DataType = "string", ParameterType = "query", Required = true),
            SwaggerParameter("_evaluatedAssetUid", "The unique identifier of an asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for. The default value is 1.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by (Default ascending).", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateStart", "Return results with effective date after this date", DataType = "date-time", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateEnd", "Return results with effective date before this date", DataType = "date-time", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json", "application/vnd.ms-excel", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request has one or more invalid parameters.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of Data Quality Results.", typeof(DataQualityResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetDataQualityResults()
        {
            var queryParams = Request.GetQueryNameValuePairs();

            Asset asset = null;

            Asset ruleAsset = null;

            Guid _owningAssetUid;
            Guid? _evaluatedAssetUid = null;
            string _order = null;
            string _direction = "asc";
            DateTime? _effectiveDateStart = null;
            DateTime? _effectiveDateEnd = null;
            int _pageSize = 250;
            int _pageNum = 1;

            #region Model Validation
            if (queryParams.Any(q => q.Key == "_owningAssetUid"))
            {
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_owningAssetUid").Value, out _owningAssetUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid", $"OwningAssetUid {queryParams.ToList().FirstOrDefault(q => q.Key == "_owningAssetUid").Value} is not a valid Uid"));
                }

                ruleAsset = AssetRepository.GetAssetByUID(_owningAssetUid);

                if (ruleAsset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {_owningAssetUid} could not be found."));
                }
                else if (ruleAsset.AssetType.Class != AssetTypeClass.Rule)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid", $"_owningAssetUid {_owningAssetUid} is not valid");
                }
            }
            else
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Request", $"_owningAssetUid is a required parameter"));
            }

            if (queryParams.Any(q => q.Key == "_evaluatedAssetUid"))
            {
                Guid tempEvaluatedUid;
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_evaluatedAssetUid").Value, out tempEvaluatedUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid", $"EvaluatedAssetUid {queryParams.ToList().FirstOrDefault(q => q.Key == "_evaluatedAssetUid").Value} is not a valid Uid"));
                }

                _evaluatedAssetUid = tempEvaluatedUid;

                asset = AssetRepository.GetAssetByUID(_evaluatedAssetUid.Value);

                if (asset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {_evaluatedAssetUid.Value} could not be found."));
                }
                else if (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid", $"EvaluatedAssetUid {_evaluatedAssetUid.Value} is not valid"));
                }
            }


            if (!Company.HasAssetPermission(ruleAsset.AssetType.Object, ruleAsset.AssetType.ObjectID, Permission.ReadAsset) && (_evaluatedAssetUid != null && !Company.HasAssetPermission(asset.AssetType.Object, asset.AssetType.ObjectID, Permission.ReadAsset)))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                _order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value;
                List<string> _orderColumns = new List<string>() { "ResultUid", "EvaluatedAssetUid", "OwningAssetUid", "EffectiveDate", "RunDate", "Passcount", "FailCount", "Passed" };
                if (_orderColumns.FindIndex(x => x.Equals(_order, StringComparison.InvariantCultureIgnoreCase)) == -1)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_order value '{_order}' is not valid. Value must be one of the following: {string.Join(",", _orderColumns.ToArray())}.");
                }
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                _direction = queryParams.ToList().FirstOrDefault(q => q.Key == "_direction").Value;
                if (!_direction.Equals("asc", StringComparison.InvariantCultureIgnoreCase) && !_direction.Equals("desc", StringComparison.InvariantCultureIgnoreCase))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_direction value '{_direction}' is not valid. Value must be one of the following: asc, desc.");
                }
            }

            if (queryParams.Any(q => q.Key == "_effectiveDateStart"))
            {
                DateTime _tempEffectiveDateStart;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_effectiveDateStart").Value, out _tempEffectiveDateStart))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_effectiveDateStart is not valid.");
                }
                _effectiveDateStart = _tempEffectiveDateStart;

                if (_effectiveDateStart == DateTime.MinValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_effectiveDateStart is not valid.");
                }
            }

            if (queryParams.Any(q => q.Key == "_effectiveDateEnd"))
            {
                DateTime _tempEffectiveDateEnd;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_effectiveDateEnd").Value, out _tempEffectiveDateEnd))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_effectiveDateEnd is not valid.");
                }
                _effectiveDateEnd = _tempEffectiveDateEnd;
                if (_effectiveDateEnd == DateTime.MinValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_effectiveDateEnd is not valid.");
                }
                if (_effectiveDateStart != null && _effectiveDateEnd < _effectiveDateStart)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"_effectiveDateEnd must be after _effectiveDateStart.");
                }
            }
            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
            }
            else
            {
                if (queryParams.Any(q => q.Key == "_pageNum"))
                {
                    _pageNum = int.Parse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value);
                }
                if (queryParams.Any(q => q.Key == "_pageSize"))
                {
                    _pageSize = int.Parse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value);
                }
            }
            #endregion

            try
            {
                d360.core.entities.Metric.DataQualityResult dataQualityResult = new d360.core.entities.Metric.DataQualityResult();

                dataQualityResult = await Task.FromResult(MetricsRepository.GetDataQualityResults(_owningAssetUid, _evaluatedAssetUid, _pageSize, _pageNum, _order, _direction, _effectiveDateStart, _effectiveDateEnd));

                if (Request.Headers.Accept.ToString().Equals("application/octet-stream", StringComparison.InvariantCultureIgnoreCase) || Request.Headers.Accept.ToString().Equals("application/vnd.ms-excel", StringComparison.InvariantCultureIgnoreCase))
                {
                    SLDocument document = CreateResponseDocument(dataQualityResult);
                    var stream = new System.IO.MemoryStream();
                    document.SaveAs(stream);

                    var result = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(stream.GetBuffer())
                    };
                    result.Content.Headers.ContentLength = stream.Length;

                    result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                    {
                        FileName = $"Data_Quality_Results_{System.DateTime.Now.ToString("yyyy-MM-dd")}.xlsx"
                    };
                    result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

                    return ResponseMessage(result);
                }
                else
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dataQualityResult));
                }

            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving Data Quality Results", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }



        /// <summary>
        /// Create the data quality result for an asset / Rule
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// </remarks>
        /// <returns>A list of data quality results including any error messages.</returns>
        [
            HttpPost,
            Route("quality/results/"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the Uid of the new data quality result.", typeof(List<DataQualityResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> PostDataQualityResultAsync(List<DataQualityInsertModel> request)
        {
            List<DataQualityResponseModel> responseList = new List<DataQualityResponseModel>();


            var execution = getApiExecution(request.Count);

            responseList = await Task.FromResult(MetricsRepository.InsertDataQualityResult(request, execution));
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseList));
        }

        /// <summary>
        /// Delete data quality result(s) based on parameters provided
        /// </summary>
        /// <returns>A response containing the status of the request</returns>
        [
            HttpDelete,
            Route("quality/results/"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request has one or more invalid parameters.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the status of the request", typeof(DataQualityResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> DeleteDataQualityResultsAsync(DataQualityDeleteModel model)
        {
            Asset asset = null;

            Asset ruleAsset = null;

            Guid? _OwningUid = null;

            #region Model Validation            
            asset = null;

            if ((!model.Uid.HasValue || model.Uid.Value == Guid.Empty) && (!model.OwningAssetUid.HasValue || model.OwningAssetUid.Value == Guid.Empty) && (!model.EvaluatedAssetUid.HasValue || model.EvaluatedAssetUid.Value == Guid.Empty))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Request", "At least one of the following MUST be provided: Uid, OwningAssetUid, EvaluatedAssetUid.");
            }

            if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
            {
                var dataQualityAssetResult = MetricsRepository.GetAssetResultDetailsByUid(model.Uid.Value);

                if (dataQualityAssetResult == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Result not found", String.Format("Result with Uid {0} could not be found.", model.OwningAssetUid));
                }

                if (model.OwningAssetUid.HasValue && model.OwningAssetUid.Value != Guid.Empty && !dataQualityAssetResult.Exists(x => x.AssetUid == model.OwningAssetUid.Value && x.Class == (int)ResultRelationClass.Owns))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "OwningAssetUid Invalid", String.Format(DataQualityErrors.AssetNotValidError, "OwningAssetUid", model.OwningAssetUid));
                }
                else
                {
                    _OwningUid = dataQualityAssetResult.Find(x => x.Class == (int)ResultRelationClass.Owns)?.AssetUid;
                }

                if (model.EvaluatedAssetUid.HasValue && model.EvaluatedAssetUid.Value != Guid.Empty && !dataQualityAssetResult.Exists(x => x.AssetUid == model.EvaluatedAssetUid.Value && x.Class == (int)ResultRelationClass.EvaluatedBy))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "EvaluatedAssetUid Invalid", String.Format(DataQualityErrors.AssetNotValidError, "EvaluatedAssetUid", model.EvaluatedAssetUid));
                }

            }

            if (model.OwningAssetUid.HasValue && model.OwningAssetUid.Value != Guid.Empty)
            {
                ruleAsset = AssetRepository.GetAssetByUID(model.OwningAssetUid.Value);

                if (ruleAsset == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Owning Asset not found", String.Format(DataQualityErrors.AssetNotFoundError, model.OwningAssetUid));
                }
                else if (ruleAsset.AssetType.Class != AssetTypeClass.Rule || ruleAsset.State == State.InActive)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "OwningAssetUid Invalid", String.Format(DataQualityErrors.AssetNotValidError, "OwningAssetUid", model.OwningAssetUid));
                }

                _OwningUid = model.OwningAssetUid;
            }
            else
            {
                if (_OwningUid.HasValue)
                {
                    ruleAsset = AssetRepository.GetAssetByUID(_OwningUid.Value);
                }
            }

            if (model.EvaluatedAssetUid.HasValue && model.EvaluatedAssetUid.Value != Guid.Empty)
            {
                asset = AssetRepository.GetAssetByUID(model.EvaluatedAssetUid.Value);

                if (asset == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Evaluated Asset not found", String.Format(DataQualityErrors.AssetNotFoundError, model.EvaluatedAssetUid));
                }
                else if ((asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset) || asset.State == State.InActive)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "EvaluatedAssetUid Invalid", String.Format(DataQualityErrors.AssetNotValidError, "EvaluatedAssetUid", model.EvaluatedAssetUid));
                }
            }

            if (_OwningUid.HasValue && !Company.HasAssetPermission(ruleAsset.AssetType.Object, ruleAsset.AssetType.ObjectID, Permission.DeleteAsset) && (model.EvaluatedAssetUid != null && !Company.HasAssetPermission(asset.AssetType.Object, asset.AssetType.ObjectID, Permission.DeleteAsset)))
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            if (model.EffectiveDateStart.HasValue && model.EffectiveDateEnd.HasValue && model.EffectiveDateStart > model.EffectiveDateEnd)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Request", String.Format(DataQualityErrors.GreaterThanError, "EffectiveDateStart", "EffectiveDateEnd"));
            }

            if (model.RunDateStart.HasValue && model.RunDateEnd.HasValue && model.RunDateStart > model.RunDateEnd)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Request", String.Format(DataQualityErrors.GreaterThanError, "RunDateStart", "RunDateEnd"));
            }

            #endregion

            List<DataQualityDeleteResponseModel> responseList = new List<DataQualityDeleteResponseModel>();


            var execution = getApiExecution(1);

            responseList = await Task.FromResult(MetricsRepository.DeleteDataQualityResult(new List<DataQualityDeleteModel> { model }, execution));
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseList.FirstOrDefault()));
        }

        /// <summary>
        /// Update data quality result(s) for an asset / Rule
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// </remarks>
        /// <returns>A list of data quality results including any error messages.</returns>
        [
            HttpPut,
            Route("quality/results/"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the Uid of the data quality result.", typeof(List<DataQualityResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> PutDataQualityResultAsync(List<DataQualityUpdateModel> request)
        {
            List<DataQualityResponseModel> responseList = new List<DataQualityResponseModel>();

            var execution = getApiExecution(request.Count);

            responseList = await Task.FromResult(MetricsRepository.UpdateDataQualityResult(request, execution));
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseList));
        }

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with the details of the data quality results</returns>
        private SLDocument CreateResponseDocument(core.entities.Metric.DataQualityResult dataQualityResult)
        {
            SLDocument doc = new SLDocument();
            doc.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Results");

            #region Create the list sheet

            #region Header

            int index = 1;
            int rowNumber = 1;
            doc.SetCellValue(rowNumber, index++, "ResultUid");
            doc.SetCellValue(rowNumber, index++, "OwningAssetUid");
            doc.SetCellValue(rowNumber, index++, "EvaluatedAssetUid");
            doc.SetCellValue(rowNumber, index++, "EffectiveDate");
            doc.SetCellValue(rowNumber, index++, "RunDate");
            doc.SetCellValue(rowNumber, index++, "PassCount");
            doc.SetCellValue(rowNumber, index++, "FailCount");
            doc.SetCellValue(rowNumber, index++, "Passed");

            #endregion
            #region Body
            foreach (var row in dataQualityResult.items)
            {
                index = 1;
                rowNumber++;
                doc.SetCellValue(rowNumber, index++, row.ResultUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.OwningAssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.EffectiveDate.ToString());
                doc.SetCellValue(rowNumber, index++, row.RunDate.ToString());
                doc.SetCellValue(rowNumber, index++, row.PassCount);
                doc.SetCellValue(rowNumber, index++, row.FailCount);
                doc.SetCellValue(rowNumber, index++, row.Passed);
            }
            doc.AutoFitColumn(1, 8);
            #endregion
            #endregion
            return doc;
        }
    }
}
