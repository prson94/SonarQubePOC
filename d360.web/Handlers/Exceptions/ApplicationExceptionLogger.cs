using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;

namespace d360.web.Handlers.Exceptions
{
	public class ApplicationExceptionLogger : ExceptionLogger
	{
		public override void Log(ExceptionLoggerContext context)
		{
			if (context != null && context.Exception != null)
			{
				var Log = DependencyResolver.Current.GetService<ILogger>();

				var controllerName = context.ExceptionContext.ControllerContext?.ControllerDescriptor?.ControllerName ?? string.Empty;
				var actionName = context.ExceptionContext.ActionContext?.ActionDescriptor?.ActionName ?? string.Empty;

				var properties = new Dictionary<string, string>
				{
					{ "Url", context.Request.RequestUri.PathAndQuery },
					{ "ControllerName", controllerName },
					{ "ActionName", actionName },
				};
				using (Log.BeginScope(properties))
				{
					Log.LogError(context.Exception, context.Exception.Message);
				}
			}

			base.Log(context);
		}
	}
}
