using System;
using System.Web.Http.ExceptionHandling;
using d360.web.Models;
using d360.web.Utilities;

namespace d360.web.Handlers.Exceptions
{
	internal sealed class RestApiExceptionWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{
		public RestApiExceptionWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool CanHandle(Exception exception)
		{
			return exception is RestApiException;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			if (context.Exception is RestApiException gex)
			{
				problemDetails.Status = (int)gex.Status;
				problemDetails.Title = gex.Title;
			}
		}
	}
}
