using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using d360.model;
using d360.extensions;
using Autofac;
using d360.web.Controllers;
using System.Diagnostics;
//using Snap;
//using Snap.Autofac;
//using Castle.DynamicProxy;
using System.Web.Http.OData.Extensions;
using d360.media.formatters;
using Autofac.Configuration;
using System.Security.Cryptography.X509Certificates;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Deployment.Internal.CodeSigning;
using d360.core;
using System.Reflection;
using d360.web.Models.Attributes;
using System.Collections.Generic;
using System.Linq;


namespace d360.web
{
    public class DiModel
    {
        string CheckForAccessTokenInHeader()
        {
            string tokenName = "";
            string tokenValue = "";

            tokenName = "oauth2_access_token";
            if (HttpContext.Current.Request.QueryString.AllKeys.Contains(tokenName))
            {
                tokenValue = HttpContext.Current.Request.QueryString[tokenName];
            }

            if (string.IsNullOrEmpty(tokenValue))
            {
                tokenName = "key";
                if (HttpContext.Current.Request.QueryString.AllKeys.Contains(tokenName))
                {
                    tokenValue = HttpContext.Current.Request.QueryString[tokenName];
                }            
            }

            return tokenValue;
        }

        void CheckForAccessTokenCredentials()
        {
            try
            {
                var tokenValue = CheckForAccessTokenInHeader();
                if (!string.IsNullOrEmpty(tokenValue))
                {
                    string c = "";
                    try
                    {
                        c = HttpContext.Current.Request.Headers["Host"];
                        //EventLog.WriteEntry("Application", c);
                    }
                    catch
                    {
                        c = HttpContext.Current.Request.Url.Authority;
                    }

                    var rawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();

                    var cnn = new CommunityContext(
                        new d360.extensions.caching.MemoryCachingProvider(),
                        new d360.extensions.queue.DummyQueueSource(),
                        new d360.extensions.info.UriSecurityContextProvider
                        {
                            RawCompanyID = rawCompanyID,
                            UserIDType = UserIdentifierType.AccessToken,
                            RawUserID = tokenValue
                        }
                    );
                    var model = cnn.ValidateAccessTokenResource(tokenValue);
                    if (model != null)
                    {
                        if (model.Companies.Contains(cnn.CurrentCompanyID))
                        {
                            HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(model.Username, "AccessToken"), null);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        void CheckForApiCredentials()
        {
            try
            {
                if (HttpContext.Current.Request.Headers["Authorization"] != null) // Comment this IF statement only when performing penetration tests with Metasploit.
                {
                    //var headerValue = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";     // Use this only when performing penetration tests with Metasploit.
                    var headerValue = HttpContext.Current.Request.Headers.GetValues("Authorization").FirstOrDefault();
                    if (!string.IsNullOrEmpty(headerValue))
                    { 
                        var authValues = headerValue.Split(';');
                        if (authValues.Length == 2)
                        {
                            string c = "";
                            try
                            {
                                //c = "demo.dev.data3sixty.com";    // Use this only when performing penetration tests with Metasploit.
                                c = HttpContext.Current.Request.Headers["Host"];
                                //EventLog.WriteEntry("Application", c);
                            }
                            catch
                            {
                                c = HttpContext.Current.Request.Url.Authority;
                            }
                            var rawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();
                            var key = authValues[0];
                            var secret = authValues[1];
                            var cnn = new CommunityContext(
                                new d360.extensions.caching.MemoryCachingProvider(),
                                new d360.extensions.queue.DummyQueueSource(),
                                new d360.extensions.info.UriSecurityContextProvider
                                {
                                    RawCompanyID = rawCompanyID,
                                    UserIDType = UserIdentifierType.ApiKey,
                                    RawUserID = key
                                }
                            );
                            var model = cnn.ValidateApiResource(key, secret);
                            if (model != null)
                            {
                                if (model.Companies.Contains(cnn.CurrentCompanyID))
                                {
                                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(model.Username, "ApiKey"), null);
                                }
                            }
                        }                
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();

            //SnapConfiguration.For(new AutofacAspectContainer(builder)).Configure(c =>
            //{
            //    c.IncludeNamespace("d360.services.*");
            //    c.Bind<LoggingInterceptor>().To<LoggingAttribute>();
            //});

            builder.RegisterType<CommunityContext>().As<CommunityContext>().InstancePerRequest().AsSelf();
            builder.RegisterType<CompanyContext>().As<CompanyContext>().InstancePerRequest().AsSelf();

            #region Extension DI

            #region Config Setting Reader
            //builder.RegisterModule(new ConfigurationSettingsReader());
            builder.RegisterType<d360.extensions.search.AzureSearchSource>().As<ISearchSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
            builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
            #endregion

            builder.RegisterType<d360.extensions.info.UriSecurityContextProvider>().As<ISecurityContextProvider>()
                .InstancePerRequest()
                .OnActivating(i => {

                    try
                    {
                        var req = HttpContext.Current.Request;

                        if (req != null)
                        {
                            string c = "";
                            try
                            {
                                c = req.Headers["Host"];
                                //EventLog.WriteEntry("Application", c);
                            }
                            catch
                            {
                                c = req.Url.Authority;
                            }

                            Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> DNS Authority is: {0}, Host is: {1}", req.Url.Authority, c);

                            Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> Begin: Check API for credentials");
                            CheckForApiCredentials();
                            Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> End: Check API for credentials");

                            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                            {
                                Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> Begin: Check Access Token for credentials");
                                CheckForAccessTokenCredentials();
                                Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> End: Check Access Token for credentials");                            
                            }
                            
                            var u = "";

                            if (HttpContext.Current.User.Identity.IsAuthenticated)
                            {
                                u = HttpContext.Current.User.Identity.Name.ToLower();
                            }

                            if (c.Contains(".data3sixty"))
                            {
                                i.Instance.RawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();
                            }
                            else
                            {
                                i.Instance.RawCompanyID = "demo.dev";
                            }
                            
                            i.Instance.RawUserID = u;

                            Trace.TraceInformation("Global.asax >> ISecurityContextProvider >> OnActivating >> Raw Company = {0}, Raw User = {1}", i.Instance.RawCompanyID, i.Instance.RawUserID);
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

            #region Hub DI

            //builder.RegisterHubs(Assembly.GetExecutingAssembly());
            //builder.RegisterHubs(typeof(HomeController).Assembly);
            //builder.RegisterType<SocialHub>().ExternallyOwned();

            #endregion

            return builder.Build();
        }
    }

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            #region Autofac

            Trace.WriteLine("Begin - Dependency Injection With Autofac");

            try
            {
                var di = new DiModel();
                var container = di.GetContainer();
                DependencyResolver.SetResolver(new Autofac.Integration.Mvc.AutofacDependencyResolver(container));
                GlobalConfiguration.Configuration.DependencyResolver = new Autofac.Integration.WebApi.AutofacWebApiDependencyResolver(container);
            }
            catch (Exception ex)
            {
            }

            Trace.WriteLine("End - Dependency Injection With Autofac");
            
            #endregion

            GlobalConfiguration.Configure(WebApiConfig.Register);
            CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }
    }
}