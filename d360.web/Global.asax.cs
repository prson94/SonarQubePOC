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
using d360.web.Filters;
using d360.web.Models;
using System.Collections.Generic;
using System.Linq;
using d360.model.DataAccessLayer;

namespace d360.web
{
    public class DiModel
    {
        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();


            #region Extension DI

            #region Config Setting Reader            
            builder.RegisterType<d360.extensions.search.ElasticSearchSource>().As<ISearchSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
            builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
            #endregion

            builder.RegisterType<CommunityContext>().As<ICommunityContext>().InstancePerRequest();
            builder.RegisterType<CompanyContext>().As<ICompanyContext>().InstancePerRequest();

            builder.RegisterType<AssetRepository>().As<IAssetRepository>().InstancePerRequest();
            builder.RegisterType<FieldsRepository>().As<IFieldsRepository>().InstancePerRequest();

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
        protected void Application_Start()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine()); //only use razor view engine

            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey = System.Web.Configuration.WebConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            #region Autofac

            Trace.WriteLine("Begin - Dependency Injection With Autofac");

            try
            {
                var di = new DiModel();
                var container = di.GetContainer();
                DependencyResolver.SetResolver(new Autofac.Integration.Mvc.AutofacDependencyResolver(container));
                GlobalConfiguration.Configuration.DependencyResolver = new Autofac.Integration.WebApi.AutofacWebApiDependencyResolver(container);
            }
            catch (Exception )
            {
            }

            Trace.WriteLine("End - Dependency Injection With Autofac");
            
            #endregion

            GlobalConfiguration.Configure(WebApiConfig.Register);
            CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }
    }
}