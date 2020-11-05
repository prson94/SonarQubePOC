using Microsoft.Owin;
using Owin;
using System.Diagnostics;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using d360.web.Models.Attributes;

[assembly: OwinStartup(typeof(d360.web.Startup))]

namespace d360.web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            #region Mvc

            MvcHandler.DisableMvcResponseHeader = true; // Security (by obscurity) disable ASP MVC Version header i.e. X-AspNetMvc-Version:5.2
            
            GlobalFilters.Filters.Add(new AiHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NoCacheAttribute());
            if (!System.Web.HttpContext.Current.IsDebuggingEnabled)
            {
                GlobalFilters.Filters.Add(new RequireHttpsAttribute());
                GlobalFilters.Filters.Add(new ValidateContractsAttribute());
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
                name: "API-Fallback",
                url: "api/{*url}", // a/{*url}
                defaults: new { controller = "Home", action = "NotFound" }
            );

            RouteTable.Routes.MapRoute(
                name: "SPA-Fallback",
                url: "{*url}", // a/{*url}
                defaults: new { controller = "Home", action = "App" }
            );


            #endregion
            
            app.Use<IpRestrictionMiddleware>();
            app.Use<CompanyIDCheckMiddleware>();
            app.Use<UserIDCheckMiddleware>();
            app.Use<ContractValidationMiddleware>();
            app.Use<CachingHeaderMiddleware>();
            app.Use<CorsMiddleware>();
        }
    }
}
