using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Linq;
using System.Text.RegularExpressions;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/connectorLabels"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class ConnectorLabelsController : BaseV2ApiController
    {

        public ConnectorLabelsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {

        }


        /// <summary>
        /// Retrieves a list of available labels by search term
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("search"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of connector labels."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetLabels(string q = null)
        {

            if (!string.IsNullOrEmpty(q))
                q = $"%{q}%";

            var labelsSql = $@"SELECT top 10 uid, Value
                                  FROM [dbo].[ConnectorLabel]
                                where Value like @q and state = 1
                                order by Value";

            var response = Company.Query<dynamic>(labelsSql, new { q });

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

        }

        /// <summary>
        /// Create or get label by label name
        /// Used by connector label autocomplete control in Process Designer
        /// </summary>
        /// <returns></returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of connector labels."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> CreateOrGetLabel(ConnectorLabelPostModel label)
        {


            if (label == null || string.IsNullOrEmpty(label.Value) || label.Value.Trim() == "")
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "Label value cannot be empty.")));

            var labelValue = label.Value.Trim();
            var dbRecord = Company.ConnectorLabels.FirstOrDefault(x => x.Value.ToLower() == labelValue.ToLower());
            if (dbRecord != null)
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dbRecord)));


            if (labelValue.Length > 40)
            {
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "Maximum length of label is 40 characters.")));
            }

            dbRecord = new ConnectorLabel();
            dbRecord.Value = labelValue;
            dbRecord.UpdatedBy = dbRecord.CreatedBy = Company.CurrentResourceID;
            dbRecord.UpdatedOn = dbRecord.CreatedOn = DateTime.UtcNow;

            Company.Add(dbRecord);

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dbRecord)));

        }
    }
}

