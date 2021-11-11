using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;
using Resources;

namespace d360.web.Filters
{
    public class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is RestApiException)
            {
                var restEx = (context.Exception as RestApiException);

                context.Response = context.Request.CreateResponse(
                   restEx.Status,
                   new ErrorResponse { title = restEx.Title, message = restEx.Message }
               );
            }
            else 
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(OthersMessages.NeedAdministratorHelp),
                    ReasonPhrase = OthersMessages.CriticalException
                });
            }
        }
    }
}