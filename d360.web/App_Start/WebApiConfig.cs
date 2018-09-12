using d360.media.formatters;
using d360.web.Filters;
using d360.web.Handlers;
using d360.web.Models.Attributes;
using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.OData.Extensions;
using Swashbuckle.Application;
using System.Globalization;
using System.Web.Http.Description;
using System.Web.Http.Routing;
using Microsoft.Web.Http.Routing;
using Microsoft.Web.Http.Versioning;

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
                foreach (var group in apiExplorer.ApiDescriptions)
                {
                    info.Version(group.Name, $"Govern REST API v{group.ApiVersion}");
                }
            });

            #endregion

            #region Swagger Config

            config
                .EnableSwagger(c => {
                 // If you want the output Swagger docs to be indented properly, enable the "PrettyPrint" option.
                 c.PrettyPrint();

                    // If your API has multiple versions, use "MultipleApiVersions" instead of "SingleApiVersion".
                    // In this case, you must provide a lambda that tells Swashbuckle which actions should be
                    // included in the docs for a given API version. Like "SingleApiVersion", each call to "Version"
                    // returns an "Info" builder so you can provide additional metadata per API version.
                    c.MultipleApiVersions(versionSupportResolver, versionInfoBuilder);

                    //c.MultipleApiVersions(
                    // (apiDesc, version) =>
                    // {
                    //     var path = apiDesc.RelativePath.Split('/');
                    //     var pathVersion = path[1];

                    //     return CultureInfo.InvariantCulture.CompareInfo.IndexOf(pathVersion, version, CompareOptions.IgnoreCase) >= 0;
                    // },
                    // vc =>
                    // {
                    //     vc.Version("2.0", "Data3Sixty Govern API 2.0");
                    //     vc.Version("1.0", "Data3Sixty Govern API 1.0");
                    // });

                 // You can use "BasicAuth", "ApiKey" or "OAuth2" options to describe security schemes for the API.
                 // See https://github.com/swagger-api/swagger-spec/blob/master/versions/2.0.md for more details.
                 // NOTE: These only define the schemes and need to be coupled with a corresponding "security" property
                 // at the document or operation level to indicate which schemes are required for an operation. To do this,
                 // you'll need to implement a custom IDocumentFilter and/or IOperationFilter to set these properties
                 // according to your specific authorization implementation
                 // NOTE: You must also configure 'EnableApiKeySupport' below in the SwaggerUI section
                 c.ApiKey("ApiKey")
                     .Description("API Key Authentication (i.e.   KEY;SECRET)")
                     .Name("Authorization")
                     .In("header");

                 // If you annotate Controllers and API Types with
                 // Xml comments (http://msdn.microsoft.com/en-us/library/b2s063f7(v=vs.110).aspx), you can incorporate
                 // those comments into the generated docs and UI. You can enable this by providing the path to one or
                 // more Xml comment files.
                 c.IncludeXmlComments($@"{System.AppDomain.CurrentDomain.BaseDirectory}\App_Data\Documentation.XML");

                 // In accordance with the built in JsonSerializer, Swashbuckle will, by default, describe enums as integers.
                 // You can change the serializer behavior by configuring the StringToEnumConverter globally or for a given
                 // enum type. Swashbuckle will honor this change out-of-the-box. However, if you use a different
                 // approach to serialize enums as strings, you can also force Swashbuckle to describe them as strings.
                 c.DescribeAllEnumsAsStrings();
             })
                .EnableSwaggerUi(c => {

                    // Use the "DocumentTitle" option to change the Document title.
                    // Very helpful when you have multiple Swagger pages open, to tell them apart.
                    c.DocumentTitle("Data3Sixty Govern API Console");

                    c.EnableDiscoveryUrlSelector();

                    // Use the "InjectStylesheet" option to enrich the UI with one or more additional CSS stylesheets.
                    // The file must be included in your project as an "Embedded Resource", and then the resource's
                    // "Logical Name" is passed to the method as shown below.
                    //c.InjectStylesheet(containingAssembly, "Swashbuckle.Dummy.SwaggerExtensions.testStyles1.css");

                    // By default, swagger-ui will validate specs against swagger.io's online validator and display the result
                    // in a badge at the bottom of the page. Use these options to set a different validator URL or to disable the
                    // feature entirely.
                    //c.SetValidatorUrl("http://localhost/validator");
                    c.DisableValidator();

                    // Specify which HTTP operations will have the 'Try it out!' option. An empty paramter list disables
                    // it for all operations.
                    c.SupportedSubmitMethods("GET", "POST", "PUT", "DELETE");

                    // If your API has multiple versions and you've applied the MultipleApiVersions setting
                    // as described above, you can also enable a select box in the swagger-ui, that displays
                    // a discovery URL for each version. This provides a convenient way for users to browse documentation
                    // for different API versions.
                    c.EnableDiscoveryUrlSelector();
                });

            #endregion

            config.AddODataQueryFilter();
            config.EnableCors();
            config.Formatters.Add(new DictionaryXmlMediaTypeFormatter());

            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));
            config.MessageHandlers.Add(new HeadHandler());

            config.EnsureInitialized();
        }
    }
}