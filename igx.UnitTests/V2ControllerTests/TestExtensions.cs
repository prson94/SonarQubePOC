using System;
using System.Threading;
using MediatR;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
    internal static class TestExtensions
    {
        public static void SetupMediator<TRequest, TResponse>(this Mock<IMediator> mockMediator, Action<TRequest> validateRequest, TResponse response, CancellationToken cancellationToken = default)
            where TRequest: IRequest<TResponse>
        {
            mockMediator.Setup(x => x.Send(It.IsAny<TRequest>(), cancellationToken))
                .Callback<IRequest<TResponse>, CancellationToken>((request, _) => { validateRequest((TRequest)request); })
                .ReturnsAsync(response);
        }

        public static void VerifyRequest<TRequest, TResponse>(this Mock<IMediator> mockMediator, CancellationToken cancellationToken, Func < Times> times)
            where TRequest : IRequest<TResponse>
        {
            mockMediator.Verify(x => x.Send(It.IsAny<TRequest>(), cancellationToken), times);
        }
    }
}