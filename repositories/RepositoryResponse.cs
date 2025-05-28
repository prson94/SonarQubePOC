using System;

namespace repositories
{
	public class RepositoryResponse<T>
	{
		public RepositoryResponse(int statusCode, string message = null)
		{
			Data = default(T);
			IsSuccess = false;
			Message = message;
			StatusCode = statusCode;
		}

		public RepositoryResponse(T data, int statusCode, bool success, string message = null)
		{
			Data = data;
			IsSuccess = success;
			Message = message;
			StatusCode = statusCode;
		}

		public RepositoryResponse(T data, int statusCode, bool success, Exception ex, string message = null)
		{
			Data = data;
			IsSuccess = success;
			Message = message;
			StatusCode = statusCode;
			Ex = ex;
		}

		public int StatusCode { get; set; }

		public bool IsSuccess { get; set; }

		public string Message { get; set; } = "";

		public Exception Ex { get; set; }

		public T Data { get; set; }
	}
}
