using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System.Threading.Tasks;
using Dapper;
using d3s.community.core;
using System.Data.SqlClient;
using Microsoft.AspNet.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System;
using System.Diagnostics;

namespace d3s.community.startup
{

    //public static class OwinMiddleware
    //{

    //    private class DomainPrefix
    //    {
    //        public int CompanyID { get; set; }
    //        public string UrlPrefix { get; set; }
    //    }

    //    //private static Func<IDictionary<string, object>, Task> next;


    //    //public OwinMiddleware(RequestDelegate next, IHostingEnvironment env)
    //    //{
    //    //    this.next = next;
    //    //    this.env = env;
    //    //}

    //    public static Task CompanyCheck(IDictionary<string, object> environment)
    //    {
    //        var requestHeaders = (IDictionary<string, string[]>)environment["owin.RequestHeaders"];


    //        var conn = new SqlConnection(Constants.COMMUNITY_DATABASE_CONNECTION);
    //        var sql = @"select 
    //                        cd.DatabaseID as CompanyID,
    //                        cd.UrlPrefix 
    //                    from CompanyEnvironmentDetail cd
    //                    join CompanyEnvironment ce on ce.CompanyID = cd.DatabaseID
    //                    join Environment e on e.ID = ce.EnvironmentID";

    //        var prefixes = conn.Query<DomainPrefix>(sql);

    //        string host = requestHeaders["Host"].First();

    //        if (host.Contains(".data3sixty"))
    //            host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
    //        else
    //            host = "demo.dev";

    //        var prefix = prefixes.Where(p => p.UrlPrefix == host).SingleOrDefault();

    //        if (prefix != null)
    //        {
    //            requestHeaders["CompanyDomain"] = new string[] { host };
    //            requestHeaders["CompanyID"] = new string[] { prefix.CompanyID.ToString() };
    //        }

    //        var responseStream = (Stream)environment["owin.ResponseBody"];

    //        return responseStream.WriteAsync(responseStream.)
    //    }
    //}


    public class CompanyCheckMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IHostingEnvironment env;

        private class DomainPrefix
        {
            public int CompanyID { get; set; }
            public string UrlPrefix { get; set; }
        }


        public CompanyCheckMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            this.next = next;
            this.env = env;
        }

        public async Task Invoke(HttpContext context)
        {

            var conn = new SqlConnection(Constants.COMMUNITY_DATABASE_CONNECTION);
            var sql = @"select 
                            DatabaseID as CompanyID,
                            UrlPrefix 
                        from CompanyEnvironmentDetail";

            var prefixes = conn.Query<DomainPrefix>(sql);
            conn.Close();
            conn.Dispose();

            string host = context.Request.Headers["Host"];


            if (host.Contains(".data3sixty"))
                host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
            else
                host = "demo.dev";

            var prefix = prefixes.Where(p => p.UrlPrefix == host).SingleOrDefault();

            if (prefix != null)
            {
                if (!context.Request.Headers.ContainsKey("CompanyDomain"))
                    context.Request.Headers.Add("CompanyDomain", host);
                else
                    context.Request.Headers["CompanyDomain"] = host;
                if (!context.Request.Headers.ContainsKey("CompanyID"))
                    context.Request.Headers.Add("CompanyID", prefix.CompanyID.ToString());
                else
                    context.Request.Headers["CompanyID"] = prefix.CompanyID.ToString();
            }
            else
            {
                await context.Response.WriteAsync($"Company {host} not found");
                Trace.TraceWarning($"Could not locate the company with host address of: {host}");
                return;
            }

            await next.Invoke(context);
        }
    }
}
