using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.extensions;
using d360.model;
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

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling metrics and scoring for assets throughout your environment.
    /// </summary>
    [ 
        ApiVersion("2.0"), 
        RoutePrefix("api/v{version:apiVersion}/metrics"), 
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
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

        #region Group

        /// <summary>
        /// Gets a metric group by its internal ID.
        /// </summary>
        /// <param name="id">The internal ID (whole number) for the metric group.</param>
        /// <returns>The metric group.</returns>
        [
            HttpGet, 
            Route("groups/{id:int}"), 
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric group.", typeof(MetricGroup))
        ]
        public MetricGroup GetGroupById(int id)
        {
            return Company.GetById<MetricGroup>(id, i => i.Children);
        }

        [
            HttpPost, 
            Route("groups"),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the ADD request.", typeof(string))
        ]
        public IHttpActionResult AddGroup(MetricGroup model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a group."));

            Company.Add(model);

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, "Group added."));
        }

        [
            HttpPut, 
            Route("groups/{id:int}"),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the UPDATE request.", typeof(string))
        ]
        public IHttpActionResult UpdateGroup(int id, MetricGroup model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this group."));

            var groupToUpdate = Company.GetById<MetricGroup>(id);

            if (groupToUpdate == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Group not found."));

            groupToUpdate.Description = model.Description;
            groupToUpdate.Name = model.Name;
            //groupToUpdate.Weight = model.Weight;
            Company.Update(groupToUpdate);

            if (model.Children != null && model.Children.Count > 0)
            {
                foreach (var c in model.Children)
                {
                    var child = Company.GetById<MetricGroup>(c.ID);
                    if (child != null)
                    {
                        child.Weight = c.Weight;
                        Company.Update(child);
                    }
                }
            }

            Company.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, "Group updated."));
        }

        [
            HttpDelete, 
            Route("groups/{id:int}"),
            SwaggerResponse(HttpStatusCode.Created, "A message indicating the status of the UPDATE request.", typeof(string))
        ]
        public IHttpActionResult DeleteGroupById(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this group."));

            var group = Company.GetById<MetricGroup>(id);

            if (group == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Group not found."));

            var maps = Company.MetricMaps.Where(m => m.GroupID == group.ID).ToList();
            maps.ForEach(m =>
            {
                m.State = State.Deleted;
                //var conditions = Company.MetricConditions.Where(c => c.MapID == m.ID).ToList();
                //Company.MetricConditions.RemoveRange(conditions);
            });

            group.State = State.Deleted;
            Company.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, "Group removed."));
        }

        #endregion

        /// <summary>
        /// Gets a hierarchical structure of metric groups, items, and metric conditions associated with the asset typeUID provided.
        /// </summary>
        /// <param name="assetTypeUid">The UID of the asset type.</param>
        /// <param name="effectiveDate">The date which you want to pull the metric hierarchy for. If not provided, today's date is used. Optionally, you may also provide a past or future effective date.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet, 
            Route("{assetTypeUid:Guid}/definition"),
            SwaggerResponse(HttpStatusCode.OK, "The hierarchical structure of metric groups, items, and conditions.", typeof(MetricGroupHierarchyModels))
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
        /// Adds one or more metric results for processing and scoring.
        /// </summary>
        /// <param name="model">The list of raw metrics to save for processing.</param>
        /// <returns>he list of staging results.</returns>
        [
            HttpPost, 
            Route(""), 
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
