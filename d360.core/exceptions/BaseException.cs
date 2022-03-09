using System;
using System.Net;

namespace d360.core.exceptions
{
    public abstract class BaseException : Exception
    {
        public HttpStatusCode StatusCode { get; internal set; }
        
        public string StatusMessage { get; internal set; }
        
        public string StatusDescription { get; internal set; }

        protected BaseException(HttpStatusCode code, string message, string description = null)
        {
            StatusCode = code;
            StatusMessage = message;
            StatusDescription = description ?? message;
        }
    }
}
