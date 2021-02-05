using System;
using System.Net;

namespace d360.core.exceptions
{
    public class StatusCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; internal set; }

        public StatusCodeException(HttpStatusCode code)
        {
            StatusCode = code;
        }
    }
}
