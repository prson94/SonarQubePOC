using System;
using System.Web.Http.ExceptionHandling;
using d360.core.exceptions;
using d360.web.Utilities;

namespace d360.web.Handlers.Exceptions
{
	internal sealed class GenericExceptionWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{
		public GenericExceptionWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool CanHandle(Exception exception)
		{
			return exception is GenericException;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			if (context.Exception is GenericException gex)
			{
				problemDetails.Status = (int)gex.StatusCode;
				problemDetails.Title = gex.StatusMessage;
				problemDetails.Detail = gex.StatusDescription;
			}
		}
	}
}
