using d360.core;
using d360.extensions.caching;
using d360.web.caching;
using Dapper;
using Microsoft.Owin;
using NetTools;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.web
{
    public class IpRestrictionMiddleware
    {
        public class CompanyIpSetting
        {
            public CompanyIpSetting()
            {
                ranges = null;
            }

            public string UrlPrefix { get; set; }
            public string Value { get; set; }

            List<IpRange> ranges;
            public List<IpRange> Ranges {
                get {
                    if (ranges == null)
                        ranges = XElement.Parse(Value).Elements("ip").Select(i => new IpRange { Start = i.Element("start").Value, End = i.Element("end").Value }).ToList();
                    return ranges;
                }
            }
        }


        public class IpRange
        {
            public string Start { get; set; }
            public string End { get; set; }
        }

        Func<IDictionary<string, object>, Task> _next;
        public IpRestrictionMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        async Task<Dictionary<string, List<IpRange>>> loadCache()
        {
            var key = "CompanyIpRanges";
            Dictionary<string, List<IpRange>> dict = null;
            var cache = new MemoryCachingProvider();
            
            if (cache != null)
            {
                dict = cache.GetItem<Dictionary<string, List<IpRange>>>(key);
            }

            if (dict == null)
            {
                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnn.Open();
                    dict = (await cnn.QueryAsync<CompanyIpSetting>(@"select	D.UrlPrefix,
		coalesce(S.Value, '<ips />') as Value 
from	Company C 
		inner join CompanyDomainSetting D on D.CompanyID = C.ID
		left join CompanySetting S on S.CompanyID = C.ID and S.SettingID = 4")).ToDictionary(k => k.UrlPrefix, v => v.Ranges);
                    
                }
                cache.SetItem(key, dict, true, 1);
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
                
                Trace.TraceInformation("Host is : {0}", host);

                var dict = await loadCache();

                if (dict.ContainsKey(host))
                {
                    if (!host.Contains("-d3s") && !host.Contains("-igx")) // If d3s url, automatically allow the user as they are a Data3Sixty employee.
                    {
                        var ranges = dict[host];
                        if (ranges.Count > 0)
                        {
                            Trace.TraceInformation("Range Count is: {0}", ranges.Count);

                            var currentIp = context.Environment["server.RemoteIpAddress"].ToString(); 
                            bool isCurrentIpAllowed = false;

                            foreach (var range in ranges)
                            {
                                Trace.TraceInformation("{0} - {1}", range.Start, range.End);

                                var rangeTest = IPAddressRange.Parse(string.Format("{0} - {1}", range.Start, range.End));
                                isCurrentIpAllowed = rangeTest.Contains(IPAddress.Parse(currentIp));

                                if (isCurrentIpAllowed) break;
                            }

                            if (!isCurrentIpAllowed)
                            {
                                context.Response.Write(string.Format("IP Address [{0}] Not Allowed", currentIp));
                                return;
                            }
                        }
                    }

                    context.Response.Headers.AppendValues("Platform", new string[] { "Data3Sixty" });
                }
                else
                {
                    Trace.TraceWarning("Could not locate the company with host address of: {0}", host);
                   
                }
            }
            catch (Exception e)
            {
                //log error
                var properties = new Dictionary<string, string>
                {
                    {"Middleware","IpRestrictionMiddleware" },
                    {"Host", host }
                };
                var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();
                
                telemetry.TrackException(e, properties);                
            }


            await _next.Invoke(environment);
        }
    }
}