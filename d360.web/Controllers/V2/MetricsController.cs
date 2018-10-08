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
    public class MetricsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;

        public MetricsController(CommunityContext community, CompanyContext company, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
        }

        #endregion

        /// <summary>
        /// Gets a metric by its ID.
        /// </summary>
        /// <param name="uid">The public ID for the metric.</param>
        /// <returns>The metric.</returns>
        [
            HttpGet,
            Route("{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(MetricAsset))
        ]
        public MetricAsset GetAssetById(Guid uid)
        {
            return Company.Filter<MetricAsset>(i => i.Uid == uid, i => i.Children).SingleOrDefault();
        }

        /// <summary>
        /// Add or updates a metric.
        /// </summary>
        /// <param name="model">The definition of the metric itself. If updating an existing metric, ensure that you populate the Uid property.</param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the ADD request.", typeof(string)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the UPDATE request.", typeof(string))
        ]
        public IHttpActionResult UpsertAsset(MetricAssetViewModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this metric."));

            if (model == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "You are have provided a null metric."));

            MetricAsset metricAsset = null;
            var isNew = false;

            if (model.Uid != Guid.Empty)
            {
                metricAsset = Company.Filter<MetricAsset>(i => i.Uid == model.Uid).SingleOrDefault();
                if (metricAsset == null)
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Metric not found."));

                metricAsset.Description = model.Description;
                metricAsset.Name = model.Name;
            }
            else
            {
                metricAsset = new MetricAsset
                {
                    //Uid = Guid.NewGuid(),
                    AssetTypeUid = model.AssetTypeUid,
                    Description = model.Description,
                    IsGroup = model.IsGroup,
                    Name = model.Name,
                    State = State.Active
                };

                if (model.AssetTypeUid == Guid.Empty)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Asset type not found or is empty."));
                }

                if (model.ParentUid != Guid.Empty && model.ParentUid.HasValue)
                {
                    var parentMetricAsset = Company.Filter<MetricAsset>(i => i.Uid == model.ParentUid).SingleOrDefault();
                    if (parentMetricAsset == null)
                    {
                        return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Parent metric not found."));
                    }
                    else
                    {
                        if (parentMetricAsset.AssetTypeUid != metricAsset.AssetTypeUid)
                        {
                            return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Parent metric must belong to the same asset type."));
                        }
                    }
                    metricAsset.ParentUid = model.ParentUid;
                }

                Company.Add(metricAsset);
            }

            var cleanDate = model.EffectiveDate.Date;
            var metricAssetVersion = Company.Filter<MetricAssetVersion>(i => i.Uid == model.Uid && i.EffectiveDate == cleanDate, v => v.Conditions).SingleOrDefault();

            string newConditionHash = string.Join("|", model.Conditions.Select(c => string.Join(";", c.FieldTypeID, c.Operator, c.Values)));
            newConditionHash = newConditionHash.GetD3sHashString();
            if (metricAssetVersion == null)
            {
                metricAssetVersion = new MetricAssetVersion
                {
                    Uid = metricAsset.Uid,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    ConditionAndOr = model.ConditionAndOr,
                    EffectiveDate = model.EffectiveDate,
                    Weight = model.Weight
                };

                if (model.Conditions.Count > 0)
                {
                    if (metricAssetVersion.Conditions == null)
                        metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();
                    model.Conditions.ForEach(c => {
                        metricAssetVersion.Conditions.Add(new MetricAssetVersionCondition { FieldTypeID = c.FieldTypeID, Operator = c.Operator, ValueJson = c.Values });
                    });
                }

                Company.MetricAssetVersions.Add(metricAssetVersion);
            }
            else
            {
                if (metricAssetVersion.Weight != model.Weight)
                {
                    return ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.BadRequest,
                            getErrorResponse("Error updating metric", "You may not alter the weight of this metric without also altering its effective date.")
                        )
                    );
                }
                if (metricAssetVersion.ConditionAndOr != model.ConditionAndOr)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, getErrorResponse("Error updating metric", "You may not alter the condition type of this metric without also altering its effective date.")));
                }

                string existingConditionHash = string.Join("|", metricAssetVersion.Conditions.Select(c => string.Join(";", c.FieldTypeID, c.Operator, c.ValueJson)));
                existingConditionHash = existingConditionHash.GetD3sHashString();
                if (newConditionHash != existingConditionHash)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, getErrorResponse("Error updating metric", "You may not alter the conditions of this metric without also altering its effective date.")));
                }
            }

            Company.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, "Metric updated."));
        }

        [
            HttpDelete,
            Route("{uid}"),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the UPDATE request.", typeof(string))
        ]
        public IHttpActionResult DeleteById(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this metric."));

            var model = Company.Filter<MetricAsset>(i => i.Uid == uid).SingleOrDefault();

            if (model == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Metric not found."));

            model.State = State.Deleted;
            Company.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, "Metric removed."));
        }

        /// <summary>
        /// Gets a hierarchical structure of metrics and conditions associated with the asset type UID provided.
        /// </summary>
        /// <param name="assetTypeUid">The UID of the asset type.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past or future effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/definition"),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metrics and conditions.", typeof(MetricAssetTypeHierarchyModels))
        ]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetTypeAsync(Guid assetTypeUid, DateTime? effectiveDate = null)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to retrieve the metric heirarchy for this asset type.")));

            var prefix = "Metrics.GetMetricHierarchyByAssetTypeAsync => ";
            var errorMessage = "";

            try
            {
                var result = (Company.Database.Connection as SqlConnection).GetMetricDefinitionHierarchyByAssetType(assetTypeUid, effectiveDate);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Gets a hierarchical structure of metrics associated with the asset UID provided, for a given effective date. If no effective date is provided, today's date is used.
        /// </summary>
        /// <param name="assetUid">The UID of the asset.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/pointbreakdown"),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric values for a given asset.", typeof(MetricAssetTypeHierarchyModels))]
        public async Task<IHttpActionResult> GetMetricHierarchyByAssetAsync(Guid assetUid, DateTime? effectiveDate = null)
        {
            /*
                         declare @effectiveDate date = '10/3/2018',
                                @assetTypeUid uniqueidentifier = '8371C4C6-E17E-4620-BA8B-AE0301966E0E',
                                @assetUid uniqueidentifier = '5DFA86D6-9DFE-4BB6-B417-F75E3BC9E095';
            */
            var prefix = "Metrics.GetMetricHierarchyByAssetAsync => ";
            var errorMessage = "";

            try
            {
                var result = (Company.Database.Connection as SqlConnection).GetMetricHierarchyByAsset(assetUid, effectiveDate);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Gets a administrative hierarchical structure of metrics associated with the asset type UID provided.
        /// </summary>
        /// <param name="assetTypeUid">The UID of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("structure/{assetTypeUid:Guid}"),
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

                var fragments = Company.Query<string>($@"
select	A.Uid,
		A.ParentUid,
		A.AssetTypeUid,
		A.IsGroup,
		A.Name,
		A.Description,
		V.EffectiveDate,
		V.Weight,
		V.ConditionAndOr,
		(
			select	FieldTypeID,
					Operator,
					[ValueJson] as [Values]
			from	metrics.AssetVersionCondition
			where	Uid = V.Uid and EffectiveDate = V.EffectiveDate
			for		json path

		) as Conditions
from	metrics.Asset A
		cross apply (
			select	max(EffectiveDate) as EffectiveDate
			from	metrics.AssetVersion
			where	Uid = A.Uid
		) MV
		inner join metrics.AssetVersion V on V.Uid = A.Uid and V.EffectiveDate = MV.EffectiveDate and A.[State] = 1
where	A.AssetTypeUid = '{assetTypeUid.ToString()}'
for		json path").ToList();

                models = JsonConvert.DeserializeObject<List<MetricAssetViewModel>>(string.Join("", fragments));

                //Func<List<MetricAssetViewModel>, MetricAssetViewModel, List<MetricAssetViewModel>> buildTree = delegate(List<MetricAssetViewModel> list, MetricAssetViewModel parent) {
                //    var returnList = new List<MetricAssetViewModel>();

                //    if (parent == null)
                //    {
                //        foreach(var i in list.Where(o => !o.ParentUid.HasValue))
                //        {
                //            i.Children.AddRange(buildTree(list, i));
                //            returnList.Add(i);
                //        }
                //    }

                //    list.ForEach(c =>
                //    {
                //        if (c.Children == null)
                //            c.Children = new List<MetricAssetViewModel>();

                //        if (!c.ParentUid.HasValue)
                //            returnList.Add(c);
                //    });

                //    return returnList;
                //};

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
        /// Gets a administrative hierarchical structure of metrics associated with the asset type UID provided.
        /// </summary>
        /// <param name="assetTypeUid">The UID of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("fields/{assetTypeUid:Guid}"),
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

                var fragments = Company.Query<string>($@"
select	F.ID,
		F.FriendlyName as Name,
		F.Type,
		(
			select	Value,
					Text
			from	FieldLookupValue
			where	FieldTypeID = F.ID
			for		json path

		) as [Values]
from	AssetType A
		inner join FieldType F on F.AssetTypeID = A.ID and A.[uid] = '{assetTypeUid.ToString()}' and F.Type in ('Boolean', 'Date', 'Lookup', 'Number', 'Text')
for		json path").ToList();

                models = JsonConvert.DeserializeObject<List<MetricFieldTypeViewModel>>(string.Join("", fragments));

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
        /// Adds one or more metric results for processing and scoring.
        /// </summary>
        /// <param name="model">The list of raw metrics to save for processing.</param>
        /// <returns>he list of staging results.</returns>
        [
            HttpPost,
            Route("results"),
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<BulkMetricTemporaryTableModel>))
        ]
        public IHttpActionResult PostBulkMetricsToStagingAsync(BulkMetricsImport model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships of this type."));

            var prefix = "Metrics.PostBulkMetricsToStagingAsync => ";
            var errorMessage = "";

            try
            {
                var results = (Company.Database.Connection as SqlConnection).BulkMetricsImport(model);

                //return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
                return ResponseMessage(Request.CreateResponse<List<BulkMetricTemporaryTableModel>>(HttpStatusCode.OK, results));
                //return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { message = "Metric results queued for processing."})));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage));
            }
        }
    }
}
