using d360.web.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Resources;

namespace igx.UnitTests.ServicesTests
{
    public class ForbiddenBusinessLayerExceptionTests : ExceptionWithDefaultConstructorsTestBase<ForbiddenBusinessLayerException>
    {
        internal override void ConstructorTest()
        {
            var testClass = new ForbiddenBusinessLayerException();
            using (new AssertionScope())
            {
                testClass.Message.Should().Be(ApiMessages.ForbiddenUserNotAuthorizedMessage);
                testClass.InnerException.Should().BeNull();
                testClass.Data.Should().NotBeNull();
                testClass.Data.Count.Should().Be(0);
                testClass.HResult.Should().Be(-2146233088);
                testClass.HelpLink.Should().Be(default);
                testClass.Source.Should().Be(default);
                testClass.StackTrace.Should().Be(default);
                testClass.TargetSite.Should().Be(default);
            }
        }
    }
}