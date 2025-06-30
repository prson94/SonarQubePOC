using System.Net;

namespace services
{
	public class Response<T>
	{
		public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

		public bool IsSuccess { get; set; } = true;

		public string Message { get; set; } = "";

		public T? Data { get; set; }

		public void SetError(HttpStatusCode code, string message)
		{
			StatusCode = code;
			IsSuccess = false;
			Message = message;
		}

		public void SetSuccess(HttpStatusCode code, string message, T data)
		{
			StatusCode = code;
			IsSuccess = true;
			Message = message;
		}
	}
}
