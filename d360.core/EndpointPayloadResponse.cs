using System.Net;

namespace d360.core
{
	public class EndpointPayloadResponse<T>
	{
		public HttpStatusCode Code { get; set; }
		public string Message { get; set; }
		public T Payload { get; set; }
	}
}
