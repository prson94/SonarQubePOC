using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using d360.core;
using d360.core.types;
using d360.extensions;
using d360.web.Controllers;
using d360.web.Handlers.Exceptions;
using d360.web.Models;
using d360.web.Models.Attributes;
using d360.web.Services;
using d360.web.Services.Favorites;
using d360.web.Utilities;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.ApplicationInsights;
using Microsoft.Owin;
using Owin;
using d360.model;

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
                name: "Error-Fallback",
                url: "ErrorBadRequest/{*url}", // a/{*url}
                defaults: new { controller = "Home", action = "BadRequest" }
            );

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

            #region Autofac

            try
            {
				var builder = new ContainerBuilder();

				builder.RegisterType<RuntimeInfo>().As<IRuntimeInfo>().SingleInstance();

				builder.AddWebApiExceptionHandler<DefaultWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<GenericExceptionWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<RestApiExceptionWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<BadRequestWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<NotFoundWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<UnauthorizedWebApi2ExceptionHandler>();
				builder.AddWebApiExceptionHandler<ForbiddenWebApi2ExceptionHandler>();
				builder.RegisterType<WebApi2ExceptionHandlerMediator>().AsSelf().SingleInstance();

				// register telemetry client (instance per request?)
				builder.RegisterType<TelemetryClient>().AsSelf().SingleInstance();

				builder.RegisterType<DateTimeService>().As<IDateTimeService>().SingleInstance();
				builder.RegisterType<DecimalService>().As<IDecimalService>().SingleInstance();
				builder.RegisterType<Int64Service>().As<IInt64Service>().SingleInstance();
				builder.RegisterType<Int32TypeService>().As<IInt32TypeService>().SingleInstance();
				builder.RegisterType<DependencyInjectionTypeServiceProvider>().As<ITypeServiceProvider>().SingleInstance();
				builder.RegisterType<AssetService>().As<IAssetService>().SingleInstance();
				builder.RegisterType<FavoriteRouteMatcherService>().SingleInstance();
				builder.RegisterType<RequestValidator>().As<IRequestValidator>().InstancePerRequest();
				builder.RegisterType<ApplicationUriProvider>().As<IApplicationUriProvider>().InstancePerRequest();

				builder.RegisterControllers(typeof(MvcApplication).Assembly);
				builder.RegisterMediatR(typeof(MvcApplication).Assembly);

				#region Extension DI

				#region Config Setting Reader            
				builder.RegisterType<extensions.search.ElasticSearchSource>().As<ISearchSource>().InstancePerRequest();
				builder.RegisterType<extensions.mail.MandrillMailProvider>().As<IMailProvider>().InstancePerRequest().OnActivating(i => {
					i.Instance.ApiKey = Config.GetValue<string>(constants.MAIL_API_KEY);
					i.Instance.SubAccount = Config.GetValue<string>(constants.MAIL_SUB_ACCOUNT);
				});

				builder.RegisterType<caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
				builder.RegisterType<extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
				builder.RegisterType<extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
				#endregion

				builder.RegisterModelModule();

				builder.RegisterType<LaunchDarkly.Sdk.Server.LdClient>().As<LaunchDarkly.Sdk.Server.LdClient>()
					.SingleInstance()
					.WithParameter("sdkKey", Config.GetValue<string>("LaunchDarklySdkKey"));

				builder.RegisterType<OidcDiscoveryCache>().AsSelf().InstancePerRequest();

				builder.RegisterType<CoreComponentSet>().As<ICoreComponentSet>().InstancePerRequest();

				builder.RegisterType<extensions.info.UriSecurityContextProvider>().As<ISecurityContextProvider>()
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
								i.Instance.ResourceID = ctx.Get<int>("ResourceID");
								i.Instance.IsAdministrator = ctx.Get<bool>("IsAdministrator");
							}
						}
						catch (Exception ex)
						{
							// do nothing.
						}
					});

				#endregion

				#region Repositories

				//builder.Register<repositories.IAssetTypeRepository>(c => {
				//	var community = c.Resolve<ICommunityContext>();
				//	var connectionString = community.GetCompanyConnectionString();
				//	return new model.DataAccessLayer.AssetTypeRepository { ConnectionString = connectionString };
				//}).InstancePerRequest();

				#endregion

				#region Controller DI

				builder.RegisterAssemblyTypes(typeof(HomeController).Assembly).InNamespaceOf<HomeController>().AsSelf();

				#endregion

				var container = builder.Build();

				DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

                app.UseAutofacMiddleware(container);
                app.UseAutofacMvc();

                // For WebAPI:
                var config = GlobalConfiguration.Configuration;
                config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

                app.UseAutofacWebApi(config);
            }
            catch
            {
                //surpress any startup exception 
            }

            #endregion

            app.Use<CompanyIDCheckMiddleware>(); // This must be first, as it checks for active environments and clients.
            app.Use<ClaimMappingsMiddleware>();
            app.Use<UserIDCheckMiddleware>();
            app.Use<IpRestrictionMiddleware>();
            app.Use<CachingHeaderMiddleware>();
            app.Use<CorsMiddleware>();
            app.Use<ContentSecurityPolicyMiddleware>();
        }
    }
}
