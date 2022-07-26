using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

using d360.core.exceptions;
using d360.web.Models;

using Resources;

namespace d360.web.Handlers
{
	public class ErrorMessageHandler : DelegatingHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			var response = await base.SendAsync(request, cancellationToken);
			try
			{
				return GenerateResponse(request, response);
			}
			catch (Exception ex)
			{
				return request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
			}
		}

		private HttpResponseMessage GenerateResponse(HttpRequestMessage request, HttpResponseMessage response)
		{
			if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
			{
				var responseMetadata = new ErrorResponse
				{
					message = null,
					title = OthersMessages.BadRequestSubmitted
				};
				var result = request.CreateResponse(response.StatusCode, responseMetadata);

				return result;
			}

			return response;
		}

		private bool IsResponseValid(HttpResponseMessage response)
		{
			int statusCode = (int)response.StatusCode;

			if ((response != null) && (statusCode >= 200 && statusCode < 300))
			{
				return true;
			}

			return false;
		}
	}
}
