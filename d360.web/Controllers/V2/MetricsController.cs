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

            if (string.IsNullOrEmpty(model.Name))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You are have provided an invalid name.");
            }

            if (model.Weight == 0)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You must supply a weight greater than 0.");
            }

            if (model.IsGroup && model.Conditions.Count > 0)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "Groups should not have conditions.");
            }


            if (model.Conditions.Any(x => x.FieldTypeID <= 0))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "FieldTypeID must be greater than 0.");
            }

            var isNew = true;

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
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to retrieve the metric heirarchy for this asset type.")));

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
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/pointbreakdown"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(MetricAssetHierarchyModels)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetAsync(Guid assetUid, DateTime? effectiveDate = null)
        {
            /*
                         declare @effectiveDate date = '10/3/2018',
                                @assetTypeUid uniqueidentifier = '8371C4C6-E17E-4620-BA8B-AE0301966E0E',
                                @assetUid uniqueidentifier = '5DFA86D6-9DFE-4BB6-B417-F75E3BC9E095';
            */
            var prefix = "Metrics.GetMetricHierarchyByAssetAsync => ";

            try
            {
                var asset = AssetRepository.GetAssetByUID(assetUid);

                if (asset == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {assetUid} could not be found."));

                var result = MetricsRepository.GetMetricHierarchyByAsset(assetUid, effectiveDate);

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

                List<string> fragments = MetricsRepository.GetMetricStructureFragments(assetTypeUid);

                models = JsonConvert.DeserializeObject<List<MetricAssetViewModel>>(string.Join("", fragments));

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
        /// Adds one or more metric results for processing and scoring.
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
        /// <returns>Calculated scores.</returns>
        [
            HttpGet,
            Route("{assetTypeUid}/scores"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding calculated scores.", typeof(MetricScoreApiModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric score is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateStart", "Effective start date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateEnd", "Effective end date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The specific Uid of the asset you want the score for.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("CustomField ", "Any custom non-computed field defined on the asset type.", DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetMetricScores(string assetTypeUid)
        {
            var prefix = "Metrics.GetMetricScores => ";
            
            try
            {
                Guid atUid = Guid.Parse(assetTypeUid);

                if (atUid == null || atUid == Guid.Empty)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", $"Invalid asset type uid '{assetTypeUid}'.");
                }
                AssetType assetType = AssetRepository.GetAssetTypeByUID(atUid);
                if (assetType == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset type with Uid of {atUid.ToString()} not found.");
                }
                var queryParams = Request.GetQueryNameValuePairs();
                var result = MetricsRepository.GetMetricScore(assetType, queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            catch(Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }


        }



    }
}
