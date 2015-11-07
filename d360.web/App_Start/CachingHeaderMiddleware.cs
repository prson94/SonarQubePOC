using d360.core;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using System.Web.Caching;
using d360.extensions.caching;
using d360.core.entities;
using System.Diagnostics;

namespace d360.web
{
    public class CachingHeaderMiddleware
    {
        Func<IDictionary<string, object>, Task> _next;
        public CachingHeaderMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            context.Response.Headers.Add("If-Modified-Since", new string[]{ "01 Jan 1970 00:00:00 GMT"});
            if (context.Response.ContentType == "application/json") {
                context.Response.Headers.Add("Cache-Control", new string[] { "no-cache, no-store, must-revalidate" });
                context.Response.Headers.Add("Pragma", new string[] { "no-cache" });
            }
            context.Response.Headers.Add("Expires", new string[] { "0" });
            await _next.Invoke(environment);
        }
    }
}