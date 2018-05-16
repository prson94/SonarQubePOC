using d360.media.formatters;
using d360.web.Filters;
using d360.web.Models.Attributes;
using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.OData.Extensions;

namespace d360.web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            
            // disabled for now
            //config.Filters.Add(new Filters.OrgAuthenticationAttribute());
            //config.Filters.Add(new Filters.BasicAuthenticationAttribute());

            config.Services.Add(typeof(IExceptionLogger), new AiExceptionLogger());

            
            var hideErrorDetails = System.Configuration.ConfigurationManager.AppSettings["security:surpressApiErrorDetails"];

            if ((hideErrorDetails ?? "").ToLower() == "true")
            {
                config.Filters.Add(new ExceptionHandlingAttribute());
            }


            // Web API routes            
            config.MapHttpAttributeRoutes();

            config.AddODataQueryFilter();
            config.EnableCors();
            config.Formatters.Add(new DictionaryXmlMediaTypeFormatter());

            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));

            config.EnsureInitialized();
        }
    }
}