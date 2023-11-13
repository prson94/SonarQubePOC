using System;

namespace d360.core.search
{
	[Serializable]
	public class SearchIndexException : Exception
	{
		public SearchIndexException()
		{ }
		public SearchIndexException(string message)
			: base(message)
		{ }
		public SearchIndexException(string message, Exception innerException)
			: base(message, innerException)
		{ }
	}
}
