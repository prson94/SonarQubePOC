using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Owin;

namespace d360.web
{
    public class CachingHeaderMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> _next;

        public CachingHeaderMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

			if (context.Response.ContentType == "application/json" || (context.Request.Path.HasValue && context.Request.Path.Value.Contains("environment/theme")))
			{
				context.Response.Headers.Add("Cache-Control", new string[] { "no-store" });
			}
			else 
			{
				const int durationInSeconds = 60 * 60 * 12;
				context.Response.Headers.Add("Cache-Control", new string[] { "public,max-age=" + durationInSeconds });
			}

            // Remove the server version number from being sent with every response.
            context.Response.Headers.Remove("server");

            await _next(environment);
        }
    }
}
