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

namespace d360.web
{
    public class DiModel
    {
        //string CheckForAccessTokenInHeader()
        //{
        //    string tokenName = "";
        //    string tokenValue = "";

        //    tokenName = "oauth2_access_token";
        //    if (HttpContext.Current.Request.QueryString.AllKeys.Contains(tokenName))
        //    {
        //        tokenValue = HttpContext.Current.Request.QueryString[tokenName];
        //    }

        //    if (string.IsNullOrEmpty(tokenValue))
        //    {
        //        tokenName = "key";
        //        if (HttpContext.Current.Request.QueryString.AllKeys.Contains(tokenName))
        //        {
        //            tokenValue = HttpContext.Current.Request.QueryString[tokenName];
        //        }            
        //    }

        //    return tokenValue;
        //}

        //void CheckForAccessTokenCredentials()
        //{
        //    try
        //    {
        //        var tokenValue = CheckForAccessTokenInHeader();
        //        if (!string.IsNullOrEmpty(tokenValue))
        //        {
        //            string c = "";
        //            try
        //            {
        //                c = HttpContext.Current.Request.Headers["Host"];
        //                //EventLog.WriteEntry("Application", c);
        //            }
        //            catch
        //            {
        //                c = HttpContext.Current.Request.Url.Authority;
        //            }

        //            var rawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();

        //            var cnn = new CommunityContext(
        //                new d360.extensions.caching.MemoryCachingProvider(),
        //                new d360.extensions.queue.DummyQueueSource(),
        //                new d360.extensions.info.UriSecurityContextProvider
        //                {
        //                    RawCompanyID = rawCompanyID,
        //                    UserIDType = UserIdentifierType.AccessToken,
        //                    RawUserID = tokenValue
        //                }
        //            );
        //            var model = cnn.ValidateAccessTokenResource(tokenValue);
        //            if (model != null)
        //            {
        //                if (model.Companies.Contains(cnn.CurrentCompanyID))
        //                {
        //                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(model.Username, "AccessToken"), null);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        //void CheckForApiCredentials()
        //{
        //    try
        //    {
        //        if (HttpContext.Current.Request.Headers["Authorization"] != null) // Comment this IF statement only when performing penetration tests with Metasploit.
        //        {
        //            //var headerValue = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";     // Use this only when performing penetration tests with Metasploit.
        //            var headerValue = HttpContext.Current.Request.Headers.GetValues("Authorization").FirstOrDefault();
        //            if (!string.IsNullOrEmpty(headerValue))
        //            { 
        //                var authValues = headerValue.Split(';');
        //                if (authValues.Length == 2)
        //                {
        //                    string c = "";
        //                    try
        //                    {
        //                        //c = "demo.dev.data3sixty.com";    // Use this only when performing penetration tests with Metasploit.
        //                        c = HttpContext.Current.Request.Headers["Host"];
        //                        //EventLog.WriteEntry("Application", c);
        //                    }
        //                    catch
        //                    {
        //                        c = HttpContext.Current.Request.Url.Authority;
        //                    }
        //                    var rawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();
        //                    var key = authValues[0];
        //                    var secret = authValues[1];
        //                    var cnn = new CommunityContext(
        //                        new d360.extensions.caching.MemoryCachingProvider(),
        //                        new d360.extensions.queue.DummyQueueSource(),
        //                        new d360.extensions.info.UriSecurityContextProvider
        //                        {
        //                            RawCompanyID = rawCompanyID,
        //                            UserIDType = UserIdentifierType.ApiKey,
        //                            RawUserID = key
        //                        }
        //                    );
        //                    var model = cnn.ValidateApiResource(key, secret);
        //                    if (model != null)
        //                    {
        //                        if (model.Companies.Contains(cnn.CurrentCompanyID))
        //                        {
        //                            HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(model.Username, "ApiKey"), null);
        //                        }
        //                    }
        //                }                
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<CommunityContext>().As<CommunityContext>().InstancePerRequest().AsSelf();
            builder.RegisterType<CompanyContext>().As<CompanyContext>().InstancePerRequest().AsSelf();

            #region Extension DI

            #region Config Setting Reader
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
                            var ctx = req.GetOwinContext();
                            i.Instance.CompanyPrefix = ctx.Get<string>("CompanyDomain");
                            i.Instance.CompanyID = ctx.Get<int>("CompanyID");
                            i.Instance.ResourceID= ctx.Get<int>("ResourceID");
                            i.Instance.IsAdministrator = ctx.Get<bool>("IsAdministrator");
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