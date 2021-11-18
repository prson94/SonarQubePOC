using System;
using System.Threading;
using MediatR;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
    internal static class TestExtensions
    {
        public static void SetupMediator<TRequest, TResponse>(this Mock<IMediator> mockMediator, Action<TRequest> validateRequest, TResponse response)
            where TRequest: IRequest<TResponse>
        {
            mockMediator.Setup(x => x.Send(It.IsAny<TRequest>(), CancellationToken.None))
                .Callback<IRequest<TResponse>, CancellationToken>((request, _) => { validateRequest((TRequest)request); })
                .ReturnsAsync(response);
        }
    }
}