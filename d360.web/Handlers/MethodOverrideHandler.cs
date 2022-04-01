using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace d360.web.Handlers
{
    public class MethodOverrideHandler : DelegatingHandler
    {
        private readonly string[] _methods = { "DELETE" };
        private const string _header = "X-HTTP-Method-Override";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            // Check for HTTP POST with the X-HTTP-Method-Override header.
            if (request.Method == HttpMethod.Post && request.Headers.Contains(_header))
            {
                // Check if the header value is in our methods list.
                var method = request.Headers.GetValues(_header).FirstOrDefault();

                if (_methods.Contains(method, StringComparer.InvariantCultureIgnoreCase))
                {
                    // Change the request method.
                    request.Method = new HttpMethod(method);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
