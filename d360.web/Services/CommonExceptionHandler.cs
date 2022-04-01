using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR.Pipeline;

namespace d360.web.Services
{
    public class CommonExceptionHandler<TRequest, TResponse> : AsyncRequestExceptionHandler<TRequest, TResponse>
    {
        protected override Task Handle(TRequest request,
            Exception exception,
            RequestExceptionHandlerState<TResponse> state,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case BusinessLayerException businessLayerException:
                    throw businessLayerException;
                default:
                    throw new UnrecoverableBusinessLayerException("Unhandled business layer exception", exception);
            }
        }
    }
}
