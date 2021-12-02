using System;
using AutoFixture.Xunit2;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public abstract class ExceptionWithDefaultConstructorsTestBase<T> : ExceptionTestBase<T>
        where T : Exception
    {
        protected override T CreateSerializeTestException()
        {
            return Activator.CreateInstance<T>();
        }

        [Fact]
        internal virtual void ConstructorTest()
        {
            var testClass = (T)Activator.CreateInstance(typeof(T));
            using (new AssertionScope())
            {
                testClass.Message.Should().Be($"Exception of type '{typeof(T).FullName}' was thrown.");
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

        [Theory, AutoData]
        public virtual void ConstructorWithMessageTest(string message)
        {
            var testClass = (T)Activator.CreateInstance(typeof(T), message);
            using (new AssertionScope())
            {
                testClass.Message.Should().Be(message);
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

        [Theory, AutoData]
        public virtual void ConstructorWithMessageAndExceptionTest(string message, Exception innerException)
        {
            var testClass = (T)Activator.CreateInstance(typeof(T), message, innerException);
            using (new AssertionScope())
            {
                testClass.Message.Should().Be(message);
                testClass.InnerException.Should().BeSameAs(innerException);
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