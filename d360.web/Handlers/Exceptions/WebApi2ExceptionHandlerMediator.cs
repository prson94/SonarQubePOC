using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace d360.web.Handlers.Exceptions
{
	public class WebApi2ExceptionHandlerMediator
	{
		public WebApi2ExceptionHandlerMediator(ICollection<IWebApi2ExceptionHandler> exceptionHandlers)
		{
			ExceptionHandlers = exceptionHandlers;
		}

		private ICollection<IWebApi2ExceptionHandler> ExceptionHandlers { get; }

		private IWebApi2ExceptionHandler GetDefaultHandler()
		{
			var list = ExceptionHandlers.Where(x => x.IsDefault).ToList();
			switch (list.Count)
			{
				case 0:
					throw new ConfigurationErrorsException($"{nameof(IWebApi2ExceptionHandler)} with {nameof(IWebApi2ExceptionHandler.IsDefault)} == true should be registered.");
				case 1:
					return list[0];
				default:
					throw new ConfigurationErrorsException($"Multiple {nameof(IWebApi2ExceptionHandler)} with {nameof(IWebApi2ExceptionHandler.IsDefault)} == true are not allowed.");
			}
		}

		private IWebApi2ExceptionHandler GetConcreteExceptionHandler(Exception exception)
		{
			var list = ExceptionHandlers.Where(x => x.IsDefault == false && x.CanHandle(exception)).ToList();
			switch (list.Count)
			{
				case 0:
					return GetDefaultHandler();
				case 1:
					return list[0];
				default:
					var error = new ConfigurationErrorsException($"Multiple {nameof(IWebApi2ExceptionHandler)} can process exception {exception.GetType()}.");
					error.Data.Add("exception-handler-duplicate", list.Select(x => x.GetType().FullName).ToList());
					throw error;
			}
		}

		public async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
		{
			var exceptionHandler = GetConcreteExceptionHandler(context.Exception);
			await exceptionHandler.HandleAsync(context, cancellationToken);
		}
	}
}
