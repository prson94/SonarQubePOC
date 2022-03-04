using System;
using System.Net;

namespace d360.core.exceptions
{
    public class WorkStatusException : Exception
    {
        public HttpStatusCode Status { get; private set; }

        public WorkStatusException() { }
        
        public WorkStatusException(HttpStatusCode status, string message) : base(message)
        {
            Status = status;
        }
    }
}
