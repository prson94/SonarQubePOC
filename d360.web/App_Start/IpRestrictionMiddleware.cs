using d360.core.enums;
using d360.model;
using Microsoft.Owin;
using NetTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.web
{
    public class IpRestrictionMiddleware: BaseMiddleware
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

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            var host = context.Request.Headers["Host"];
            int? companyID = context.Request.Get<int?>("CompanyID");

            try
            {
                if (host.Contains(".data3sixty"))
                {
                    host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
                }

                if (companyID.HasValue && !host.Contains("-d3s") && !host.Contains("-igx")) // If d3s url, automatically allow the user as they are a Data3Sixty employee.
                {
                    var ctx = CreateOwinCompanyContext(companyID.Value);
                    var ipXml = ctx.GetSettingValue<string>(Setting.IpRestriction);

                    var ip = new CompanyIpSetting { Value = ipXml };
                    var ranges = ip.Ranges;

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
                    context.Response.Headers.AppendValues("Platform", new string[] { "Data360 Govern" });
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