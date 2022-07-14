using System;
using System.Web.Http.ExceptionHandling;
using d360.web.Services;
using d360.web.Utilities;
using Resources;

namespace d360.web.Handlers.Exceptions
{
	public class ForbiddenWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{
		public ForbiddenWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool CanHandle(Exception exception)
		{
			return exception is ForbiddenBusinessLayerException;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			problemDetails.Status = 403;
			problemDetails.Title = ApiMessages.Forbidden;
			problemDetails.Detail = ApiMessages.ForbiddenUserNotAuthorizedMessage;
		}
	}
}
