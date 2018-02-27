using Microsoft.Owin;
using Owin;
//using Microsoft.AspNet.SignalR.Hubs;
using System.Diagnostics;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using d360.web.Models.Attributes;

[assembly: OwinStartup(typeof(d360.web.Startup))]

namespace d360.web
{
    #region Helper Classes

    //public class ErrorHandlingPipelineModule : HubPipelineModule
    //{
    //    protected override void OnIncomingError(Microsoft.AspNet.SignalR.Hubs.ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
    //    {
    //        Debug.WriteLine("=> Exception " + exceptionContext.Error.Message);
    //        if (exceptionContext.Error.InnerException != null)
    //        {
    //            Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message);
    //        }
    //        base.OnIncomingError(exceptionContext, invokerContext);
    //    } 
    //}

    //public class LoggingPipelineModule : HubPipelineModule
    //{
    //    protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
    //    {
    //        Debug.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
    //        return base.OnBeforeIncoming(context);
    //    }
    //    protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
    //    {
    //        Debug.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
    //        return base.OnBeforeOutgoing(context);
    //    }
    //}

    #endregion

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            #region Mvc

            MvcHandler.DisableMvcResponseHeader = true; // Security (by obscurity) disable ASP MVC Version header i.e. X-AspNetMvc-Version:5.2

            GlobalFilters.Filters.Add(new AiHandleErrorAttribute());
            if (!System.Web.HttpContext.Current.IsDebuggingEnabled)
            {
                GlobalFilters.Filters.Add(new RequireHttpsAttribute());
            }
            

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());

            RouteTable.Routes.IgnoreRoute("Content/{*url}");
            RouteTable.Routes.IgnoreRoute("fonts/{*url}");
            RouteTable.Routes.IgnoreRoute("images/{*url}");
            RouteTable.Routes.IgnoreRoute("scripts/{*url}");
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("{resource}.ico");

            RouteTable.Routes.MapMvcAttributeRoutes();  // MVC Routes
                        
            RouteTable.Routes.MapRoute(
                name: "SPA-Fallback",
                url: "{*url}", // a/{*url}
                defaults: new { controller = "Home", action = "App" }
            );

            #endregion

            app.Use<IpRestrictionMiddleware>();
            app.Use<CompanyIDCheckMiddleware>();
            app.Use<UserIDCheckMiddleware>();
            app.Use<CachingHeaderMiddleware>();
        }
    }
}
