using Autofac;
using d360.web.Handlers.Exceptions;
using d360.web.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Deployment.Internal.CodeSigning;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;



namespace d360.web
{
    public static class AutofacExtensions
    {
	    public static void AddWebApiExceptionHandler<T>(this ContainerBuilder builder)
			where T: IWebApi2ExceptionHandler
		{
		    builder.RegisterType<T>().As<IWebApi2ExceptionHandler>().SingleInstance();
	    }
    }

    public class MvcApplication : HttpApplication
    {
        protected void Application_BeginRequest()
        {
            // Set the locale based on the accept language header in the request for only UI resources (strings)
            InternationalizationUtilities.SetUserLocale(setCulture: false);
        }

        protected void Application_PreSendRequestHeaders(object sender, EventArgs e)
        {
            // GOV-14170 server and x-powered-by still appearing on some files in govern
            Response.Headers.Remove("Server");
            Response.Headers.Remove("X-Powered-By");

            if (((Response.StatusCode == 404 && Request.Path != "/api/Nofound") ||
                            (Response.StatusCode == 400 && Request.Path != "/ErrorBadRequest/BadReq")) && Request.Path.StartsWith("/api/") && Response.ContentType.ToLower() == "text/html")
            {

                bool isInvalidCharInUrl = CheckUrlValidity(Request.RawUrl);

                if (Response.StatusCode == 404 && isInvalidCharInUrl)
                {
                    Response.Clear();
                    Response.Redirect(Response.ApplyAppPathModifier("~/api/Nofound"));
                    return;
                }
                if (Response.StatusCode == 400 && isInvalidCharInUrl)
                {
                    Response.Clear();
                    Response.Redirect(Response.ApplyAppPathModifier("~/ErrorBadRequest/BadReq"));
                    return;
                }
            }

            /*
             * If Govern is accessed in a frame, cookies will be considered 3rd party cookies by the ancestor page
             * so to work, the SameSite flag needs to be set to "None", and when SameSite is set to none, the Secure flag
             * must be set.
             * Cookie settings only needs to be downgraded when Response is setting new cookies and if the request originated
             * from a frame. To track if the session is "framed", a separate Frame cookie is set on the first request when it's
             * possible to deduct that it originated in a frame. The frame-cookie settings are derived from the authentication cookie
             */

            if (Response.Cookies.Count == 0)
            {
                return;
            }

            string frameRequestCookieId = System.Web.Security.FormsAuthentication.FormsCookieName + "Frame";
            bool framedRequest = false;

            if (Request.Cookies.AllKeys.Contains(frameRequestCookieId))
            {
                framedRequest = true;
            }
            else
            {
                try
                {
                    var req = HttpContext.Current?.Request;
                    if (req != null)
                    {
                        var ctx = req.GetOwinContext();
                        if (ctx.Get<bool>("CompanyFrameRequestStart"))
                        {
                            //Request comes from a valid frame, set cookie to indicate a framed session
                            framedRequest = true;
                            HttpCookie framecookie = new HttpCookie(frameRequestCookieId, "1")
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = System.Web.Security.FormsAuthentication.FormsCookiePath,
                                Domain = System.Web.Security.FormsAuthentication.CookieDomain
                            };
                            Response.AppendCookie(framecookie);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if(framedRequest)
            {
                foreach (string s in Response.Cookies.AllKeys)
                {
                    HttpCookie c = Response.Cookies.Get(s);
                    c.SameSite = SameSiteMode.None;
                    c.Secure = true;
                }
            }
        }

        protected void Application_Start()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine()); //only use razor view engine

            //GOV-14022 remove the X-Frame-Options from the forms / password reset we add this in the web.config this avoids it appearing 2x
            System.Web.Helpers.AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

			TelemetryConfiguration.Active.ConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["APPLICATIONINSIGHTS_CONNECTION_STRING"];
			DisableAppInsightsIfInDebug();

			GlobalConfiguration.Configure(WebApiConfig.Register);
            CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }

		[Conditional("DEBUG")]
		private static void DisableAppInsightsIfInDebug()
		{
			TelemetryConfiguration.Active.DisableTelemetry = true;
		}

		private bool CheckUrlValidity(string urlwithparameter)
        {
            string url = urlwithparameter;
            bool returnVar = false;
            if (urlwithparameter.Contains("?"))
            {
                int lendata =  urlwithparameter.LastIndexOf("?", StringComparison.InvariantCulture);
                url = urlwithparameter.Substring(0, lendata);
            }
            string Checkchar = url.Substring(url.Length - 1, 1).ToLowerInvariant();

            string invalidcharList = " &%*<>?:";

            if (invalidcharList.IndexOf(Checkchar,StringComparison.InvariantCulture) > -1)
            {
                returnVar = true;
            }

            return returnVar;
        }
    }
}

