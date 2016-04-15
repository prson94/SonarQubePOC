using d360.core;
using d360.model;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Retrieve report tile results.
    /// </summary>
    [RoutePrefix("services/reports"), Authorize]
    public class ReportsController : BaseApiController
    {
        #region DI

        public ReportsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        [Route("{reportID:int}/{type}/{id:int}/tiles/{tileID:int}/data")]
        public HttpResponseMessage GetReportTileData(int reportID, SystemObjects type, int id, int tileID)
        {
            HttpResponseMessage response = null;

            try
            {
                var models = Company.GetReportQueryResults(tileID, type, id);
                response = Request.CreateResponse(HttpStatusCode.OK, models);
            }
            catch (SqlException ex)
            {
                response = Request.CreateErrorResponse(HttpStatusCode.PreconditionFailed, ex.GetFullExceptionData(), ex);
            }

            return response;
        }

    }
}
