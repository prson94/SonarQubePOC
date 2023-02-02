using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using d360.web.Utilities;
using Newtonsoft.Json;

namespace d360.web.Handlers.Exceptions
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class WebApi2ExceptionHandlerBase : IWebApi2ExceptionHandler
	{
		protected WebApi2ExceptionHandlerBase(IRuntimeInfo runtimeInfo)
		{
			RuntimeInfo = runtimeInfo;
		}

		protected IRuntimeInfo RuntimeInfo { get; }
		public virtual bool IsDefault => false;
		public abstract bool CanHandle(Exception exception);

		public virtual Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
		{
			var exception = context.Exception;
			var problem = new ProblemDetailsResponse();
			problem.Type = "error";
			problem.Title = "Error";
			problem.Method = context.Request.Method.ToString();
			problem.Instance = context.Request.RequestUri.ToString();
			problem.Status = 500;
			var messages = GetExceptionMessageCollection(exception);
			problem.Detail = messages.FirstOrDefault() ?? string.Empty;
			if (RuntimeInfo.IsReleaseBuild == false || RuntimeInfo.IsDebuggerAttached)
			{
				problem.Extra.Add("messages", messages);
				problem.Extra.Add("stack_trace", exception.StackTrace?.Split(new[]
				{
					"\r\n"
				}, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
			}

			ComposeErrorResponse(context, problem);
			var json = JsonConvert.SerializeObject(problem, Formatting.Indented);
			var response = context.Request.CreateResponse();
			response.StatusCode = (HttpStatusCode)problem.Status;
			response.Content = new StringContent(json, Encoding.UTF8, "application/problem+json");
			context.Result = new ResponseMessageResult(response);
			return Task.CompletedTask;
		}

		private static ICollection<string> GetExceptionMessageCollection(Exception exception)
		{
			var result = new List<string>();
			var ex = exception;
			do
			{
				bool IsParamNameExists = false;
				if (ex is TargetInvocationException)
				{
					continue;
				}
				
				if (ex is ArgumentNullException )
				{
					var aex = (ArgumentNullException)ex;
					if  (!string.IsNullOrEmpty(aex.ParamName))
					{
						IsParamNameExists = true;
					}
				}

				if (ex is ArgumentException)
				{
					var aex = (ArgumentException)ex;
					if (!string.IsNullOrEmpty(aex.ParamName))
					{
						IsParamNameExists = true;
					}
				}

				var msg = ex.Message;
				if (IsParamNameExists)
				{
					msg = msg.Replace("\n", " ").Replace("\r", "");
				}

				result.Add(msg);
				ex = ex.InnerException;
			} while (ex != null);

			return result;
		}

		protected abstract void ComposeErrorResponse(ExceptionHandlerContext context, ProblemDetailsResponse problemDetails);
	}
}
