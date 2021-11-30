using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace d360.web.Services
{
    [ExcludeFromCodeCoverage]
    public static class MediatorExtensions
    {
        public static Task<TResponse> Send<TRequest, TResponse>(this IMediator mediator, Action<TRequest> setupAction, CancellationToken cancellationToken = default)
            where TRequest : IRequest<TResponse>, new()
        {
            var request = new TRequest();
            setupAction?.Invoke(request);
            return mediator.Send(request, cancellationToken);
        }

        /// <summary>
        /// Facade to access known entity validator.
        /// Note! Internal because it should not be called outside of business layer.
        /// </summary>
        /// <param name="mediator"></param>
        /// <returns></returns>
        internal static IEntityValidatorFacade EntityValidators(this IMediator mediator)
        {
            return new EntityValidatorFacade(mediator);
        }
    }
}