using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web.Http.Filters;
using d360.model.helpers.filters;

namespace d360.web.Handlers.Exceptions
{
	public class BusinessExceptionHandler : ExceptionFilterAttribute
	{
		public override void OnException(HttpActionExecutedContext actionExecutedContext)
		{
			base.OnException(actionExecutedContext);
			if (actionExecutedContext.Exception is FilterExpressionParserException)
			{
				var ex = actionExecutedContext.Exception as FilterExpressionParserException;
				actionExecutedContext.Response = CreateResponse(new ProblemDetailsResponse()
				{
					Status = (int)ex.StatusCode,
					Detail = ex.StackTrace,
					Title = ex.Message,
					Type = "error",
					Method = actionExecutedContext.Request.Method.Method,
					Instance = actionExecutedContext.Request.RequestUri.ToString(),

				});
				return;
			}
		}

		public static HttpResponseMessage CreateResponse(ProblemDetailsResponse data)
		{
			var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
			var json = JsonSerializer.Serialize(data);

			response.Content = new StringContent(json);
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			return response;
		}
	}
}