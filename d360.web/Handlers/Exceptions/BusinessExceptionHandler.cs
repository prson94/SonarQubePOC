using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Filters;
using d360.model.helpers.filters;
using Newtonsoft.Json;

namespace d360.web.Handlers.Exceptions
{
	public class BusinessExceptionHandler : ExceptionFilterAttribute
	{
		public override void OnException(HttpActionExecutedContext actionExecutedContext)
		{
			if (actionExecutedContext.Exception is FilterExpressionParserException)
			{
				var ex = actionExecutedContext.Exception as FilterExpressionParserException;
				var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
				var problemDetails = new ProblemDetailsResponse
				{
					Status = (int)ex.StatusCode,
					Detail = errorMessage,
					Title = "Filter expression parse error",
					Type = "error",
					Method = actionExecutedContext.Request.Method.Method,
					Instance = actionExecutedContext.Request.RequestUri.ToString(),

				};
				actionExecutedContext.Response = CreateResponse(problemDetails);
				return;
			}
		}

		public static HttpResponseMessage CreateResponse(ProblemDetailsResponse data)
		{
			var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
			var json = JsonConvert.SerializeObject(data, Formatting.Indented);

			response.Content = new StringContent(json);
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			return response;
		}
	}
}