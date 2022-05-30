using d360.web.Services;
using FluentAssertions;
using FluentAssertions.Execution;

namespace igx.UnitTests.ServicesTests
{
    public class NotFoundBusinessLayerExceptionTests : ExceptionWithDefaultConstructorsTestBase<NotFoundBusinessLayerException>
    {
	    internal override void ConstructorTest()
	    {
		    var testClass = new NotFoundBusinessLayerException();
		    using (new AssertionScope())
		    {
			    testClass.Message.Should().Be("Entity not found");
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
