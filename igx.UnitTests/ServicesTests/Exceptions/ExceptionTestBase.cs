using System;

namespace igx.UnitTests.ServicesTests
{
	public abstract class ExceptionTestBase<T>
        where T : Exception
    {
        protected abstract T CreateSerializeTestException();
    }
}