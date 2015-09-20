using Autofac;
using d360.admin.ui.Controllers;
using d360.extensions;
using d360.model;
using System;
using System.Collections.Generic;
using System.Deployment.Internal.CodeSigning;
using System.Diagnostics;
using System.IdentityModel.Services;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace d360.admin.ui
{
    public class DiModel
    {
        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();

            //SnapConfiguration.For(new AutofacAspectContainer(builder)).Configure(c =>
            //{
            //    c.IncludeNamespace("d360.services.*");
            //    c.Bind<LoggingInterceptor>().To<LoggingAttribute>();
            //});

            builder.RegisterType<CommunityContext>().As<CommunityContext>().InstancePerRequest().AsSelf();

            #region Extension DI

            builder.RegisterType<d360.extensions.caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
            builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
            builder.RegisterType<d360.extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
            builder.RegisterType<d360.extensions.info.UriSecurityContextProvider>().As<ISecurityContextProvider>()
                .InstancePerRequest()
                .OnActivating(i =>
                {
                    var c = HttpContext.Current.Request.Url.DnsSafeHost;
                    var u = HttpContext.Current.User.Identity.Name.ToLower();

                    i.Instance.RawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower();
                    i.Instance.RawUserID = u;
                });

            #endregion

            #region Controller DI

            builder.RegisterAssemblyTypes(typeof(HomeController).Assembly).InNamespaceOf<HomeController>().AsSelf();

            #endregion

            return builder.Build();
        }
    }

    public class MvcApplication : System.Web.HttpApplication
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
            }
            catch
            {
            }

            Trace.WriteLine("End - Dependency Injection With Autofac");

            #endregion

            AreaRegistration.RegisterAllAreas();
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            RouteTable.Routes.MapMvcAttributeRoutes();                                 // MVC Routes

            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }
    }
}
