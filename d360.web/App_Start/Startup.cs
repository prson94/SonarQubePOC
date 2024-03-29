using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using d360.core;
using d360.core.types;
using d360.extensions;
using d360.featureflags;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Controllers;
using d360.web.Handlers.Exceptions;
using d360.web.Models;
using d360.web.Models.Attributes;
using d360.web.Services;
using d360.web.Services.Favorites;
using d360.web.Utilities;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Owin;
using Owin;
using repositories;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

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

				builder.RegisterType<DateTimeService>().As<IDateTimeService>().SingleInstance();
				builder.RegisterType<DecimalService>().As<IDecimalService>().SingleInstance();
				builder.RegisterType<Int64Service>().As<IInt64Service>().SingleInstance();
				builder.RegisterType<Int32TypeService>().As<IInt32TypeService>().SingleInstance();
				builder.RegisterType<DependencyInjectionTypeServiceProvider>().As<ITypeServiceProvider>().SingleInstance();
				builder.RegisterType<FavoriteRouteMatcherService>().SingleInstance();
				builder.RegisterType<RequestValidator>().As<IRequestValidator>().InstancePerRequest();
				builder.RegisterType<ApplicationUriProvider>().As<IApplicationUriProvider>().InstancePerRequest();

				builder.RegisterControllers(typeof(MvcApplication).Assembly);
				builder.RegisterMediatR(typeof(MvcApplication).Assembly);

				#region Config Setting Reader   

				builder.RegisterType<extensions.search.ElasticSearchSource>().As<ISearchSource>()
					.InstancePerRequest().OnActivating(i => {
						i.Instance.CommunityConnectionString = Config.GetValue<string>(constants.Setting.Community);
					});
				builder.RegisterType<extensions.mail.MandrillMailProvider>().As<IMailProvider>()
					.InstancePerRequest().OnActivating(i => {
						i.Instance.ApiKey = Config.GetValue<string>("MandrillApiKey");
						i.Instance.SubAccount = Config.GetValue<string>("MandrillSubAccount");
					});
				builder.RegisterType<caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
				builder.RegisterType<extensions.events.AzureQueueSource>().As<IQueueSource>()
					.InstancePerRequest().OnActivating(i => {
						i.Instance.StorageConnectionString = Config.GetValue<string>(constants.Setting.Storage);
					});
				builder.RegisterType<extensions.storage.AzureStorageProvider>().As<IStorageProvider>()
					.InstancePerRequest().OnActivating(i => {
						i.Instance.StorageConnectionString = Config.GetValue<string>(constants.Setting.Storage);
					});

				#endregion

				builder.Register(c =>
				{
					return new FeatureFlagService(Config.GetValue<string>(constants.Setting.FeatureFlagKey));
				}).As<IFeatureFlagService>().SingleInstance();

				builder.RegisterType<OidcDiscoveryCache>().AsSelf().InstancePerRequest();

				builder.RegisterType<CoreComponentSet>().As<ICoreComponentSet>().InstancePerRequest();

				builder.RegisterType<extensions.info.UriSecurityContextProvider>().As<ISecurityContextProvider>().InstancePerRequest().OnActivating(i => {
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

				// Logging
				builder.Register(o => {
					TelemetryClient ai = new TelemetryClient();
					TelemetryConfiguration.Active.ConnectionString = Config.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
					return ai;
				}).AsSelf().InstancePerRequest();


				builder.Register(o => {
					var client = o.Resolve<TelemetryClient>();
					var sec = o.Resolve<ISecurityContextProvider>();
					if (sec != null)
					{
						client.Context.GlobalProperties.Add("ClientID", sec.ClientID.ToString());
						client.Context.GlobalProperties.Add("CompanyPrefix", sec.CompanyPrefix);
						client.Context.GlobalProperties.Add("CompanyID", sec.CompanyID.ToString());
						client.Context.GlobalProperties.Add("DomainSettingID", sec.DomainSettingID.ToString());
						var resourceId = sec.ResourceID;
						if (resourceId > 0)
						{
							client.Context.GlobalProperties.Add("ResourceID", resourceId.ToString());
						}
					}
					return new ApplicationInsightsLogger("Govern", client, new ApplicationInsightsLoggerOptions { FlushOnDispose = true, IncludeScopes = true, TrackExceptionsAsExceptionTelemetry = true });
				}).As<ILogger>().InstancePerRequest();

				#region Repositories

				builder.Register(o => {
					var connectionString = Config.GetValue<string>(constants.Setting.Community);
					var cache = o.Resolve<ICachingProvider>();
					var queue = o.Resolve<IQueueSource>();
					var ctx = o.Resolve<ISecurityContextProvider>();
					return new CommunityContext(connectionString, cache, queue, ctx);
					}).As<ICommunityContext>().InstancePerRequest();

				builder.Register(c =>
				{
					var community = c.Resolve<ICommunityContext>();
					var connectionString = community.GetCompanyConnectionString();
					return new repositories.azure.DapperConnectionProvider { ConnectionString = connectionString };
				}).InstancePerRequest();

				builder.RegisterType<CompanyContext>().As<ICompanyContext>().InstancePerRequest();

				builder.RegisterType<CommentRepository>().As<ICommentRepository>().InstancePerRequest();
				builder.RegisterModelModule(); // Register repos from d360.model
				
				builder.RegisterType<repositories.azure.Catalog>().As<ICatalog>()
					.InstancePerRequest().OnActivating(i => {
						var sec = i.Context.Resolve<ISecurityContextProvider>();
						i.Instance.CurrentUserId = sec.ResourceID;
					});
				builder.RegisterType<repositories.dis.Catalog>().As<ICatalog>().InstancePerRequest();
				builder.RegisterType<repositories.azure.Workspaces>().As<IWorkspaces>()
					.InstancePerRequest().OnActivating(i => {
						var sec = i.Context.Resolve<ISecurityContextProvider>();

						i.Instance.CurrentUserId = sec.ResourceID;
						i.Instance.CompanyId = sec.CompanyID;
						i.Instance.WorkspaceId = "";
					});

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
                //supress any startup exception 
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
