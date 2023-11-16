using System;

namespace d360.core.search
{
	public class SearchResultsException : Exception
    {
        public SearchResultsException(Exception ex)
            : base("An error occurred while trying to get search results.", ex)
        {
        }
    }
}
