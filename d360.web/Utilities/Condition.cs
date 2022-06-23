using System;

namespace d360.web.Utilities
{
	public static class Condition
	{
		public static void Require<T, TException>(T value, Func<TException> exceptionFactory)
			where T : class
			where TException: Exception
		{
			IsTrue(value != null, exceptionFactory);
		}

		public static void IsTrue<TException>(bool value, Func<TException> exceptionFactory)
			where TException : Exception
		{
			if (value == false)
			{
				throw exceptionFactory();
			}
		}

		public static void IsFalse<TException>(bool value, Func<TException> exceptionFactory)
			where TException : Exception
		{
			IsTrue(!value, exceptionFactory);
		}
	}
}
