using System.Threading.Tasks;
using AutoFixture;
using igx.UnitTests.Core;
using Moq;
using Moq.Language;
using Moq.Language.Flow;

namespace igx.UnitTests
{
	public static class MockExtensions
	{
		public static TestException ThrowsTestExceptionAsync<TMock, TResult>(this IReturns<TMock, Task<TResult>> mock) where TMock : class
		{
			return ThrowsTestExceptionAsync(mock, FixtureProvider.Create());
		}

		public static TestException ThrowsTestExceptionAsync<TMock, TResult>(this IReturns<TMock, Task<TResult>> mock, IFixture fixture) where TMock : class
		{
			var exception = fixture.Create<TestException>();
			mock.ThrowsAsync(exception);
			return exception;
		}

		public static TestException ThrowsTestException(this IThrows mock)
		{
			return ThrowsTestException(mock, FixtureProvider.Create());
		}

		public static TestException ThrowsTestException(this IThrows mock, IFixture fixture)
		{
			var exception = fixture.Create<TestException>();
			mock.Throws(exception);
			return exception;
		}

		public static TResult ReturnsNewValueAsync<TMock, TResult>(this IReturns<TMock, Task<TResult>> mock) where TMock : class
		{
			return ReturnsNewValueAsync(mock, FixtureProvider.Create());
		}

		public static TResult ReturnsNewValueAsync<TMock, TResult>(this IReturns<TMock, Task<TResult>> mock, IFixture fixture) where TMock : class
		{
			var value = fixture.Create<TResult>();
			mock.ReturnsAsync(() => value);
			return value;
		}

		public static TResult ReturnsNewValue<TMock, TResult>(this IReturns<TMock, TResult> mock, IFixture fixture) where TMock : class
		{
			var value = fixture.Create<TResult>();
			mock.Returns(() => value);
			return value;
		}

		public static TResult ReturnsNewValue<TMock, TResult>(this IReturns<TMock, TResult> mock) where TMock : class
		{
			return ReturnsNewValue(mock, FixtureProvider.Create());
		}

		public static IReturnsResult<TMock> ReturnsNullAsync<TMock, TResult>(this IReturns<TMock, Task<TResult>> mock) where TMock : class
		{
			return mock.ReturnsAsync(() => default);
		}

		public static IReturnsResult<TMock> ReturnsNull<TMock, TResult>(this IReturns<TMock, TResult> mock) where TMock : class
		{
			return mock.Returns(() => default);
		}
	}
}
