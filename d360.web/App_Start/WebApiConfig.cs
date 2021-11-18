using d360.web.Filters;
using d360.web.Handlers;
using d360.web.Models;
using d360.web.Models.Attributes;
using d360.web.Models.Formatters;
using Microsoft.Web.Http.Routing;
using Microsoft.Web.Http.Versioning;
using Swashbuckle.Application;
using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.OData.Extensions;
using System.Web.Http.Routing;

namespace d360.web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Services.Add(typeof(IExceptionLogger), new AiExceptionLogger());

            
            var hideErrorDetails = System.Configuration.ConfigurationManager.AppSettings["security:surpressApiErrorDetails"];

            if ((hideErrorDetails ?? "").ToLower() == "true")
            {
                config.Filters.Add(new ExceptionHandlingAttribute());
            }

            // Web API routes
            var constraintResolver = new DefaultInlineConstraintResolver() { ConstraintMap = { ["apiVersion"] = typeof(ApiVersionRouteConstraint) } };
            config.MapHttpAttributeRoutes(constraintResolver);
            
            #region API Versioning

            config.AddApiVersioning(o => {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
                o.ApiVersionSelector = new CurrentImplementationApiVersionSelector(
                    new ApiVersioningOptions { ApiVersionReader = new UrlSegmentApiVersionReader() }
                );
                o.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader()
                );
            }); // API default Version assumed to be 1.0.

            var apiExplorer = config.AddVersionedApiExplorer(options => {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

            var versionSupportResolver = new Func<ApiDescription, string, bool>((apiDescription, version) => apiDescription.GetGroupName() == version);

            var versionInfoBuilder = new Action<VersionInfoBuilder>(info => {
                var HelpBaseUri = System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"];
                HelpBaseUri += "Default.htm#f-api/api-overview.htm";

                foreach (var group in apiExplorer.ApiDescriptions.OrderByDescending(i=>i.Name) )
                {
                    var v = info.Version(group.Name, $"Govern REST API v{group.ApiVersion}");

                    var desc = $@"
Below you will find a list of various REST services to access information or to modify content within your Govern environment. 
When modifying content on assets, please be aware that you will need to use the API Names on field definitions when updating or referencing content.<br/><br/>
For general API usage and instructions please see the <a href='{HelpBaseUri}' target='_blank'>API documentation in the Govern Help</a>.";

                    if (group.Name == "v1") {
                        desc += "<br><br><b>The Govern v1 API has been deprecated and will not be supported in a future release.&nbsp;&nbsp;Please use the improved v2 API where possible.</b>";
                    }

                    v.Description(desc);

                }
                
            });

            #endregion

            #region Swagger Config

            config
                .EnableSwagger(c => {
                    c.OperationFilter<Consumes>();
                    c.OperationFilter<Produces>();
                    c.OperationFilter<ExamplesOperationFilter>();
                    c.OperationFilter<SwaggerParameterAttributeFilter>();
                    c.OperationFilter<SwaggerDescriptionAttributeFilter>();
                    c.SchemaFilter<SwaggerExcludeFilter>();
                    c.DocumentFilter<SwaggerOrderFilter>();
                    c.PrettyPrint();
                    c.DescribeAllEnumsAsStrings(false);
                    c.MultipleApiVersions(versionSupportResolver, versionInfoBuilder);
                    c.ApiKey("ApiKey")
                         .Description("API Key Authentication (i.e.   KEY;SECRET)")
                         .Name("Authorization")
                         .In("header");
                     c.IncludeXmlComments($@"{System.AppDomain.CurrentDomain.BaseDirectory}\App_Data\Documentation.XML");
                     c.DescribeAllEnumsAsStrings();
                 })
                .EnableSwaggerUi(c => {
                    c.DocumentTitle("Data3Sixty Govern API Console");
                    c.EnableDiscoveryUrlSelector();
                    c.CustomAsset("index", typeof(WebApiConfig).Assembly, "d360.web.Content.Swagger.index.html");
                    c.InjectStylesheet(typeof(WebApiConfig).Assembly, "d360.web.Content.Swagger.swagger.css");
                    c.DisableValidator();
                    c.SupportedSubmitMethods("GET", "POST", "PUT", "DELETE");
                    c.EnableDiscoveryUrlSelector();                    
                });

            #endregion

            config.AddODataQueryFilter();
            config.EnableCors();
            
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new GuidConverter());
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            // default is "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK" capital f's mean include none zero values so between 0 and 7 precision lower case f means always DQ+ cant handle big F's
            config.Formatters.JsonFormatter.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'";
            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));
            config.MessageHandlers.Add(new HeadHandler());
            config.MessageHandlers.Add(new ErrorMessageHandler());
            config.MessageHandlers.Add(new MethodOverrideHandler());
            config.Filters.Add(new ApplicationExceptionFilterAttribute());

            config.EnsureInitialized();
        }
    }
}