using d360.core;
using d360.web.caching;
using Dapper;
using Microsoft.Owin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace d360.web
{
    public class CorsMiddleware
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
            var cache = new MemoryCachingProvider();
            bool isPreflight = false;
            int? companyID = context.Request.Get<int?>("CompanyID");
            ConcurrentBag<CompanyOrigin> origins = cache.GetItem<ConcurrentBag<CompanyOrigin>>(OriginKey) ?? new ConcurrentBag<CompanyOrigin>();

            if (context.Request.Headers.ContainsKey("Origin"))
            {
                var acceptOrigin = context.Request.Headers["Origin"];

                if (companyID.HasValue)
                {
                    isPreflight = context.Request.Method == "OPTIONS";
                    CompanyOrigin companyOrigin = origins.FirstOrDefault(o => o.CompanyID == (int)companyID);
                    string originsSetting = string.Empty;

                    if (companyOrigin == null)
                    {
                        using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                        {
                            cnn.Open();
                            originsSetting = cnn.Query<string>(@"select coalesce(CS.Value, S.DefaultValue) from Setting S 
                                left join CompanySetting CS on CS.SettingID = S.ID and CS.CompanyID = @companyID
                                where S.ID = 76", new { companyID }).FirstOrDefault();

                            companyOrigin = new CompanyOrigin() { CompanyID = (int)companyID, Origins = originsSetting };
                            origins.Add(companyOrigin);
                        }
                    }
                    else
                    {
                        originsSetting = companyOrigin.Origins;
                    }
                    cache.SetItem(OriginKey, origins);

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