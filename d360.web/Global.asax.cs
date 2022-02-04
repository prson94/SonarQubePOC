using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using d360.model;
using d360.extensions;
using Autofac;
using d360.web.Controllers;
using System.Diagnostics;
using System;
using System.Security.Cryptography;
using System.Deployment.Internal.CodeSigning;
using d360.core;
using System.Linq;
using d360.core.types;
using Autofac.Integration.Mvc;
using d360.web.Utilities;
using d360.extensions.caching;
using d360.web.Services;
using MediatR.Extensions.Autofac.DependencyInjection;
using d360.web.Services.Favorites;

namespace d360.web
{
    public class DiModel
    {
        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DateTimeService>().As<IDateTimeService>().SingleInstance();
            builder.RegisterType<DecimalService>().As<IDecimalService>().SingleInstance();
            builder.RegisterType<Int64Service>().As<IInt64Service>().SingleInstance();
            builder.RegisterType<DependencyInjectionTypeServiceProvider>().As<ITypeServiceProvider>().SingleInstance();
            builder.RegisterType<AssetService>().As<IAssetService>().SingleInstance();
            builder.RegisterType<FavoriteRouteMatcherService>().SingleInstance();

            builder.RegisterType<ApplicationUriProvider>().As<IApplicationUriProvider>().SingleInstance();

            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterMediatR(typeof(MvcApplication).Assembly);

            #region Extension DI

            #region Config Setting Reader            
            builder.RegisterType<d360.extensions.search.ElasticSearchSource>().As<ISearchSource>().InstancePerRequest();
            builder.RegisterType<extensions.mail.MandrillMailProvider>().As<IMailProvider>().InstancePerRequest().OnActivating(i => {
                i.Instance.ApiKey = Config.GetValue<string>(constants.MAIL_API_KEY);
                i.Instance.SubAccount = Config.GetValue<string>(constants.MAIL_SUB_ACCOUNT);
            });
            if (Config.GetValue<bool>("RedisEnabled"))
            {
                builder.RegisterType<RedisCachingProvider>().As<ICachingProvider>().InstancePerRequest();
            }
            else
            {
                builder.RegisterType<caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
            }
            builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
            #endregion

            builder.RegisterModelModule();

            builder.RegisterType<LaunchDarkly.Sdk.Server.LdClient>().As<LaunchDarkly.Sdk.Server.LdClient>()
                .SingleInstance()
                .WithParameter("sdkKey", Config.GetValue<string>("LaunchDarklySdkKey"));

            builder.RegisterType<CoreComponentSet>().As<ICoreComponentSet>().InstancePerRequest();

            builder.RegisterType<d360.extensions.info.UriSecurityContextProvider>().As<ISecurityContextProvider>()
                .InstancePerRequest()
                .OnActivating(i => {
                    try
                    {
                        var req = HttpContext.Current.Request;
                        if (req != null)
                        {
                            var ctx = req.GetOwinContext();
                            i.Instance.CompanyPrefix = ctx.Get<string>("CompanyDomain");
                            i.Instance.ClientID = ctx.Get<int>("ClientID");
                            i.Instance.CompanyID = ctx.Get<int>("CompanyID");
                            i.Instance.DomainSettingID = ctx.Get<int>("DomainSettingID");
                            i.Instance.ResourceID= ctx.Get<int>("ResourceID");
                            i.Instance.IsAdministrator = ctx.Get<bool>("IsAdministrator");

                            Trace.TraceInformation("Company Domains: {0}, ResourceID: {1}", ctx.Get<string>("CompanyDomain"), ctx.Get<int>("ResourceID"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Global.asax >> ISecurityContextProvider >> OnActivating >> {0}", ex.GetFullExceptionData());
                    }
                });

            #endregion

            #region Controller DI

            builder.RegisterAssemblyTypes(typeof(HomeController).Assembly).InNamespaceOf<HomeController>().AsSelf();

            #endregion

            return builder.Build();
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

            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey = System.Web.Configuration.WebConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            
            GlobalConfiguration.Configure(WebApiConfig.Register);
            CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
            // set the app insights telemetry initializer so that the user id can be passed with app insights info
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.TelemetryInitializers.Add(new GovernAppInsightsTelemetryInitializer());
        }


    }
}