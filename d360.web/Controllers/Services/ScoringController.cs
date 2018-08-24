using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.web.Models;
using Microsoft.Web.Http;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Search everything in Data3Sixty.
    /// </summary>
    [ApiVersion("1.0"), RoutePrefix("services/scoring"), Authorize]
    public class ScoringController : BaseApiController
    {
        #region DI

        public ScoringController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        /// <summary>
        /// NOT YET FUNCTIONAL.  DO NOT USE.
        /// </summary>
        /// <param name="object">the type of the object (i.e. Artifact, Taxonomy, Policy).</param>
        /// <param name="objectID">The ID of the object.</param>
        /// <param name="scoreTypeMetricID">The Score type metric ID.</param>
        /// <param name="model">The value to storage.  This should be a decimal that represents the fulfillment % of the maximum score stored in Data3Sixty.</param>
        /// <returns></returns>
        [HttpPost, Route("{object}/{objectID:int}/{scoreTypeMetricID:int}")]
        public HttpResponseMessage AddScoreValueForMetric(string @object, int objectID, int scoreTypeMetricID, ExternalScoreModel model)
        {
            if (!Company.CurrentResourceIsAdmin)//HasPermission(SystemObjects.ScoreTypeMetric, id, Claim.Update))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this metric result.");

            var dtl = Company.GetObjectDetail(@object, objectID);

            if (dtl == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Object specified could not be found.");

            var metricVersion = Company.Filter<ScoreTypeMetricVersion>(i => i.ScoreTypeMetricID == scoreTypeMetricID).OrderByDescending(i => i.UpdatedOn).FirstOrDefault();

            if (metricVersion == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Metric could not be found.");

            if (metricVersion.CheckType == core.enums.StatisticCheckType.External)
                return Request.CreateErrorResponse(HttpStatusCode.Conflict, "Metric is not marked as External check type.");

            var today = DateTime.UtcNow.Date;

            //var scoreMetric = Company.Filter<ScoreMetric>(i => i.)

            return Request.CreateResponse(HttpStatusCode.Created);
        }
    }
}
