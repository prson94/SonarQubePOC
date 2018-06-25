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
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace d360.web
{
    public class CompanyIDCheckMiddleware
    {
        public class cd
        {
            public int CompanyID { get; set; }
            public string UrlPrefix { get; set; }
        }

        Func<IDictionary<string, object>, Task> _next;
        public CompanyIDCheckMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        async Task<Dictionary<string, int>> loadCache()
        {
            var key = "CompanyPrefixes";
            var cache = new MemoryCachingProvider();//RedisCachingProvider();
            var dict = cache.GetItem<Dictionary<string, int>>(key);

            if (dict == null)
            {
                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    dict = (await cnn.QueryAsync<cd>("select CompanyID, UrlPrefix from CompanyDomainSetting")).ToDictionary(k => k.UrlPrefix, v => v.CompanyID);                                        
                }
                cache.SetItem(key, dict, true, 5);
            }
            return dict;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            var host = context.Request.Headers["Host"];
            try
            {
                if (host.Contains(".data3sixty"))
                {
                    host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
                }
                //else
                //{
                //    host = "demo.dev";
                //}

                var dict = await loadCache();

                if (dict.ContainsKey(host))
                {
                    context.Request.Set("CompanyDomain", host);
                    context.Request.Set("CompanyID", dict[host]);
                }
                else
                {
                    context.Response.Write(string.Format("Company [{0}] Not Found", host));
                    Trace.TraceWarning("Could not locate the company with host address of: {0}", host);
                    return;
                }
            }
            catch (Exception e)
            {
                //log error
                var properties = new Dictionary<string, string>
                {
                    {"Middleware","CompanyIDCheckMiddleware" },
                    {"Host", host }
                };
                var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();

                telemetry.TrackException(e, properties);
            }
            await _next.Invoke(environment);
        }
    }
}