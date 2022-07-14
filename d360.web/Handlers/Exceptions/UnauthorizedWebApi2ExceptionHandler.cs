using System;
using System.Web.Http.ExceptionHandling;
using d360.web.Services;
using d360.web.Utilities;
using Resources;

namespace d360.web.Handlers.Exceptions
{
	public class UnauthorizedWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{
		public UnauthorizedWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool CanHandle(Exception exception)
		{
			return exception is UnauthorizedBusinessLayerException;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			problemDetails.Status = 401;
			problemDetails.Title = ApiMessages.EndpointNotAuthorizedHeading;
			problemDetails.Detail = ApiMessages.EndpointNotAuthorizedMessage;
		}
	}
}
