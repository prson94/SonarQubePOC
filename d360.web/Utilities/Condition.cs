using System;

namespace d360.web.Utilities
{
	public static class Condition
	{
		public static void Require<T>(T value, Func<Exception> exceptionFactory)
			where T : class
		{
			if (value == null)
			{
				throw exceptionFactory();
			}
		}
	}
}
