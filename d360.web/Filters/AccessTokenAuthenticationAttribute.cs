using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;


namespace d360.web.Filters
{

    public class AccessTokenResultWithChallenge : IHttpActionResult
    {
        private readonly IHttpActionResult next;

        public AccessTokenResultWithChallenge(IHttpActionResult next)
        {
            this.next = next;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(
                                    CancellationToken cancellationToken)
        {
            var response = await next.ExecuteAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Headers.WwwAuthenticate.Add(
                    new AuthenticationHeaderValue(
                        "Bearer",//"WWW-Authenticate", 
                        "authorization_uri=\"https://login.windows.net/21a2b0d9-a4b4-449e-af0b-f22a7129b71f\",resource_id=f2177994-e15f-4340-b0d9-4f987352f222"//https://data3sixty.com/ui"
                    )
                );  //new AuthenticationHeaderValue("WWW-Authenticate", "authorization_uri=https://login.windows.net"));
            }
            /*  "authorization_uri=\"https://login.windows.net/{TENANT}\",resource_id={AUDIENCE}";    */
            return response;
        }
    }

    public class AccessTokenAuthenticationAttribute : Attribute, IAuthenticationFilter
    {     

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = context.Request;

            IPrincipal principal = null;

            if (principal == null)
            {
                // Authentication was attempted but failed. Set ErrorResult to indicate an error.
                context.ErrorResult = new AuthenticationFailureResult("Invalid username or password", request);
            }
            else
            {
                // Authentication was attempted and succeeded. Set Principal to the authenticated user.
                context.Principal = principal;
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new ResultWithChallenge(context.Result);
            return Task.FromResult(0);
        }

        public virtual bool AllowMultiple { get { return false; } }
    }
}