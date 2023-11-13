using System;

namespace d360.core.search
{
	public class SearchServerConnectionException : Exception
    {
        public string Server { get; set; }
        public string Index { get; set; }
        public SearchServerConnectionException(Exception ex, string server, string index)
            : base("Cannot connect to Search Server.", ex)
        {
            Server = server;
            Index = index;
        }
    }
}
