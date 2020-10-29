using d360.core;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace d360.web.Handlers
{
    public class CorsHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool isCorsRequest = request.Headers.Contains("Origin");
            bool isPreflightRequest = request.Method == HttpMethod.Options;
            const string Key = "AllowOrigins";
            const int ExpirationMinutes = 10;

            List<string> origins = new List<string>();
            if (isCorsRequest)
            {
                var origin = request.Headers.GetValues("Origin").First();
                ObjectCache cache = MemoryCache.Default;
                var items = new Dictionary<string, List<string>>();           

                if (cache.Contains(Key))
                    items = (Dictionary<string, List<string>>)cache.Get(Key);

                if (isPreflightRequest)
                {
                    return Task.Factory.StartNew(() =>
                    {
                        var host = request.Headers.GetValues("Host").First();
                        if (host.Contains(".data3sixty"))
                        {
                            host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
                        }

                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

                        if (items.ContainsKey(host))
                        {
                            origins = items[host];
                        }
                        else
                        {
                            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                            {
                                cnn.Open();
                                var value = cnn.Query<string>(@"select coalesce(CS.Value, S.DefaultValue) from Setting S 
                                inner join CompanyDomainSetting DS on DS.UrlPrefix = @prefix
                                left join CompanySetting CS on CS.SettingID = S.ID and CS.CompanyID = DS.CompanyID 
                                where S.ID = 76", new { prefix = host }).FirstOrDefault();

                                if (!string.IsNullOrEmpty(value))
                                {
                                    origins = value.Split(',').ToList();
                                    items.Add(host, origins);
                                    cache.Set(Key, items, DateTime.UtcNow.AddMinutes(ExpirationMinutes));
                                }
                            }

                        }
                        

                        if (!origins.Any(v => v == origin))
                        {
                            response.StatusCode = HttpStatusCode.MethodNotAllowed;
                            return response;
                        }

                        response.Headers.Add("Access-Control-Allow-Origin", origin);
                        response.Headers.Add("Access-Control-Allow-Methods", "*");
                        response.Headers.Add("Access-Control-Allow-Headers", "*");

                        return response;
                    }, cancellationToken);
                }
                else
                {
                    return base.SendAsync(request, cancellationToken).ContinueWith(t =>
                    {
                        HttpResponseMessage resp = t.Result;
                        resp.Headers.Add("Access-Control-Allow-Origin", origin);
                        return resp;
                    });
                }
            }
            else
            {
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}