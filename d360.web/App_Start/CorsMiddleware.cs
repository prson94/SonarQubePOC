using d360.core;
using d360.web.caching;
using Dapper;
using Microsoft.Owin;
using System;
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


            if (context.Request.Headers.ContainsKey("Origin"))
            {
                var acceptOrigin = context.Request.Headers["Origin"];
                if (companyID.HasValue)
                {
                    isPreflight = context.Request.Method == "OPTIONS";
                    string origins = string.Empty;

                    if (!cache.ItemExists<string>(OriginKey))
                    {
                        using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                        {
                            cnn.Open();
                            origins = cnn.Query<string>(@"select coalesce(CS.Value, S.DefaultValue) from Setting S 
                                left join CompanySetting CS on CS.SettingID = S.ID and CS.CompanyID = @companyID
                                where S.ID = 76", new { companyID }).FirstOrDefault();

                            cache.SetItem(OriginKey, origins);
                        }
                    }
                    else
                    {
                        origins = cache.GetItem<string>(OriginKey);
                    }

                    if (!string.IsNullOrEmpty(origins))
                    {
                        var allowOrigin = origins
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
    }
}