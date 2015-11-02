using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.web
{
    public class IpRestrictionMiddleware
    {
        Func<IDictionary<string, object>, Task> _next;
        public IpRestrictionMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }
        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            context.Response.Headers.AppendValues("Platform", new string[] { "Data3Sixty" });
            await _next.Invoke(environment);
        }
    }
}