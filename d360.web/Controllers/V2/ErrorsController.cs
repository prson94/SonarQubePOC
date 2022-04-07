using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core.exceptions;
using d360.web.Models;

using Microsoft.Web.Http;

namespace d360.web.Controllers.V2
{
    [
        ApiExplorerSettings(IgnoreApi = true),
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/errors"), Authorize
    ]
    public class ErrorsController : BaseV2ApiController
    {
        #region DI

        public ErrorsController(CoreComponentSet set) : base(set)
        {

        }

        #endregion

        #region LogClientError

        /// <summary>
        /// This logs the client error on server side
        /// </summary>
        /// <param name="model">Error model</param>
        /// <returns>Returns Http Status code 200 if logged successfully; else return http status code 500 with error message</returns>
        [HttpPost, Route("log/clienterror")]
        public HttpResponseMessage SaveClientError(ClientErrorModel model)
        {
            try
            {
                IDictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "name", model.Name },
                    { "stacktrace", model.Stack }
                };
                SendException(new ClientSideException(model.Message), properties);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        #endregion
    }
}
