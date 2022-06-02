using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
	[Serializable]
	public class NotFoundBusinessLayerException : UnrecoverableBusinessLayerException
	{
		public NotFoundBusinessLayerException() : this(CreateMessage())
		{
		}

		public NotFoundBusinessLayerException(string message) : this(message, null)
		{
		}

		public NotFoundBusinessLayerException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected NotFoundBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		private static string CreateMessage(string message = null)
		{
			return message ?? "Entity not found";
		}

		public static NotFoundBusinessLayerException Create(string entityName)
		{
			return new NotFoundBusinessLayerException($"{entityName} not found.");
		}

		public static NotFoundBusinessLayerException Create<T>()
		{
			return Create(typeof(T).Name);
		}
	}
}
