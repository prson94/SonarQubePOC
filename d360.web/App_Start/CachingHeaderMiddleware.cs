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

            // security headers - https://www.owasp.org/index.php/List_of_useful_HTTP_headers 
            // clickjacking protection, prevent the site from being placed in a frameset not originating from D3S
            //context.Response.Headers.Add("X-Frame-Options", new string[] { "SAMEORIGIN" });

            //Setting this header will prevent the browser from interpreting files as something else than declared by the content type in the HTTP headers.
            //context.Response.Headers.Add("X-Content-Type-Options", new string[] { "nosniff" });

            // Turn on browser xss protection and render blank page.  Its usually on by default however this reenables it if the user has turned it off
            //context.Response.Headers.Add("X-XSS-Protection", new string[] { "1; mode=block" });
            //X - XSS - Protection:1; mode = block
            //context.Response.Headers.Add("Expires", new string[] { "0" });
            // Remove the server version number from being sent with every response.
            context.Response.Headers.Remove("server");

            await _next.Invoke(environment);
        }
    }
}
