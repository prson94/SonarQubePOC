using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using d360.web.Controllers.V2;
using d360.web.Models;
using d360.web.Services;
using d360.web.Utilities;
using Resources;

namespace d360.web.Handlers
{
    internal class ApplicationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            // compose error message (probably better formatting is needed)
            var ex = context.Exception;
            var errorMessage = ex.Message; // + (ex.InnerException?.Message ?? "");

            //#region I don't understand why this trace is needed however moved it here for now... ask @pungupta

            //var traceAttribute = context.ActionContext.ActionDescriptor.GetCustomAttributes<TracePrefixAttribute>().FirstOrDefault();
            //var traceScope = "UNKNOWN";

            //if (traceAttribute != null)
            //{
            //    traceScope = traceAttribute.Text;
            //}
            //else
            //{
            //    var controllerName = context.ActionContext.ControllerContext.ControllerDescriptor.ControllerName;
            //    var actionName = context.ActionContext.ActionDescriptor.ActionName;
            //    traceScope = $"{controllerName}.{actionName}";
            //}

            //Trace.TraceError("{0} => {1}", traceScope, errorMessage);

            //#endregion

            switch (context.Exception)
            {
                case UnauthorizedBusinessLayerException unauthorized:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.Unauthorized,
                        new ErrorResponse { title = ApiMessages.EndpointNotAuthorizedHeading, message = ApiMessages.EndpointNotAuthorizedMessage }
                    );
                    break;
                case ForbiddenBusinessLayerException forbidden:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.Forbidden,
                        new ErrorResponse { title = ApiMessages.Forbidden, message = ApiMessages.ForbiddenUserNotAuthorizedMessage }
                    );
                    break;
                case NotFoundBusinessLayerException notFoundException:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.NotFound,
                        new ErrorResponse { title = ApiMessages.NotFound, message = notFoundException.Message }
                    );
                    break;
                default:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.InternalServerError,
                        new ErrorResponse { title = ApiMessages.UnknownError, message = errorMessage }
                    );
                    break;
            }
        }
    }
}