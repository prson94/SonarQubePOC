using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public abstract class ExceptionTestBase<T>
        where T : Exception
    {
        protected abstract T CreateSerializeTestException();

        [Fact]
        internal virtual void SerializeTest()
        {
            var expectedException = CreateSerializeTestException();
            var mem = new MemoryStream();
            var b = new BinaryFormatter();
            T actualException = default(T);
            try
            {
                b.Serialize(mem, expectedException);
                mem.Position = 0;
                actualException = (T)b.Deserialize(mem);
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }

            actualException.Should().BeEquivalentTo(expectedException);
        }
    }
}