using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Helpers;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Mvc;

// See http://aspnet13.orcsweb.com/web-api/overview/security/preventing-cross-site-request-forgery-(csrf)-attacks
namespace d360.web.Filters
{
    /// <summary>
    /// Validates antiforgery tokens, plus also checks for http header version used in ajax forms.
    /// </summary>
    public class ValidateHttpAntiForgeryTokenAttribute : AuthorizationFilterAttribute
    {
        public const string RequestVerificationTokenName = "RequestVerificationToken";

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            HttpRequestMessage request = actionContext.ControllerContext.Request;

            try
            {
                if (IsAjaxRequest(request))
                {
                    ValidateRequestHeader(request);
                }
                else
                {
                    AntiForgery.Validate();
                }
            }
            catch (HttpAntiForgeryException e)
            {
                actionContext.Response = request.CreateErrorResponse(HttpStatusCode.Forbidden, e);
            }
        }

        private bool IsAjaxRequest(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues("X-Requested-With", out IEnumerable<string> xRequestedWithHeaders))
            {
                string headerValue = xRequestedWithHeaders.FirstOrDefault();

                if (!string.IsNullOrEmpty(headerValue))
                {
                    return string.Equals(headerValue, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private void ValidateRequestHeader(HttpRequestMessage request)
        {
            string cookieToken = string.Empty;
            string formToken = string.Empty;

            if (request.Headers.TryGetValues(RequestVerificationTokenName, out IEnumerable<string> tokenHeaders))
            {
                string tokenValue = tokenHeaders.FirstOrDefault();

                if (!string.IsNullOrEmpty(tokenValue))
                {
                    string[] tokens = tokenValue.Split(':');
                    
                    if (tokens.Length == 2)
                    {
                        cookieToken = tokens[0].Trim();
                        formToken = tokens[1].Trim();
                    }
                }
            }

            AntiForgery.Validate(cookieToken, formToken);
        }
    }
}
