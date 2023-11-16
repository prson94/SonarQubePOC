using System.Net;

namespace d360.core.validators
{
	public class WorkHttpStatus
	{
		public HttpStatusCode StatusCode { get; set; }

		public string Error { get; set; }

		public string Message { get; set; }

		public WorkHttpStatus(HttpStatusCode hsc, string err, string msg)
		{
			StatusCode = hsc;
			Error = err;
			Message = msg;
		}
	}
}
