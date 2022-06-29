using System;
using System.Web.Http.ExceptionHandling;
using d360.core.exceptions;
using d360.web.Services;
using d360.web.Utilities;
using Resources;

namespace d360.web.Handlers.Exceptions
{
	/// <summary>
	///     Handle common exception which leads to 404 status code.
	/// </summary>
	internal sealed class NotFoundWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{

		public NotFoundWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		/// <inheritdoc />
		public override bool CanHandle(Exception exception)
		{
			return exception is NotFoundBusinessLayerException || exception is NotFoundException;
		}

		/// <inheritdoc />
		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			problemDetails.Status = 404;
			problemDetails.Title = ApiMessages.NotFound;
		}
	}
}
