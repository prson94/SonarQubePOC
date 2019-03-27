using d360.core;
using d360.model;
using Microsoft.Web.Http;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Retrieve report tile results.
    /// </summary>
    [ApiVersion("1.0"), ApiExplorerSettings(IgnoreApi = true), RoutePrefix("services/deprecated/reports"), Authorize]
    public class ReportsController : BaseApiController
    {
        #region DI

        public ReportsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        [Route("{reportID:int}/{type}/{id:int}/tiles/{tileID:int}/data")]
        public HttpResponseMessage GetReportTileData(int reportID, SystemObjects type, int id, int tileID, bool? metadata = false)
        {
            HttpResponseMessage response = null;

            try
            {
                var models = Company.GetReportQueryResults(tileID, type, id);

                if (metadata.GetValueOrDefault())
                {
                    List<dynamic> header = new List<dynamic>();

                    var firstRow = models.FirstOrDefault();

                    if (firstRow != null)
                    {
                        foreach (KeyValuePair<string, object> kvp in firstRow) { // enumerating over it exposes the Properties and Values as a KeyValuePair
                            var dataType = typeof(string).ToString();

                            if (kvp.Value != null)
                                dataType = kvp.Value.GetType().ToString();

                            header.Add(new { field = kvp.Key, type = dataType });
                        }
                    }
                    
                    response = Request.CreateResponse(HttpStatusCode.OK, new { metadata = header, data = models });
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, models);
                }                
            }
            catch (SqlException ex)
            {
                response = Request.CreateErrorResponse(HttpStatusCode.PreconditionFailed, ex.GetFullExceptionData(), ex);
            }

            return response;
        }

    }
}
