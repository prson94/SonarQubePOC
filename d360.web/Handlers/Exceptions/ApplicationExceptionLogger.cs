using System.Collections.Generic;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using d360.extensions;
using Microsoft.ApplicationInsights;

namespace d360.web.Handlers.Exceptions
{
	public class ApplicationExceptionLogger : ExceptionLogger
	{

		public ApplicationExceptionLogger()
		{
			Telemetry = new TelemetryClient();
		}

		public TelemetryClient Telemetry { get; set; }

		public override void Log(ExceptionLoggerContext context)
		{
			if (context != null && context.Exception != null)
			{
				var telemetryClient = DependencyResolver.Current.GetService<TelemetryClient>();

				// get companyId 
				var securityContextProvider = DependencyResolver.Current.GetService<ISecurityContextProvider>();
				var companyId = securityContextProvider.CompanyID;

				var controllerName = context.ExceptionContext.ControllerContext?.ControllerDescriptor?.ControllerName ?? string.Empty;
				var actionName = context.ExceptionContext.ActionContext?.ActionDescriptor?.ActionName ?? string.Empty;

				// compose properties dictionary
				var properties = new Dictionary<string, string>();

				// request data
				properties.Add("Url", context.Request.RequestUri.PathAndQuery);
				properties.Add("ControllerName", controllerName);
				properties.Add("ActionName", actionName);
				// other data
				properties.Add("CompanyID", companyId.ToString());

				// track exception
				telemetryClient.TrackException(context.Exception, properties);
			}

			base.Log(context);
		}
	}
}
