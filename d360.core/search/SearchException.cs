using System;

namespace d360.core.search
{
	public class SearchException : Exception
    {
        public SearchException(Exception ex)
            : base("An error occurred in search.", ex)
        {
        }
    }
}
