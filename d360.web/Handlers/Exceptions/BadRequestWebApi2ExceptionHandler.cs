using System;
using System.Web.Http.ExceptionHandling;
using d360.web.Services;
using d360.web.Utilities;
using Resources;

namespace d360.web.Handlers.Exceptions
{
	internal sealed class BadRequestWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{

		public BadRequestWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool CanHandle(Exception exception)
		{
			return exception is InvalidRequestException || exception is ArgumentException;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			problemDetails.Title = ApiMessages.BadRequest;
			problemDetails.Status = 400;
		}
	}
}
