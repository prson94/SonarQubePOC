using System;
using d360.web.Services;

namespace d360.web.Utilities
{
	public static class ExceptionFactory
	{
		public static NotFoundBusinessLayerException NotFound(string entityName)
		{
			return new NotFoundBusinessLayerException($"{entityName} not found.");
		}

		public static ForbiddenBusinessLayerException Forbid()
		{
			return new ForbiddenBusinessLayerException();
		}

		public static ForbiddenBusinessLayerException Forbid(string message)
		{
			return new ForbiddenBusinessLayerException(message);
		}

		public static ForbiddenBusinessLayerException Forbid(string message, Exception innerException)
		{
			return new ForbiddenBusinessLayerException(message, innerException);
		}

		public static NotFoundBusinessLayerException NotFound<T>()
		{
			return NotFound(typeof(T).Name);
		}

		public static ArgumentException ArgumentException(string parameterName, string message)
		{
			return new ArgumentException(message, parameterName);
		}

		public static ArgumentException ArgumentNullException(string parameterName)
		{
			return new ArgumentNullException(parameterName);
		}

		public static ArgumentException ArgumentNullException(string parameterName, string message)
		{
			return new ArgumentNullException(parameterName, message);
		}
	}
}
