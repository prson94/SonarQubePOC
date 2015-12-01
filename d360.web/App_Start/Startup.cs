using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR.Hubs;
using System.Diagnostics;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using d360.web.Models.Attributes;

[assembly: OwinStartup(typeof(d360.web.Startup))]

namespace d360.web
{
    #region Helper Classes

    public class ErrorHandlingPipelineModule : HubPipelineModule
    {
        protected override void OnIncomingError(Microsoft.AspNet.SignalR.Hubs.ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            Debug.WriteLine("=> Exception " + exceptionContext.Error.Message);
            if (exceptionContext.Error.InnerException != null)
            {
                Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message);
            }
            base.OnIncomingError(exceptionContext, invokerContext);
        } 
    }

    public class LoggingPipelineModule : HubPipelineModule
    {
        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            Debug.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }
        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            Debug.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }
    }

    #endregion

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            #region SignalR

            //var hubConfiguration = new HubConfiguration() { EnableDetailedErrors = false };

            //GlobalHost.DependencyResolver.UseRedis("d3ssession.redis.cache.windows.net", 6379, "bnIUbvztGIYchNT/VSz4iHHaL/ChYMLsppmXLbJp5Jw=", "UI-SignalR");
            ////GlobalHost.DependencyResolver.UseRedis("d3ssignalr.redis.cache.windows.net", 6380, "8ymYsgGiMttNlZeApex9AAPGmywzEyMnPMJVPfW7dwo=", "UI-SignalR");

            ////var connectionString = constants.SERVICE_BUS_UI;
            ////GlobalHost.DependencyResolver.UseServiceBus(connectionString, "D3S-UI");

            ////GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule()); 
            ////GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule());

            //app.MapSignalR(hubConfiguration);

            #endregion

            #region Mvc

            MvcHandler.DisableMvcResponseHeader = true; // Security (by obscurity) disable ASP MVC Version header i.e. X-AspNetMvc-Version:5.2

            GlobalFilters.Filters.Add(new AiHandleErrorAttribute());
            if (!System.Web.HttpContext.Current.IsDebuggingEnabled)
            {
                GlobalFilters.Filters.Add(new RequireHttpsAttribute());
            }
            //GlobalFilters.Filters.Add(new HandleErrorAttribute { View = "Error" });

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());

            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("{resource}.ico");

            RouteTable.Routes.MapMvcAttributeRoutes();  // MVC Routes

            AreaRegistration.RegisterAllAreas();
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            #endregion

            app.Use<IpRestrictionMiddleware>();
            app.Use<CompanyIDCheckMiddleware>();
            app.Use<UserIDCheckMiddleware>();
            app.Use<CachingHeaderMiddleware>();
        }
    }
}
