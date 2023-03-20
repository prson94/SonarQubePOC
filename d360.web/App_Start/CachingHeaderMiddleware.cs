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

            if (context.Response.ContentType == "application/json")
            {
                context.Response.Headers.Add("Cache-Control", new string[] { "no-cache, no-store, must-revalidate" });
                context.Response.Headers.Add("Pragma", new string[] { "no-cache" });
            }

            // Remove the server version number from being sent with every response.
            context.Response.Headers.Remove("server");

            await _next(environment);
        }
    }
}
