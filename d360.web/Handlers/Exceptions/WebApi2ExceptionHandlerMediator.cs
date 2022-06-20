using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;

namespace d360.web.Handlers.Exceptions
{
	public class WebApi2ExceptionHandlerMediator
	{
		public WebApi2ExceptionHandlerMediator(ICollection<IWebApi2ExceptionHandler> exceptionHandlers)
		{
			ExceptionHandlers = exceptionHandlers;
		}

		private ICollection<IWebApi2ExceptionHandler> ExceptionHandlers { get; }

		public async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
		{
			var defaultExceptionHandler = ExceptionHandlers.FirstOrDefault(x => x.IsDefault);
			if (defaultExceptionHandler == null)
			{
				var response = context.Request.CreateResponse();
				response.StatusCode = HttpStatusCode.InternalServerError;
				response.Content = new StringContent("Exception handler configuration error", Encoding.UTF8);
				context.Result = new ResponseMessageResult(response);
				return;
			}

			var exceptionHandler = ExceptionHandlers.FirstOrDefault(x => x.IsDefault == false && x.CanHandle(context.Exception)) ?? defaultExceptionHandler;
			await exceptionHandler.HandleAsync(context, cancellationToken);
		}
	}
}
