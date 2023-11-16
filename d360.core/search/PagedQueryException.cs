using System;

namespace d360.core.search
{
	[Serializable]
	public class PagedQueryException : Exception
	{
		public PagedQueryException()
		{ }
		public PagedQueryException(string message)
			: base(message)
		{ }
		public PagedQueryException(string message, Exception innerException)
			: base(message, innerException)
		{ }
	}
}
