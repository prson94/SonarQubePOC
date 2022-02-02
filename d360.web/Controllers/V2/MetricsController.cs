using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
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
using d360.core.queue;
using d360.model.helpers.filters;

namespace d360.web.Controllers.V2
{
    #region Swagger Example For Endpoints Below

    public class UpsertAsset_Example : IExamplesProvider
    {
        public object GetExamples()
        {
            return new MetricAssetEditModel
            {
                AllocationUid = Guid.Empty,
                Definition = new MetricAssetDefinitionViewModel
                {
                    DataQuality = new MetricAssetDefinitionDataQualityViewModel
                    {
                        FilterMatchType = MetricMatchType.All,
                        Filters = new List<MetricAssetDefinitionDataQualityFilterViewModel>() {
                      new MetricAssetDefinitionDataQualityFilterViewModel {
                       AssetTypeUid = Guid.Empty,
                       FieldTypeName = "Dimension",
                       Operator = Operator.Equals,
                       Values = new List<string>() { "Accuracy" }
                      }
                     },
                        ResultOperation = MetricRuleResultOperation.Average,
                        ResultPathUid = Guid.Empty
                    },
                    Governance = new MetricAssetDefinitionGovernanceViewModel
                    {
                        Check = MetricGovernanceCheckType.External,
                        External = new MetricAssetDefinitionGovernanceExternalViewModel
                        {
                            Instructions = "Technical instructions to be consumed by a third party calculation engine.",
                            UpdateFrequency = MetricUpdateFrequency.None
                        },
                        Field = new MetricAssetDefinitionGovernanceFieldViewModel
                        {
                            FieldTypeName = "FieldApiName",
                            Operator = Operator.NotEquals,
                            Values = new List<string>() { "Country" }
                        },
                        Owner = new MetricAssetDefinitionGovernanceOwnerViewModel
                        {
                            ResponsibilityTypeUid = Guid.Empty
                        },
                        Predicate = new MetricAssetDefinitionGovernancePredicateViewModel
                        {
                            Operator = Operator.Equals,
                            PredicateUid = Guid.Empty
                        },
                        Relation = new MetricAssetDefinitionGovernanceRelationViewModel
                        {
                            IntersectTypeUid = Guid.Empty,
                            Operator = Operator.Equals,
                            Values = new List<string>() { Guid.Empty.ToString() }
                        }
                    }
                },
                Name = "My measure display name",
                Description = "A friendly description of the purpose and definition of this measure.",
                EffectiveDate = DateTime.UtcNow.Date,
                IsGroup = false,
                MatchConditionsOnly = true,
                Weight = 0.25M,
                Threshold = (float?)0.999,
                ConditionGroups = new List<MetricAssetVersionConditionViewModel>() {
                    new MetricAssetVersionConditionViewModel {
                     MatchType = MetricMatchType.Any,
                      Position = 1,
                      Weight = 0.45M,
                      Threshold = 0.78,
                      ConditionItems = new List<MetricAssetVersionConditionItemViewModel>(){
                       new MetricAssetVersionConditionItemViewModel {
                        ConditionType = MetricConditionType.And,
                        Operator = Operator.Equals,
                        ConditionFieldTypeName = "Name",
                        Values = new List<string>(){ "An asset name" }
                       }
                      }
                    }
                }
            };
        }
    }

    #endregion

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
        IScoringRepository ScoringRepository;

        public MetricsController(ICoreComponentSet set, IQueueSource queueSource, IScoringRepository scoringRepository, IMetricsRepository metricsRepository, IAssetRepository assetRepository)
            : base(set)
        {
            QueueSource = queueSource;
            this.ScoringRepository = scoringRepository;
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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(MetricAssetViewDetailModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult GetAssetById(Guid uid)
        {
            try
            {
                var model = MetricsRepository.GetMetricViewModelByUid(uid, null);

                if (model == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, MetricsApiMessages.Errorlocatingmetric, string.Format(MetricsApiMessages.MetricUidNotFound, uid.ToString()));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /*
         Enum operators to be used in a later sprint.
        - Equals (1)
        - NotEquals (2)
        - Contains (3)
        - NotContains (4)
        - StartsWith (5)
        - EndsWith (6)
        - Before (7)
        - After (8)
        - Between (9)
        - Populated (10)
        - NotPopulated (11)
        - GreaterThan (12)
        - LessThanOrEquals (13)
        - LessThan (14)
        - GreaterThanOrEquals (15)
        - In (16)
        - NotIn (17)
        - IsTrue (18)
        - IsFalse (19)         
         */
        /// <summary>
        /// Add or updates a metric.
        /// </summary>
        /// <remarks>
        /// When creating or updating a measure, under the Definition:  
        /// - DataQuality
        ///     - Be sure to remove the Governance child property under Definition.
        ///     - FilterMatchType may be: "Any" or "All"
        ///     - ResultOperation may be: "Average", "Minimum" or "Maximum" 
        ///     - ResultPathUid should be a valid identifier that can be retrieved from the path options endpoint (_{assetTypeUid:Guid}/{scoreType}/pathoptions_) on this service.
        ///     - Filters is a list of fields to filter by, with the asset types being those contained within the path option selected above.
        /// - Governance
        ///     - Be sure to remove the DataQuality child property under Definition.    
        ///     - Check may be: "External", "Field", "Owner", "Predicate", or "Relation"
        ///     - Based on the check selected above, you must provide the child property that has the same name.
        /// 
        /// Whenever you see an Operator property, the possible values are:
        /// - Equals
        /// - NotEquals
        /// - GreaterThan
        /// - LessThanOrEquals
        /// - LessThan
        /// - GreaterThanOrEquals
        /// - Before
        /// - After
        /// - OnOrAfter
        /// - OnOrBefore
        /// 
        /// For an up-to-date list of operators as well as the rules of use, please see the operator endpoint at (_/api/v2/environment/operators_).
        /// </remarks>
        /// <param name="model">The definition of the metric itself. If updating an existing metric, ensure that you populate the Uid property.</param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(MetricAssetEditModel), typeof(UpsertAsset_Example)),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the ADD request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the UPDATE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not autheorized to make this change.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate what was incorrect about your request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that either your metric or parent metric was not found.", typeof(ErrorResponse))
        ]
        public IHttpActionResult UpsertAsset(MetricAssetEditModel model)
        {
            #region Validation

            var errorTitle = "Error updating measure";

            try
            {
                if (model.ParentUid == Guid.Empty)
                {
                    model.ParentUid = null;
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    throw new WorkStatusException(HttpStatusCode.Unauthorized, MetricsApiMessages.NotAllowUpdateMetric);
                }

                if (string.IsNullOrEmpty(model.Name) || (model.Name + "").Trim() == "")
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.NameNotEmpty);
                }

                if (model.Description != null)
                {
                    if (model.Description?.Length > 4000)
                    {
                        throw new WorkStatusException(HttpStatusCode.BadRequest, string.Format(MetricsApiMessages.DescriptionLengthValidation, model.Description.Length));
                    }
                }

                if (model.ParentUid.HasValue && model.IsGroup)
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, string.Format(Validation.MaxLevelForMeasure, 2));
                }

                if (model.AllocationUid == Guid.Empty)
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.NoAllocationAssetTypeScoreType);
                }

                var allocation = Company.GetByUid<MetricAllocation>(model.AllocationUid);
                if (allocation == null)
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.NoAllocationForUid);
                }
                else
                {
                    model.Allocation = allocation;
                }

                if (!model.Allocation.IsExternallyCalculated && model.Weight <= 0 || model.Weight > 1)
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.WeightDecimalValidation);
                }

                if (model.ParentUid != null && model.ParentUid != Guid.Empty)
                {
                    var parent = MetricsRepository.GetMetricByUid(model.ParentUid.Value);

                    if (parent == null)
                    {
                        throw new WorkStatusException(HttpStatusCode.NotFound, MetricsApiMessages.ParentMetricNotFound);
                    }

                    if (!parent.IsGroup)
                    {
                        throw new WorkStatusException(HttpStatusCode.BadRequest,MetricsApiMessages.IsGroupTrueParentMetric);
                    }

                    if (model.IsGroup || parent.ParentUid != null)
                    {
                        throw new WorkStatusException(HttpStatusCode.BadRequest, string.Format(Validation.MaxLevelForMeasure, 2));
                    }
                }

                #region Set the default for condition group, item, and value arrays.

                if (model.ConditionGroups == null)
                {
                    model.ConditionGroups = new List<MetricAssetVersionConditionViewModel>();
                }
                model.ConditionGroups.ForEach(g =>
                {
                    if (g.ConditionItems == null)
                    {
                        g.ConditionItems = new List<MetricAssetVersionConditionItemViewModel>();
                    }
                    g.ConditionItems.ForEach(i =>
                    {
                        if (i.Values == null)
                        {
                            i.Values = new List<string>();
                        }
                    });
                });

                model.ConditionGroups.RemoveAll(g => g.ConditionItems.Count == 0); // Remove empty groups.

                #endregion

                if (model.IsGroup && model.ConditionGroups.Count > 0)
                {
                    throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.GroupNotHaveCondition);
                }

                foreach (var cond in model.ConditionGroups)
                {
                    foreach (var item in cond.ConditionItems)
                    {
                        if (!string.IsNullOrEmpty(item.ConditionFieldTypeName) && item.ConditionIntersectTypeUid.HasValue)
                        {
                            throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.UseSingleCondition);
                        }
                        else if (string.IsNullOrEmpty(item.ConditionFieldTypeName) && !item.ConditionIntersectTypeUid.HasValue)
                        {
                            throw new WorkStatusException(HttpStatusCode.BadRequest,MetricsApiMessages.ConditionNotEmpty);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(item.ConditionFieldTypeName) || string.IsNullOrWhiteSpace(item.ConditionFieldTypeName))
                            {
                                throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.ConditionFieldTypeNameNotEmpty);
                            }

                            if (item.ConditionIntersectTypeUid.HasValue && item.ConditionIntersectTypeUid != Guid.Empty)
                            {
                                throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.ConditionIntersectTypeUidNotValid);
                            }
                        }

                        if (item.Values != null)
                        {
                            if (item.Values.Any(v => !string.IsNullOrEmpty(v) && v.Length > 250))
                            {
                                throw new WorkStatusException(HttpStatusCode.BadRequest, MetricsApiMessages.ConditionValueMaxChar250);
                            }
                        }
                    }
                }
            }
            catch (WorkStatusException ex)
            {
                return errorMessageResponse(ex.Status, errorTitle, ex.Message);
            }

            #endregion Validation

            var result = MetricsRepository.AddOrUpdateMetrics(model);

            if (!result.StatusCode.In(HttpStatusCode.OK, HttpStatusCode.Created))
            {
                return errorMessageResponse(result.StatusCode, result.Error, result.Message);
            }

            var isNew = (result.StatusCode == HttpStatusCode.Created);
            return successMessageResponse(
                    result.StatusCode,
                    $"{(isNew ? MetricsApiMessages.MetricAdded : MetricsApiMessages.MetricUpdated)}.",
                    $"{(isNew ? MetricsApiMessages.MetricAddedSuccessfully : MetricsApiMessages.MetricUpdatedSuccessfully)}."
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
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, MetricsApiMessages.MetricRemoveNotAllowed));
            }

            MetricAsset model = MetricsRepository.GetActiveMetric(uid);

            if (model == null)
            {
                return errorMessageResponse(HttpStatusCode.NotFound, MetricsApiMessages.ErrorRemoveMetric, MetricsApiMessages.MetricNotFound);
            }

            MetricsRepository.DeleteMetric(model);

            return successMessageResponse(HttpStatusCode.OK, MetricsApiMessages.MetricRemoved, MetricsApiMessages.MetricRemoveMessage);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true), Obsolete
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetTypeAsync(Guid assetTypeUid, DateTime? effectiveDate = null)
        {
            var prefix = "Metrics.GetMetricHierarchyByAssetTypeAsync => ";

            try
            {
                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()))).ConfigureAwait(false);
                }

                var result = MetricsRepository.GetMetricDefinitionHierarchyByAssetType(assetTypeUid, effectiveDate);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of paths (asset types and relationship types) that act as options when configuring a measure for various score types.
        /// </summary>
        /// <remarks>
        /// Some score types may never have path options.
        /// </remarks>
        /// <param name="assetTypeUid">The Uid of the asset type.</param>
        /// <param name="scoreType">The scoreType to be returned.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/{scoreType}/pathoptions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(List<MetricPathOptionViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetMetricPathOptionsBy(Guid assetTypeUid, ScoreType scoreType)
        {
            var prefix = "Metrics.GetMetricPathOptionsBy => ";

            try
            {
                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound,ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()))).ConfigureAwait(false);
                }

                var results = await MetricsRepository.GetMetricPathOptionsBy(assetType.ID, scoreType);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of fields for each asset type within the path (with one exception) that act as filters for rule results to include as part of the measure calculation in a score.
        /// </summary>
        /// <remarks>
        /// The list of fields for this rule result path will NOT include the starting asset type. Those fields would not be included as result filter fields, instead using measure conditions.
        /// </remarks>
        /// <param name="ruleResultPathUid">The Uid of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("pathoptions/{ruleResultPathUid:Guid}/fields"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(List<MetricPathOptionViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetFieldsByRuleResultPath(Guid ruleResultPathUid)
        {
            var prefix = "Metrics.GetFieldsByRuleResultPath => ";

            try
            {
                var results = await MetricsRepository.GetFieldsByRuleResultPath(ruleResultPathUid);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(List<RootMetricAssetHierarchyModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("effectiveDate", "The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past effective date.", DataType = "string", ParameterType = "query", Required = false),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetAndScoreTypeAsync(ScoreType scoreType, Guid assetUid)
        {
            var prefix = "Metrics.GetMetricHierarchyByAssetAndScoreTypeAsync => ";

            try
            {
                DateTime effectiveDate = DateTime.MinValue;
                var param = Request.GetQueryNameValuePairs();
                if (param.Any(x => x.Key.ToLower() == "effectivedate"))
                {
                    var value = param.FirstOrDefault(x => x.Key.ToLower() == "effectivedate").Value;
                    if (!DateTime.TryParse(value, out effectiveDate))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, MetricsApiMessages.InvalidEffectiveDate)).ConfigureAwait(false);
                    }
                }
                else
                {
                    effectiveDate = DateTime.UtcNow;
                }

                var assetDetail = Company.Filter<AssetDetail>(i => i.uid == assetUid).FirstOrDefault();
                if (assetDetail == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, MetricsApiMessages.AssetIdentifierNotFound)).ConfigureAwait(false);
                }

                var allocation = Company.Filter<MetricAllocation>(al =>
                    al.AssetTypeUid == assetDetail.AssetTypeUid &&
                    al.ScoreType == scoreType &&
                    string.IsNullOrEmpty(al.OverrideName)
                    ).FirstOrDefault();

                if (allocation == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(MetricsApiMessages.ScoreAllocationCorrespondingAssetNotFound, assetUid.ToString()))).ConfigureAwait(false);
                }

                var result = MetricsRepository.GetMetricHierarchyByAsset(allocation.Uid, assetUid, effectiveDate);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a hierarchical structure of metrics associated with the asset Uid provided, for a given effective date. If no effective date is provided, today's date is used.
        /// </summary>
        /// <param name="allocationUid">The allocation to be returned.</param>
        /// <param name="assetUid">The Uid of the asset.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{allocationUid}/assets/{assetUid}/pointbreakdown"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset based on the provided Uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(List<RootMetricAssetHierarchyModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("effectiveDate", "The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past effective date.", DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetAndAllocationAsync(string allocationUid, string assetUid)
        {
            var prefix = "Metrics.GetMetricHierarchyByAssetAndAllocationAsync => ";

            try
            {
                Guid _allocationUid;
                Guid _assetUid;

                var allocationStatus = validateScoreAllocation(allocationUid, out _allocationUid);
                if (allocationStatus.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(allocationStatus.StatusCode,ApiMessages.BadRequest, allocationStatus.Message)).ConfigureAwait(false);
                }

                var assetStatus = validateAsset(assetUid, Permission.ReadAsset, out _assetUid);
                if (assetStatus.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(assetStatus.StatusCode, "Bad request", assetStatus.Message)).ConfigureAwait(false);
                }

                DateTime effectiveDate = DateTime.MinValue;
                var param = Request.GetQueryNameValuePairs();
                if (param.Any(x => x.Key.ToLower() == "effectivedate"))
                {
                    var value = param.FirstOrDefault(x => x.Key.ToLower() == "effectivedate").Value;
                    if (!DateTime.TryParse(value, out effectiveDate))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.BadRequest, MetricsApiMessages.InvalidEffectiveDate)).ConfigureAwait(false);
                    }
                }
                else
                {
                    effectiveDate = DateTime.UtcNow;
                }

                var result = MetricsRepository.GetMetricHierarchyByAsset(_allocationUid, _assetUid, effectiveDate);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetMetricFieldsByAssetType(Guid assetTypeUid)
        {
            try
            {
                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);
                if (!Company.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ApiMessages.FieldNotAllowedForAssetType));
                }

                var models = MetricsRepository.GetMetricConditionsFields(assetTypeUid);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models ?? new List<MetricFieldTypeViewModel>()));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError($"Metrics.GetMetricFieldsByAssetType => {errorMessage}");
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
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<InternalScoreResultApiRequestModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostBulkMetricsToStagingAsync(List<InternalScoreResultApiRequestModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.Forbidden, ApiMessages.EndpointNotAuthorizedMessage);

                if (model == null || model.Count < 1)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage));

                var execution = getApiExecution(model.Count);
                return ResponseMessage(
                    Request.CreateResponse(
                        HttpStatusCode.OK,
                        ScoringRepository.PostScoreResults(ScoreType.Governance, execution, model)
                    )
                );
            }
            catch (GenericException ex)
            {
                return errorMessageResponse(ex.StatusCode, ex.StatusMessage, ex.StatusDescription);
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, ApiMessages.UnknownErrorInvestigatingMessage);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateStart", "Effective start date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateEnd", "Effective end date", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The specific Uid of the asset you want the score for.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_allocationUid", "The specific Uid of the measure / asset type allocation you want scores for. When using this query parameter, ensure that you are not also using the _scoreType parameter.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_scoreType", "The type of scores. The default is Governance. When using this query parameter, ensure that you are not also using the _allocationUid parameter.", DataType = "string", ParameterType = "query", Required = false, Enum = typeof(ScoreType))
        ]
        public async Task<IHttpActionResult> GetMetricScores(Guid assetTypeUid)
        {
            var prefix = "Metrics.GetMetricScores => ";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);
                if (assetType == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()));
                }

                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                (var result, string errorMessage) = MetricsRepository.GetMetricScore(assetType, queryParams);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, errorMessage);
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a administrative hierarchical structure of metrics associated with the asset Uid provided.
        /// </summary>
        /// <param name="uid">The Uid of the asset.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past or future effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet, Obsolete,
            Route("{uid}/definitionFromAsset"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetUidAsync(Guid uid, string effectiveDate = null)
        {
            var asset = Company.Filter<Asset>(x => x.uid == uid, x => x.AssetType).FirstOrDefault();
            if (asset == null)
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, uid.ToString()));
            }

            DateTime? effDate = null;
            if (!string.IsNullOrEmpty(effectiveDate))
            {
                DateTime edt;
                if (DateTime.TryParse(effectiveDate, out edt))
                {
                    if (edt.Year < 1900 || edt.Year > DateTime.UtcNow.Year)
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.InvalidDateYear, effectiveDate));
                    }

                    effDate = edt;
                }
                else
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.InvalidDate, effectiveDate));
                }
            }

            return await GetMetricHierarchyByAssetTypeAsync(asset.AssetType.uid, effDate);
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
            var model = Company.Query<dynamic>(@"EXEC GetScoreHistoryByObject @assetUid, @type", new { assetUid, type }, ApiTimeout);
            return ResponseMessage(Request.CreateResponse<dynamic>(HttpStatusCode.OK, model));
        }

        /// <summary>
        /// Gets the data quality results for a rule
        /// </summary>
        /// <remarks>
        /// Gets the data quality results for a rule and optionally a specific asset
        ///       
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
        /// 
        /// **Notes:** 
        /// * Read permissions on the rule are required.
        /// * Effective start and end and Run start and end dates can be used as additional parameters when a Rule or Asset ID is provided (OwningAssetUid or EvaluatedAssetUid)
        /// </remarks>
        /// <returns>List of data quality results</returns>
        [
            HttpGet,
            Route("quality/results/"),
            SwaggerParameter("_owningAssetUid", "Rule UID. If no other parameters are specified, all rule results for the rule will be returned", DataType = "string", ParameterType = "query", Required = true),
            SwaggerParameter("_evaluatedAssetUid", "Asset UID.  If provided only rule results for the specified asset will be returned", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for. The default value is 1.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by (Default by Effective Date).", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateStart", "Additional parameter that can be supplied when the Rule or Asset UID is provided.    If provided with no EffectiveDateEnd all results between the EffectiveDateStart and now will be returned.", DataType = "date-time", ParameterType = "query", Required = false),
            SwaggerParameter("_effectiveDateEnd", "Additional parameter that can be supplied when the Rule or Asset UID is provided.    If provided with no EffectiveDateStart all results up until the EffectiveDateEnd will be returned.", DataType = "date-time", ParameterType = "query", Required = false),
            SwaggerParameter("_isFriendlyNameExport", "Additional parameter that can be supplied when doing a file export. If provided response file will replicate format of Result List screen", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeDuplicateFlag", "If True response will include IsDuplicate flag. Defaults to false.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json", "application/vnd.ms-excel", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request has one or more invalid parameters.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of Data Quality Results.", typeof(DataQualityGetResultModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetDataQualityResults()
        {
            var queryParams = Request.GetQueryNameValuePairs();

            Asset asset = null;

            Asset ruleAsset = null;

            Guid _owningAssetUid;
            Guid? _evaluatedAssetUid = null;
            string _order = null;
            string _direction = "desc";
            DateTime? _effectiveDateStart = null;
            DateTime? _effectiveDateEnd = null;
            int _pageSize = 250;
            int _pageNum = 1;
            bool includeDuplicate = false;
            string _filter = null;
            string _simpleFilter = null;

            var isRequestAnExport = Request.Headers.Accept.ToString().Equals("application/octet-stream", StringComparison.InvariantCultureIgnoreCase) || 
                Request.Headers.Accept.ToString().Equals("application/vnd.ms-excel", StringComparison.InvariantCultureIgnoreCase);

            #region Model Validation
            if (queryParams.Any(q => q.Key == "_owningAssetUid"))
            {
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_owningAssetUid").Value, out _owningAssetUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(MetricsApiMessages.CustomUidNotValid, "OwningAssetUid", queryParams.ToList().FirstOrDefault(q => q.Key == "_owningAssetUid").Value))).ConfigureAwait(false);
                }

                ruleAsset = AssetRepository.GetAssetByUID(_owningAssetUid);

                if (ruleAsset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, _owningAssetUid.ToString()))).ConfigureAwait(false);
                }
                else if (ruleAsset.AssetType.Class != AssetTypeClass.Rule)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(MetricsApiMessages.CustomUidNotValid, "OwningAssetUid", _owningAssetUid));
                }
            }
            else
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(MetricsApiMessages.CustomRequiredParameter, "_owningAssetUid"))).ConfigureAwait(false);
            }

            if (queryParams.Any(q => q.Key == "_evaluatedAssetUid"))
            {
                Guid tempEvaluatedUid;
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_evaluatedAssetUid").Value, out tempEvaluatedUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(MetricsApiMessages.CustomUidNotValid, "EvaluatedAssetUid", queryParams.ToList().FirstOrDefault(q => q.Key == "_evaluatedAssetUid").Value))).ConfigureAwait(false);
                }

                _evaluatedAssetUid = tempEvaluatedUid;

                asset = AssetRepository.GetAssetByUID(_evaluatedAssetUid.Value);

                if (asset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, _evaluatedAssetUid.Value.ToString()))).ConfigureAwait(false);
                }
                else if (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.BadRequest, string.Format(MetricsApiMessages.CustomNotValid, "EvaluatedAssetUid", _evaluatedAssetUid.Value))).ConfigureAwait(false);
                }
            }


            if (!Company.HasAssetPermission(ruleAsset.AssetType.Object, ruleAsset.AssetType.ObjectID, Permission.ReadAsset))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                _order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value;
                List<string> _orderColumns = new List<string>() { "ResultUid", "EvaluatedAssetUid", "OwningAssetUid", "EvaluatedAssetPath", "EvaluatedAssetClass", "EffectiveDate", "EvaluatedAssetTypePath", "RunDate", "Passcount", "FailCount", "PassFraction", "TotalCount" };
                if (_orderColumns.FindIndex(x => x.Equals(_order, StringComparison.InvariantCultureIgnoreCase)) == -1)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomOrderMessage, "_order", _order, string.Join(",", _orderColumns.ToArray())));
                }
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                _direction = queryParams.ToList().FirstOrDefault(q => q.Key == "_direction").Value;
                if (!_direction.Equals("asc", StringComparison.InvariantCultureIgnoreCase) && !_direction.Equals("desc", StringComparison.InvariantCultureIgnoreCase))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter,  ApiMessages.InvalidDirection);
                }
            }

            if (queryParams.Any(q => q.Key == "_effectiveDateStart"))
            {
                DateTime _tempEffectiveDateStart;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_effectiveDateStart").Value, out _tempEffectiveDateStart))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_effectiveDateStart"));
                }
                _effectiveDateStart = _tempEffectiveDateStart;

                if (_effectiveDateStart == DateTime.MinValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_effectiveDateStart"));
                }
            }

            if (queryParams.Any(q => q.Key == "_effectiveDateEnd"))
            {
                DateTime _tempEffectiveDateEnd;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_effectiveDateEnd").Value, out _tempEffectiveDateEnd))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_effectiveDateEnd"));
                }
                _effectiveDateEnd = _tempEffectiveDateEnd;
                if (_effectiveDateEnd == DateTime.MinValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_effectiveDateEnd"));
                }
                if (_effectiveDateStart != null && _effectiveDateEnd < _effectiveDateStart)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomStartEndDateMessage, "_effectiveDateEnd", "_effectiveDateStart"));
                }
            }

            if (queryParams.Any(q => q.Key.ToLower() == "_includeduplicateflag"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includeduplicateflag").Value, out includeDuplicate))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_includeDuplicateFlag"));
                }
            }

            if (queryParams.Any(q => q.Key.ToLower() == "_filter"))
            {
                _filter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                if (string.IsNullOrEmpty(_filter))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_filter"));
                }
            }

            if (queryParams.Any(q => q.Key.ToLower() == "_simplefilter"))
            {
                _simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value;
                if (string.IsNullOrEmpty(_simpleFilter))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(MetricsApiMessages.CustomNotValid, "_simpleFilter"));
                }
            }

            string isValid = isPageSizeAndNumValid(queryParams, isRequestAnExport);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
            }

            if (isRequestAnExport)
            {
                _pageNum = 1;
                _pageSize = SettingsRepository.GetSettingValue<int>(Setting.MaxExcelExportRows);
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
                DataQualityGetResultModel dataQualityResult = new DataQualityGetResultModel();

                dataQualityResult = await Task.FromResult(MetricsRepository.GetDataQualityResults(_owningAssetUid, _evaluatedAssetUid, _pageSize, _pageNum, _order, _direction, _effectiveDateStart, _effectiveDateEnd, includeDuplicate, _filter, _simpleFilter)).ConfigureAwait(false);

                if (isRequestAnExport)
                {
                    SLDocument document = new SLDocument();
                    bool isExport = false;
                    if (bool.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_isFriendlyNameExport").Value, out isExport) && isExport)
                    {
                        document = CreateResponseDocumentForExport(dataQualityResult);
                    }
                    else
                    {
                        document = CreateResponseDocument(dataQualityResult);

                    }

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
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.ErrorFilterExpressionParse, errorMessage)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }



        /// <summary>
        /// Create the data quality result for an asset / Rule
        /// </summary>
        /// <remarks>
        ///
        /// The endpoint creates rule results for a specific rule and optional asset
        ///###Rules###
        /// <table>
        /// <tr><td>**Field**</td><td>**Required / Optional**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>OwningAssetUid</td><td>Required</td><td>UID of the Rule in which to post the results to</td><td>Must be a valid Rule UID</td></tr>
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Used to identify the request. One can be provided but if not, one will be generated</td><td>If provided must be in the correct format</td></tr>
        /// <tr><td>EvaluatedAssetUid</td><td>Optional</td><td>Asset UID  of the asset that the result is for</td><td>Must be valid Business or Technical Asset UID</td></tr>
        /// <tr><td>EffectiveDate</td><td>Required</td><td>Effective date of the rule result</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>RunDate</td><td>Required</td><td>Run date of the rule result</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>PassCount</td><td>Required</td><td>Number of rows that passed the rule</td><td>Must be greater than or equal to zero</td></tr>
        /// <tr><td>FailCount</td><td>Required</td><td>Number of rows that failed the rule</td><td>Must be greater than or equal to zero</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:** 
        /// * Edit permissions on the rule are required.
        /// 
        /// </remarks>
        /// <returns>A list of data quality results including any error messages.</returns>
        [
            HttpPost,
            Route("quality/results/"),
            SwaggerRequestExample(typeof(DataQualityInsertModel), typeof(DataQualityInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the Uid of the new data quality result.", typeof(List<DataQualityResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostDataQualityResultAsync(List<DataQualityInsertModel> request)
        {
            List<DataQualityResponseModel> responseList = new List<DataQualityResponseModel>();

            var execution = getApiExecution(request.Count);

            responseList = await Task.FromResult(MetricsRepository.InsertDataQualityResult(request, execution)).ConfigureAwait(false);
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseList));
        }

        /// <summary>
        /// Delete data quality result(s) based on parameters provided
        /// </summary>
        /// <remarks>
        /// 
        /// Deletes rules results that match the criteria supplied.
        /// 
        /// This can be used to remove unused or old results that are no longer relevant or can be used to remove rule results loaded in error. 
        /// 
        /// ###Rules###
        /// <table>
        /// <tr><td>**Field**</td><td>**Required/Optional**</td><td>**Description**</td><td>**Validation**</td></tr>        
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Used to identify the request. One can be provided but if not, one will be generated</td><td>If provided must be in the correct format</td></tr>
        /// <tr><td>Uid</td><td>Optional</td><td>Rule Result UID.<br/>If provided alone or with the OwningAssetUid, the rule result will be deleted</td><td>Valid Rule result UID</td></tr>
        /// <tr><td>OwningAssetUid</td><td>Optional</td><td>Rule UID.<br/>If provided without other UID’s all rule results for this rule will be deleted</td><td>Must be a valid Rule UID</td></tr>
        /// <tr><td>EvaluatedAssetUid</td><td>Optional</td><td>Asset UID.  If provided without other UID’s all rule results for this asset will be deleted</td><td>Must be valid Business or Technical Asset UID</td></tr>        
        /// <tr><td>EffectiveDateStart</td><td>Optional</td><td>Additional parameter that can be supplied when the Rule or Asset UID is provided.<br/>If EffectiveDateEnd is not provided all results between the EffectiveDateStart and now will be deleted.</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>EffectiveDateEnd</td><td>Optional</td><td>Additional parameter that can be supplied when the Rule or Asset UID is provided.<br/>If EffectiveDateStart is not provided all results up until the EffectiveDateEnd will be deleted.</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>RunDateStart</td><td>Optional</td><td>Additional parameter that can be supplied when the Rule or Asset UID is provided.<br/>If RunDateEnd is not provided all results between the RunDateStart and now will be deleted.</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>RunDateEnd</td><td>Optional</td><td>Additional parameter that can be supplied when the Rule or Asset UID is provided.<br/>If RunDateStart is not provided all results up until the RunDateEnd will be deleted</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:**
        /// *   Delete permissions on the Rule are required.
        /// *   One of these 3 optional fields must be provided: **Uid**, **OwningAssetUid**, **EvaluatedAssetUid**
        /// *   If more than one of the 3 optional UIDs are provided validation will occur between them.
        /// *   Effective start and end and Run start and end dates can be used as additional parameters when a Rule or Asset ID is provided (OwningAssetUid or EvaluatedAssetUid)
        /// 
        /// ###Example Requests###
        /// Delete a result based on just the result Uid
        /// ```
        /// {
        ///     "Uid": "ff41848c-1118-4870-8ee7-b78dcabf1682"
        /// }
        /// ```
        /// 
        /// 
        /// Delete a result based on the result Uid while validating Rule (OwningAssetUid) is correct.
        /// ```
        /// {
        ///     "Uid": "ff41848c-1118-4870-8ee7-b78dcabf1682",
        ///     "OwningAssetUid": "a1ee2e5b-c531-47dc-a675-9fd28c829c19"
        /// }
        /// ```
        /// 
        /// 
        /// Delete an asset (EvaluatedAssetUid) from all results.
        /// ```
        /// {
        ///     "EvaluatedAssetUid": "8415655e-638b-49e0-97f2-db840199b401"
        /// }
        /// ```
        /// 
        /// 
        /// Delete an asset (EvaluatedAssetUid) from a single result.
        /// ```
        /// {
        ///     "Uid": "ff41848c-1118-4870-8ee7-b78dcabf1682",
        ///     "EvaluatedAssetUid": "8415655e-638b-49e0-97f2-db840199b401"
        /// }
        /// ```
        /// 
        /// 
        /// Delete an asset (EvaluatedAssetUid) from results for a specific rule (OwningAssetUid)
        /// ```
        /// {
        ///     "EvaluatedAssetUid": "8415655e-638b-49e0-97f2-db840199b401",
        ///     "OwningAssetUid": "a1ee2e5b-c531-47dc-a675-9fd28c829c19"
        /// }
        /// ```
        /// 
        /// 
        /// Delete all results for a given rule (OwningAssetUid) between given effective start and end dates
        /// ```
        /// {
        ///     "OwningAssetUid": "a1ee2e5b-c531-47dc-a675-9fd28c829c19",
        ///     "EffectiveDateStart": "2020-04-15",
        ///     "EffectiveDateEnd": "2020-04-30"
        /// }
        /// ```
        /// 
        /// 
        /// Delete an asset (EvaluatedAssetUid) from all results after a given run date
        /// ```
        /// {
        ///     "EvaluatedAssetUid": "8415655e-638b-49e0-97f2-db840199b401",
        ///     "RunDateStart": "2020-04-15 11:27:33"
        /// }
        /// ```
        /// 
        /// 
        /// Delete an asset (EvaluatedAssetUid) from all results after a given effective date and between given run start and end dates
        /// ```
        /// {
        ///     "EvaluatedAssetUid": "8415655e-638b-49e0-97f2-db840199b401",
        ///     "EffectiveDateStart": "2020-04-15",
        ///     "RunDateStart": "2020-04-15 11:27:33",
        ///     "RunDateEnd": "2020-04-29 12:55:21"
        /// }
        /// ```
        /// 
        /// </remarks>
        /// <returns>A response containing the status of the request</returns>
        [
            HttpDelete,
            Route("quality/results/"),
            SwaggerRequestExample(typeof(DataQualityDeleteModel), typeof(DataQualityDeleteExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request has one or more invalid parameters.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the status of the request", typeof(DataQualityResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteDataQualityResultsAsync(DataQualityDeleteModel model)
        {
            Asset asset = null;

            Asset ruleAsset = null;

            Guid? _OwningUid = null;

            #region Model Validation            
            asset = null;

            DateTime runDateStart = new DateTime();
            DateTime effectiveDateStart = new DateTime();

            if ((!model.Uid.HasValue || model.Uid.Value == Guid.Empty) && (!model.OwningAssetUid.HasValue || model.OwningAssetUid.Value == Guid.Empty) && (!model.EvaluatedAssetUid.HasValue || model.EvaluatedAssetUid.Value == Guid.Empty))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(MetricsApiMessages.CustomAtLeastOneProvided, "Uid, OwningAssetUid, EvaluatedAssetUid"));
            }

            if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
            {
                var dataQualityAssetResult = MetricsRepository.GetAssetResultDetailsByUid(model.Uid.Value);

                if (dataQualityAssetResult == null || dataQualityAssetResult.Count == 0)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound,ApiMessages.NotFound, String.Format(MetricsApiMessages.CustomUidNotFound, "Result", model.Uid.Value));
                }

                if (model.OwningAssetUid.HasValue && model.OwningAssetUid.Value != Guid.Empty && !dataQualityAssetResult.Exists(x => x.AssetUid == model.OwningAssetUid.Value && x.Class == (int)ResultRelationClass.Owns))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.AssetNotValidError, "OwningAssetUid", model.OwningAssetUid));
                }
                else
                {
                    _OwningUid = dataQualityAssetResult.Find(x => x.Class == (int)ResultRelationClass.Owns)?.AssetUid;
                }

                if (model.EvaluatedAssetUid.HasValue && model.EvaluatedAssetUid.Value != Guid.Empty && !dataQualityAssetResult.Exists(x => x.AssetUid == model.EvaluatedAssetUid.Value && x.Class == (int)ResultRelationClass.EvaluatedBy))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.AssetNotValidError, "EvaluatedAssetUid", model.EvaluatedAssetUid));
                }

            }

            if (model.OwningAssetUid.HasValue && model.OwningAssetUid.Value != Guid.Empty)
            {
                ruleAsset = AssetRepository.GetAssetByUID(model.OwningAssetUid.Value);

                if (ruleAsset == null)
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, String.Format(DataQualityErrors.AssetNotFoundError, model.OwningAssetUid));
                }
                else if (ruleAsset.AssetType.Class != AssetTypeClass.Rule || ruleAsset.State == State.InActive)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.NotFound, String.Format(DataQualityErrors.AssetNotValidError, "OwningAssetUid", model.OwningAssetUid));
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
                    return errorMessageResponse(HttpStatusCode.NotFound, MetricsApiMessages.EvalAssetNotFound, String.Format(DataQualityErrors.AssetNotFoundError, model.EvaluatedAssetUid));
                }
                else if ((asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset) || asset.State == State.InActive)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, MetricsApiMessages.EvalAssetInvalid, String.Format(DataQualityErrors.AssetNotValidError, "EvaluatedAssetUid", model.EvaluatedAssetUid));
                }
            }

            if (_OwningUid.HasValue && !Company.HasAssetPermission(ruleAsset.AssetType.Object, ruleAsset.AssetType.ObjectID, Permission.DeleteAsset))
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            if (model.EffectiveDateStart != null && !DateTime.TryParseExact(model.EffectiveDateStart,
                                   "yyyy-MM-dd",
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.None,
                                   out effectiveDateStart))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateStart", "yyyy-MM-dd"));
            }

            if (model.EffectiveDateEnd != null)
            {
                DateTime effectiveDateEnd;
                if (!DateTime.TryParseExact(model.EffectiveDateEnd,
                                   "yyyy-MM-dd",
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.None,
                                   out effectiveDateEnd))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateEnd", "yyyy-MM-dd"));
                }
                else if (effectiveDateStart > effectiveDateEnd)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.GreaterThanError, "EffectiveDateStart", "EffectiveDateEnd"));
                }

            }

            if (model.RunDateStart != null && !DateTime.TryParseExact(model.RunDateStart,
                                   "yyyy-MM-dd HH:mm:ss",
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.None,
                                   out runDateStart))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.InvalidFormatError, "RunDateStart", "yyyy-MM-dd HH:mm:ss"));
            }

            if (model.RunDateEnd != null)
            {
                DateTime runDateEnd;
                if (!DateTime.TryParseExact(model.RunDateEnd,
                                   "yyyy-MM-dd HH:mm:ss",
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.None,
                                   out runDateEnd))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.InvalidFormatError, "RunDateEnd", "yyyy-MM-dd HH:mm:ss"));
                }
                else if (runDateStart > runDateEnd)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, String.Format(DataQualityErrors.GreaterThanError, "RunDateStart", "RunDateEnd"));
                }
            }

            #endregion

            var execution = getApiExecution(1);
            var responseList = await Task.FromResult(MetricsRepository.DeleteDataQualityResult(new List<DataQualityDeleteModel> { model }, execution)).ConfigureAwait(false);
            if (responseList == null)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, MetricsApiMessages.ErrorRuleResult, ApiMessages.UnknownErrorInvestigatingMessage);
            }

            var responseModel = responseList.FirstOrDefault();
            if (responseModel != null)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseModel));
            }
            else
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, MetricsApiMessages.ErrorRuleResult, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Update data quality result(s) for an asset / Rule
        /// </summary>
        /// <remarks>
        /// The endpoint can update various fields on a rule result.
        /// 
        /// <table>
        /// <tr><td>**Field**</td><td>**Required / Optional**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>Uid</td><td>Required</td><td>Rule Result UID</td><td>Valid Rule result UID</td></tr>
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Used to identify the request. One can be provided but if not, one will be generated</td><td>If provided must be in the correct format</td></tr>
        /// <tr><td>EvaluatedAssetUid</td><td>Optional</td><td>Provide a valid Business or Technical Asset UID to update an existing rule result.<br/>This will either add or update the asset on the rule result.</td><td>Must be valid Business or Technical Asset UID</td></tr>        
        /// <tr><td>RunDate</td><td>Optional</td><td>Provide a run date if that needs to be updated to the rule result</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>PassCount</td><td>Optional</td><td>Provide a pass count if that needs to be updated to the rule result</td><td>Must be greater than or equal to zero</td></tr>
        /// <tr><td>FailCount</td><td>Optional</td><td>Provide a fail count if that needs to be updated to the rule result</td><td>Must be greater than or equal to zero</td></tr>
        /// </table>  
        /// <br/>
        /// **Notes:**
        /// * Edit permissions on the rule are required.
        /// * One of the four optional fields that can be updated must be provided (**EvaluatedAssetUid**, **RunDate**, **PassCount**, **FailCount**).
        /// * Fields not provided will not be updated and existing values will retained.
        /// 
        /// </remarks>
        /// <returns>A list of data quality results including any error messages.</returns>
        [
            HttpPut,
            Route("quality/results/"),
            SwaggerRequestExample(typeof(DataQualityUpdateModel), typeof(DataQualityUpdateExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the Uid of the data quality result.", typeof(List<DataQualityResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutDataQualityResultAsync(List<DataQualityUpdateModel> request)
        {
            List<DataQualityResponseModel> responseList = new List<DataQualityResponseModel>();

            var execution = getApiExecution(request.Count);

            responseList = await Task.FromResult(MetricsRepository.UpdateDataQualityResult(request, execution)).ConfigureAwait(false);
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseList));
        }

        /// <summary>
        /// Create the data quality result for an asset / Rule. This endpoint is meant for a greater number of items as it stores the result list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        ///
        /// The endpoint creates rule results for a specific rule and optional asset
        ///###Rules###
        /// <table>
        /// <tr><td>**Field**</td><td>**Required / Optional**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>OwningAssetUid</td><td>Required</td><td>UID of the Rule in which to post the results to</td><td>Must be a valid Rule UID</td></tr>
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Used to identify the request. One can be provided but if not, one will be generated</td><td>If provided must be in the correct format</td></tr>
        /// <tr><td>EvaluatedAssetUid</td><td>Optional</td><td>Asset UID  of the asset that the result is for</td><td>Must be valid Business or Technical Asset UID</td></tr>
        /// <tr><td>EffectiveDate</td><td>Required</td><td>Effective date of the rule result</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>RunDate</td><td>Required</td><td>Run date of the rule result</td><td>Must not be in the future. Date format is strictly enforced.</td></tr>
        /// <tr><td>PassCount</td><td>Required</td><td>Number of rows that passed the rule</td><td>Must be greater than or equal to zero</td></tr>
        /// <tr><td>FailCount</td><td>Required</td><td>Number of rows that failed the rule</td><td>Must be greater than or equal to zero</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:** 
        /// * Edit permissions on the rule are required.
        /// 
        /// </remarks>
        /// <returns>An HTTP status code, executionId of the request and message.</returns>
        [
            HttpPost,
            Route("quality/batch/results"),
            SwaggerRequestExample(typeof(DataQualityInsertModel), typeof(DataQualityInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkDataQualityResultAsync(List<DataQualityInsertModel> request, bool triggersWorkflow = true)
        {
            var prefix = "Metrics.PostBulkDataQualityResultAsync => ";
            var errorMessage = "";

            try
            {
                if (request == null)
                {
                    request = readRequestJsonContent<List<DataQualityInsertModel>>(Request).Result;
                }

                if (request == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ErrorInvalidDatasetMessage)).ConfigureAwait(false);
                }

                var execution = getApiExecution(request.Count);

                ApiExecutionInfo executionInfo = await MetricsRepository.PostBulkDataQualityResults(request, execution, triggersWorkflow);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/executions/{executionInfo.ExecutionID}"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "requestCount", $"{((request != null) ? request.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns graph data for assets score tab
        /// </summary>
        /// <returns>Returns a list of all score points for an asset and scores by measures.</returns>
        [
            HttpGet,
            Route("{scoreType}/{assetUid:Guid}/graphPoints"),
            SwaggerRequestExample(typeof(DataQualityUpdateModel), typeof(DataQualityUpdateExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Permission denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A response with the Uid of the data quality result.", typeof(List<DataQualityResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetGraphDataPoints(ScoreType scoreType, Guid assetUid)
        {
            try
            {
                var assetDetail = Company.Filter<AssetDetail>(i => i.uid == assetUid).FirstOrDefault();
                if (assetDetail == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString()))).ConfigureAwait(false);

                var allocation = Company.Filter<MetricAllocation>(al =>
                  al.AssetTypeUid == assetDetail.AssetTypeUid &&
                  al.ScoreType == scoreType &&
                  string.IsNullOrEmpty(al.OverrideName)
                  ).FirstOrDefault();

                List<GraphPoints> results;

                if (allocation == null)
                {
                    results = new List<GraphPoints>();
                }
                else
                {
                    results = Company.Query<GraphPoints>(@"select [EffectiveDate]
      ,[Value]
  from [metrics].[Score]
  where assetuid = @assetUid and AllocationUid = @allocationUid
  order by effectivedate desc", new { allocationUid = allocation.Uid, assetUid }).ToList();
                }

                List<GraphPoints> allPoints = new List<GraphPoints>();
                foreach (var item in results)
                {
                    item.key = "score";
                    var dataPerPoint = MetricsRepository.GetMetricHierarchyByAsset(allocation.Uid, assetUid, item.EffectiveDate.Date.ToLocalTime());

                    foreach (var measure in dataPerPoint)
                    {
                        if (!measure.IsGroup)
                        {
                            var point = new GraphPoints();
                            point.key = measure.Uid.ToString();
                            point.EffectiveDate = item.EffectiveDate;
                            point.Value = measure.AdjustedWeight;
                            allPoints.Add(point);
                        }
                        else
                        {
                            if (measure.Measures != null)
                            {
                                foreach (var m in measure.Measures)
                                {
                                    var point = new GraphPoints();
                                    point.key = m.Uid.ToString();
                                    point.EffectiveDate = item.EffectiveDate;
                                    point.Value = m.AdjustedWeight;
                                    allPoints.Add(point);
                                }
                            }

                            var measurePoint = new GraphPoints();
                            measurePoint.key = measure.Uid.ToString();
                            measurePoint.EffectiveDate = item.EffectiveDate;
                            measurePoint.Value = measure.AdjustedWeight;// result;
                            allPoints.Add(measurePoint);
                        }
                    }
                }
                allPoints.AddRange(results);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allPoints.GroupBy(x => x.key).Select(x => new { key = x.Key, data = x.ToList() })));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, ex.Message)).ConfigureAwait(false);

            }

        }

        public class GraphPoints
        {
            public string key { get; set; }
            public DateTime EffectiveDate { get; set; }
            public decimal Value { get; set; }
        }

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with the details of the data quality results</returns>
        private SLDocument CreateResponseDocument(DataQualityGetResultModel dataQualityResult)
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
            doc.SetCellValue(rowNumber, index++, "EvaluatedAssetPath");
            doc.SetCellValue(rowNumber, index++, "EvaluatedAssetTypePath");
            doc.SetCellValue(rowNumber, index++, "EvaluatedAssetClass");
            doc.SetCellValue(rowNumber, index++, "EffectiveDate");
            doc.SetCellValue(rowNumber, index++, "RunDate");
            doc.SetCellValue(rowNumber, index++, "TotalCount");
            doc.SetCellValue(rowNumber, index++, "PassCount");
            doc.SetCellValue(rowNumber, index++, "FailCount");
            doc.SetCellValue(rowNumber, index++, "PassFraction");

            #endregion
            #region Body
            foreach (var row in dataQualityResult.items)
            {
                index = 1;
                rowNumber++;
                doc.SetCellValue(rowNumber, index++, row.ResultUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.OwningAssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetPath);
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetTypePath);
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetClass);
                doc.SetCellValue(rowNumber, index++, row.EffectiveDate.ToString());
                doc.SetCellValue(rowNumber, index++, row.RunDate.ToString());
                doc.SetCellValue(rowNumber, index++, row.TotalCount);
                doc.SetCellValue(rowNumber, index++, row.PassCount);
                doc.SetCellValue(rowNumber, index++, row.FailCount);
                doc.SetCellValue(rowNumber, index++, row.PassFraction.ToString());
            }
            doc.AutoFitColumn(1, 13);
            #endregion
            #endregion
            return doc;
        }

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with the details of the data quality results formatted to match the results list screen</returns>
        private SLDocument CreateResponseDocumentForExport(DataQualityGetResultModel dataQualityResult)
        {
            SLDocument doc = new SLDocument();
            doc.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Results");

            #region Create the list sheet

            #region Header
            int index = 1;
            int rowNumber = 1;

            doc.SetCellValue(rowNumber, index++, "Asset Class");
            doc.SetCellValue(rowNumber, index++, "Asset Type");
            doc.SetCellValue(rowNumber, index++, "Asset");
            doc.SetCellValue(rowNumber, index++, "Run Date");
            doc.SetCellValue(rowNumber, index++, "Effective Date");
            doc.SetCellValue(rowNumber, index++, "Pass Fraction");
            doc.SetCellValue(rowNumber, index++, "Total Rows");
            doc.SetCellValue(rowNumber, index++, "Rows Passed");
            doc.SetCellValue(rowNumber, index++, "Rows Failed");
            doc.SetCellValue(rowNumber, index++, "Rule Result UID");

            #endregion
            #region Body
            foreach (var row in dataQualityResult.items)
            {
                index = 1;
                rowNumber++;
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetClass);
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetTypePath);
                doc.SetCellValue(rowNumber, index++, row.EvaluatedAssetDisplayPath);
                doc.SetCellValue(rowNumber, index++, row.RunDate.ToString("yyyy-MM-dd HH:mm:ss"));
                doc.SetCellValue(rowNumber, index++, row.EffectiveDate.ToString("yyyy-MM-dd"));
                doc.SetCellValue(rowNumber, index++, row.PassFraction.ToString());
                doc.SetCellValue(rowNumber, index++, row.TotalCount);
                doc.SetCellValue(rowNumber, index++, row.PassCount);
                doc.SetCellValue(rowNumber, index++, row.FailCount);
                doc.SetCellValue(rowNumber, index++, row.ResultUid.ToString());
            }
            doc.AutoFitColumn(1, 11);
            #endregion
            #endregion
            return doc;
        }
    }
}
