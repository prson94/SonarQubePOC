using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace d360.web.Handlers.Exceptions
{
	public interface IWebApi2ExceptionHandler
	{
		/// <summary>
		/// Is this handler is default (handle exception if no specific handlers found)
		/// </summary>
		bool IsDefault { get; }

		/// <summary>
		/// Return true if handler can handle specific type of exception
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		bool CanHandle(Exception exception);

		/// <summary>
		/// Handle exception logic.
		/// </summary>
		/// <param name="context">Exception context from <see cref="IExceptionHandler"/></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken = default);
	}
}
