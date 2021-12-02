using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.web.Services;
using FluentAssertions;
using MediatR.Pipeline;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class CommonExceptionHandlerTests
    {
        [Theory, AutoData]
        internal async Task RethrowBusinessLayerException_Test(object request, RequestExceptionHandlerState<object> state, CancellationToken cancellationToken)
        {
            var exception = new TestException();
            var testObject = new CommonExceptionHandler<object, object>();
            try
            {
                await ((IRequestExceptionHandler<object, object, Exception>)testObject).Handle(request, exception, state, cancellationToken);
                Assert.True(false, "Exception was not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                var testException = actualException.Should().BeOfType<TestException>().Subject;
                testException.Should().Be(exception);
            }

        }

        [Theory, AutoData]
        internal async Task WrapsNonBusinessLayerException_Test(object request, RequestExceptionHandlerState<object> state, CancellationToken cancellationToken)
        {
            var exception = new Exception();
            var testObject = new CommonExceptionHandler<object, object>();
            try
            {
                await ((IRequestExceptionHandler<object, object, Exception>)testObject).Handle(request, exception, state, cancellationToken);
                Assert.True(false, "Exception was not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                var testException = actualException.Should().BeOfType<UnrecoverableBusinessLayerException>().Subject;
                testException.InnerException.Should().Be(exception);
            }

        }

        private class TestException : BusinessLayerException
        {

        }
    }
}