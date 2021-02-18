using d360.core;
using d360.web.caching;
using Dapper;
using Microsoft.Owin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace d360.web
{
    public class ContentSecurityPolicyMiddleware
    {
        Func<IDictionary<string, object>, Task> _next;
        const string FramesKey = "FrameAncestors";
        const int AncestorSettingsID = 77;

        private readonly Dictionary<string, List<string>> Permissive = new Dictionary<string, List<string>>
        {
            { "default-src", new List<string>{"*", "data:", "blob:", "filesystem:", "ws:", "wss:", "'unsafe-inline'", "'unsafe-eval'" } },
            { "script-src", new List<string>{ "*", "'unsafe-inline'", "'unsafe-eval'" } },
            { "connect-src", new List<string>{ "*", "'unsafe-inline'" } },
            { "img-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "style-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "font-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "frame-src", new List<string>{ "*" } },
        };

        public ContentSecurityPolicyMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            IOwinResponse response = context.Response;

            var cache = new MemoryCachingProvider();
            int? companyID = context.Request.Get<int?>("CompanyID");
            ConcurrentBag<CompanyFrameAncestor> ancestors = cache.GetItem<ConcurrentBag<CompanyFrameAncestor>>(FramesKey) ?? new ConcurrentBag<CompanyFrameAncestor>();

            if (companyID.HasValue)
            {
                CompanyFrameAncestor companyAncestor = ancestors.FirstOrDefault(o => o.CompanyID == (int)companyID);
                string ancestor = string.Empty;

                if (companyAncestor == null)
                {
                    using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                    {
                        cnn.Open();
                        ancestor = cnn.Query<string>(@"select coalesce(CS.Value, S.DefaultValue) from Setting S 
                            left join CompanySetting CS on CS.SettingID = S.ID and CS.CompanyID = @companyID
                            where S.ID = @AncestorSettingsID", new { companyID, AncestorSettingsID }).FirstOrDefault();

                        companyAncestor = new CompanyFrameAncestor() { CompanyID = (int)companyID, FrameAncestor = ancestor };
                        ancestors.Add(companyAncestor);
                    }
                }
                else
                {
                    ancestor = companyAncestor.FrameAncestor;
                }
                cache.SetItem(FramesKey, ancestor);

                //If company has a frame setting, a CSP header should be added to allow the frame ancestors
                if(!string.IsNullOrEmpty(ancestor))
                {
                    //Get base permissive CSP
                    Dictionary<string, List<string>> directives = Permissive.ToDictionary(d => d.Key, d => d.Value);

                    //Add the allowed ancestors from the setting
                    if(!directives.ContainsKey("frame-ancestors"))
                    {
                        directives.Add("frame-ancestors", new List<string>());
                    }
                    directives["frame-ancestors"].AddRange(ancestor.Split(',').ToList().Select(a => a.Trim()));

                    response.OnSendingHeaders(s => {
                        var res = (IOwinResponse)s;

                        string directiveString = string.Join("; ", directives
                            .Where(d => d.Value.Any())
                            .Select(d => d.Key + " " + string.Join(" ", d.Value.ToArray())).ToArray());

                        res.Headers.Add("Content-Security-Policy", new string[] { directiveString });
                    }, response);
                }
            }
            await _next.Invoke(environment);
        }

        private class CompanyFrameAncestor
        {
            public int CompanyID { get; set; }
            public string FrameAncestor { get; set; }
        }
    }
}