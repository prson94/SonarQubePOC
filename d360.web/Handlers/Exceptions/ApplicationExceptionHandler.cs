using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;

namespace d360.web.Handlers.Exceptions
{
	internal sealed class ApplicationExceptionHandler : IExceptionHandler
	{
		public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
		{
			var exceptionHandlerFacade = DependencyResolver.Current.GetService<WebApi2ExceptionHandlerMediator>();
			return exceptionHandlerFacade.HandleAsync(context, CancellationToken.None);
		}
	}
}
