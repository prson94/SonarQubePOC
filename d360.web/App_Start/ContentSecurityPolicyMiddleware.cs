using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using d360.core.enums;

using Microsoft.Owin;

namespace d360.web
{
    public class ContentSecurityPolicyMiddleware : BaseMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> _next;

        private readonly Dictionary<string, List<string>> Permissive = new Dictionary<string, List<string>>
        {
            { "default-src", new List<string>{"*", "data:", "blob:", "filesystem:", "ws:", "wss:", "'unsafe-inline'", "'unsafe-eval'" } },
            { "script-src", new List<string>{ "*", "'unsafe-inline'", "'unsafe-eval'" } },
            { "connect-src", new List<string>{ "*", "'unsafe-inline'" } },
            { "img-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "style-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "font-src", new List<string>{ "*", "data:", "blob:", "'unsafe-inline'" } },
            { "frame-src", new List<string>{ "*" } },
            { "frame-ancestors", new List<string>{ "'self'" } },
            { "worker-src", new List<string>{ "blob:" } }
    };

        public ContentSecurityPolicyMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            IOwinResponse response = context.Response;
            IOwinRequest request = context.Request;

            int? companyID = request.Get<int?>("CompanyID");

            if (companyID.HasValue)
            {
                var ctx = CreateOwinCompanyContext(companyID.Value);
                string ancestor = ctx.GetSettingValue<string>(Setting.FramingDomains);

                //If company has a frame setting, a CSP header should be added to allow the frame ancestors
                if (!string.IsNullOrEmpty(ancestor))
                {
                    //Get base permissive CSP
                    Dictionary<string, List<string>> directives = Permissive.ToDictionary(d => d.Key, d => d.Value.ToList());

                    //Add the allowed ancestors from the setting
                    if (!directives.ContainsKey("frame-ancestors"))
                    {
                        directives.Add("frame-ancestors", new List<string>());
                    }

                    List<string> frameAncestors = ancestor.Split(',').ToList().Select(a => a.Trim()).ToList();
                    directives["frame-ancestors"].AddRange(frameAncestors);

                    // Set flag for Global.asax.cs to downgrade cookies as needed, if request is from a valid frame
                    if (IsFrameSessionStart(request, frameAncestors))
                    {
                        request.Set("CompanyFrameRequestStart", true);
                    }

                    response.OnSendingHeaders(s =>
                    {
                        var res = (IOwinResponse)s;

                        string directiveString = string.Join("; ", directives
                            .Where(d => d.Value.Any())
                            .Select(d => d.Key + " " + string.Join(" ", d.Value.ToArray())).ToArray());

                        res.Headers.Add("Content-Security-Policy", new[] { directiveString });
                    }, response);
                }
            }

            await _next.Invoke(environment).ConfigureAwait(false);
        }

        /*
         * Checks if request originated in a frame from a valid domain.
         * When running in a frame, Governs cookies are considered 3rd party, and needs samesite=None, which also requires secure=true
         * Requests from a frame/iframe will have a referrer header and some browsers also sets the draft Sec-Fetch-Dest header indicating
         * the destination element/usage of the request
         * https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Sec-Fetch-Dest
         * If that header is set, only accept the request as a frame session start if the element is a frame/iframe
         */
        private bool IsFrameSessionStart(IOwinRequest request, List<string> frameAncestors)
        {
            if (request.Headers.ContainsKey("Referer"))
            {
                if (request.Headers.ContainsKey("Sec-Fetch-Dest"))
                {
                    var dest = request.Headers.Get("Sec-Fetch-Dest");

                    if (!dest.Equals("iframe") && !dest.Equals("frame"))
                    {
                        return false;
                    }
                }

                /*
                 * Valid frame-ancestors are in <host-source> format: Internet hosts by name or IP address, as well as an optional URL
                 * scheme and/or port number. The site's address may include an optional leading wildcard (the asterisk character, '*'),
                 * and you may use a wildcard (again, '*') as the port number, indicating that all legal ports are valid for the source.
                 * Extract the "host" part of the format excluding the optional leading wildcard.
                 */
                Regex hostRegex = new Regex(@"^(?:(?:https?):\/\/)?\*?([\w\d\.\-]*)(?::(?:\d+|\*))?\/?", RegexOptions.IgnoreCase);
                string referrer = new Uri(request.Headers.Get("Referer")).Host;

                //Check if referrer host matches any of the valid frame ancestors
                return frameAncestors.Any(host =>
                {
                    var m = hostRegex.Match(host);

                    if (m.Success)
                    {
                        var g = m.Groups[0].ToString();

                        //Since the frame ancestor host may contain a wildcard, just match the end of the referrer
                        return referrer.EndsWith(g);
                    }

                    return false;
                });
            }

            return false;
        }
    }
}
