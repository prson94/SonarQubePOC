using repositories;
using System.Net;

namespace d360.web.Extensions
{
	public static class RepositoryExtensions
	{
		public static HttpStatusCode GetHttpStatusCode<T>(this RepositoryResponse<T> response) 
		{
			return (HttpStatusCode)response.StatusCode;
		}
	}
}