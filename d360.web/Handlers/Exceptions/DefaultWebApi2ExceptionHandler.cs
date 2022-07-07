using System;
using System.Configuration;
using System.Web.Http.ExceptionHandling;
using d360.web.Utilities;
using Resources;

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
				problemDetails.Title = OthersMessages.CriticalException;
				problemDetails.Detail = OthersMessages.NeedAdministratorHelp;
			}
			else
			{
				problemDetails.Title = ApiMessages.UnknownError;
			}
		}
	}
}
