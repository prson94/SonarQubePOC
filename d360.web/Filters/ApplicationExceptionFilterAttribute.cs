using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

using d360.core.exceptions;
using d360.web.Models;
using d360.web.Services;

using Resources;

namespace d360.web.Filters
{
    internal class ApplicationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private bool SuppressNotHandledErrorDetails { get; }

        public ApplicationExceptionFilterAttribute(bool suppressNotHandledErrorDetails)
        {
            SuppressNotHandledErrorDetails = suppressNotHandledErrorDetails;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            switch (context.Exception)
            {
                case GenericException genericException:
                    context.Response = context.Request.CreateResponse(
                        genericException.StatusCode,
                        new ErrorResponse { title = genericException.StatusMessage, message = genericException.StatusDescription }
                    );
                    break;
                case RestApiException restApiException:
                    context.Response = context.Request.CreateResponse(
                        restApiException.Status,
                        new ErrorResponse { title = restApiException.Title, message = restApiException.Message }
                    );
                    break;
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
                case NotFoundException notFoundException:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.NotFound,
                        new ErrorResponse { title = ApiMessages.NotFound, message = notFoundException.Message }
                    );
                    break;
                case NotFoundBusinessLayerException exception:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.NotFound,
                        new ErrorResponse { title = ApiMessages.NotFound, message = exception.Message }
                    );
                    break;
                case InvalidRequestException invalidRequestException:
                    context.Response = context.Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        new ErrorResponse { title = ApiMessages.InvalidRequest, message = invalidRequestException.Message }
                    );
                    break;
                default:
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (SuppressNotHandledErrorDetails)
                    {
                        context.Response = context.Request.CreateResponse(
                            HttpStatusCode.InternalServerError,
                            new ErrorResponse { title = OthersMessages.CriticalException, message = OthersMessages.NeedAdministratorHelp }
                        );
                    }
                    else
                    {
                        context.Response = context.Request.CreateResponse(
                            HttpStatusCode.InternalServerError,
                            new ErrorResponse { title = ApiMessages.UnknownError, message = context.Exception.Message }
                        );
                    }
                    break;
            }
        }
    }
}
