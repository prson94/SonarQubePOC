using d360.core;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using d360.extensions.caching;
using System.Diagnostics;
using d360.web.caching;
using d360.core.entities;

namespace d360.web
{
    public class CompanyIDCheckMiddleware: BaseMiddleware
    {
        public class cd
        {
            public int ClientID { get; set; }
            public int CompanyID { get; set; }
            public int DomainSettingID { get; set; }
            public string UrlPrefix { get; set; }
        }

        Func<IDictionary<string, object>, Task> _next;
        public CompanyIDCheckMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        async Task<List<cd>> loadCache()
        {
            var key = "CompanyPrefixes";
            var dict = Cache.GetItem<List<cd>>(key);

            if (dict == null)
            {
                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnn.Open();
                    dict = (await cnn.QueryAsync<cd>(@"
select	E.ClientID, S.CompanyID, S.DomainSettingID, S.UrlPrefix 
from	CompanyDomainSetting S 
		inner join Company E on E.ID = S.CompanyID and E.Status = 'Active'")).ToList();                                        
                }
                Cache.SetItem(key, dict, true, 5);
            }
            return dict;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            var host = context.Request.Headers["Host"];
            try
            {
                var dict = await loadCache();
                bool searchHeaders = true;
                if (host.Contains(".data3sixty"))
                {
                    host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
                    searchHeaders = false;
                }
                if (searchHeaders || !dict.Any(d => d.UrlPrefix == host))
                {
                    if (!string.IsNullOrEmpty(context.Request.Headers["CompanyID"]))
                        host = context.Request.Headers["CompanyID"].ToLower();
                }
                
                if (dict.Any(d => d.UrlPrefix == host))
                {
                    var domainSetting = dict.Single(d => d.UrlPrefix == host);
                    context.Request.Set("CompanyDomain", host);
                    context.Request.Set("ClientID", domainSetting.ClientID);
                    context.Request.Set("CompanyID", domainSetting.CompanyID);
                    context.Request.Set("DomainSettingID", domainSetting.DomainSettingID);
                }
                else
                {
                    context.Response.ContentType = "text/html";
                    context.Response.Write(
                        string.Format(
                            "<div style='" +
                                "font-weight: bold; " +
                                "background: linear-gradient(to bottom, rgb(167, 167, 167) 0%, #e4d6d600 75%);" +
                                "height:100%;" +
                                "width:100%;" +
                                "margin: -8px;" +
                                "position: absolute;'>" +
                                "<div style='" +
                                    "width:600px;" +
                                    "height:200px;" +
                                    "margin-left:auto;" +
                                    "margin-right:auto;" +
                                    "background: #d2d2d2;" +
                                    "margin-top: 100px;" +
                                    "padding: 10px;" +
                                    "box-shadow: 2px 2px 10px 3px rgba(148,148,148,0.75);" +
                                    "text-align: center;" +
                                    "'>" +
                                    "<h1>Error locating company [{0}]</h1>" +
                                    "<p>Please check the url is correct or contact your Administrator.</p>" +
                                "" +
                                "</div>" +
                            "</div>"
                            , host));
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
