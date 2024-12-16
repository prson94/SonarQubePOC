using d360.core.resources;
using d360.web.Utilities;
using System;
using System.Configuration;
using System.Web.Http.ExceptionHandling;

namespace d360.web.Handlers.Exceptions
{
	internal sealed class DefaultWebApi2ExceptionHandler : WebApi2ExceptionHandlerBase
	{
		public DefaultWebApi2ExceptionHandler(IRuntimeInfo runtimeInfo) : base(runtimeInfo)
		{
		}

		public override bool IsDefault { get; } = true;

		public override bool CanHandle(Exception exception)
		{
			return true;
		}

		protected override void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails)
		{
			var hideErrorDetailsString = ConfigurationManager.AppSettings["security:surpressApiErrorDetails"];
			var hideErrorDetails = string.Equals(hideErrorDetailsString ?? string.Empty, "true", StringComparison.InvariantCultureIgnoreCase);

			if (hideErrorDetails)
			{
				problemDetails.Title = Error.CriticalException;
				problemDetails.Detail = Error.NeedAdministratorHelp;
			}
			else
			{
				problemDetails.Title = Error.UnknownError;
			}
		}
	}
}
