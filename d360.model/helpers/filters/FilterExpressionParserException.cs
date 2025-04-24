using System;
using System.Net;

namespace d360.model.helpers.filters
{
    public class FilterExpressionParserException : Exception
    {
		public HttpStatusCode StatusCode { get; }
		public FilterExpressionParserException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(message)
        {
			StatusCode = statusCode;
		}

        public FilterExpressionParserException(string message, Exception inner, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(message, inner)
        {
			StatusCode = statusCode;
		}

		
	}
}
