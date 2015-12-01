using d360.core;
using d360.extensions.caching;
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

        Dictionary<string, List<IpRange>> loadCache()
        {
            var key = "CompanyIpRanges";
            var cache = new MemoryCachingProvider();//RedisCachingProvider();
            var dict = cache.GetItem<Dictionary<string, List<IpRange>>>(key);

            if (dict == null)
            {
                var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                cnn.Open();
                dict = cnn.Query<CompanyIpSetting>(@"select	D.UrlPrefix,
		coalesce(S.Value, '<ips />') as Value 
from	Company C 
		inner join CompanyDomainSetting D on D.CompanyID = C.ID
		left join CompanySetting S on S.CompanyID = C.ID and S.SettingID = 4")
                    .ToDictionary(k => k.UrlPrefix, v => v.Ranges);
                cnn.Close();
                cnn.Dispose();
                cache.SetItem(key, dict, true, 3);
            }
            return dict;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            var host = context.Request.Headers["Host"];
            if (host.Contains(".data3sixty"))
            {
                host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
            }
            else
            {
                host = "demo.dev";
            }

            var dict = loadCache();

            if (dict.ContainsKey(host))
            {
                if (!host.Contains("-d3s")) // If d3s url, automatically allow the user as they are a Data3Sixty employee.
                {
                    var ranges = dict[host];
                    if (ranges.Count > 0)
                    {
                        var currentIp = context.Request.RemoteIpAddress;
                        bool isCurrentIpAllowed = false;

                        foreach (var range in ranges)
                        {
                            var rangeTest = IPAddressRange.Parse(string.Format("{0} - {1}", range.Start, range.End));
                            isCurrentIpAllowed = rangeTest.Contains(IPAddress.Parse(currentIp));

                            if (isCurrentIpAllowed) break;
                        }

                        if (!isCurrentIpAllowed)
                        {
                            context.Response.Write("IP Address Not Allowed");
                            return;
                        }
                    }
                }

                context.Response.Headers.AppendValues("Platform", new string[] { "Data3Sixty" });
            }
            else
            {
                Trace.TraceWarning("Could not locate the company with host address of: {0}", host);
                return;
            }

            await _next.Invoke(environment);
        }
    }
}