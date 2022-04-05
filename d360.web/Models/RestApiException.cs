using System;
using System.Net;

namespace d360.web.Models
{
    public class RestApiException : ApplicationException
    {
        public HttpStatusCode Status { get; set; }

        public string Title { get; set; }

        public RestApiException(HttpStatusCode status, string title, string message) : base(message)
        {
            Title = title;
            Status = status;
        }

        public RestApiException(HttpStatusCode status, string message) : base(message)
        {
            Title = "Error occured";
            Status = status;
        }
    }
}
