using d360.core.enums;
using d360.model;
using d360.web.caching;
using Microsoft.Owin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace d360.web
{
    public class CorsMiddleware : BaseMiddleware
    {
        Func<IDictionary<string, object>, Task> _next;
        const string OriginKey = "AllowedOrigins";
        public CorsMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            IOwinResponse response = context.Response;
            bool isPreflight = false;
            int? companyID = context.Request.Get<int?>("CompanyID");

            if (companyID.HasValue && context.Request.Headers.ContainsKey("Origin"))
            {
                var acceptOrigin = context.Request.Headers["Origin"];

                if (companyID.HasValue)
                {
                    isPreflight = context.Request.Method == "OPTIONS";
                    var ctx = CreateOwinCompanyContext(companyID.Value);
                    string originsSetting = ctx.GetSettingValue<string>(Setting.AllowedOrigins);

                    if (!string.IsNullOrEmpty(originsSetting))
                    {
                        var allowOrigin = originsSetting
                            .Split(',')
                            .ToList()
                            .Contains(acceptOrigin);

                        //the requested origin is allowed, set appropriate access headers
                        if (allowOrigin)
                        {
                            context.Response.OnSendingHeaders(s =>
                            {
                                var res = (IOwinResponse)s;

                                res.Headers.Add("Access-Control-Allow-Origin", new string[] { acceptOrigin });
                                res.Headers.Add("Access-Control-Allow-Methods", new string[] { "*" });
                                res.Headers.Add("Access-Control-Allow-Headers", new string[] { "*" });

                                //override status code if this is a valid preflight request
                                if (isPreflight)
                                {
                                    res.StatusCode = (int)HttpStatusCode.OK;
                                    res.ReasonPhrase = HttpStatusCode.OK.ToString();
                                }

                            }, response);
                        }
                    }
                }
            }

            await _next.Invoke(environment);
        }

        private class CompanyOrigin
        {
            public int CompanyID { get; set; }
            public string Origins { get; set; }
        }
    }
}