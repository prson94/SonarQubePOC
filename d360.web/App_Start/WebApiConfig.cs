using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http.OData.Extensions;
using d360.media.formatters;
using d360.web.Models.Attributes;
using System.Web.Http.ExceptionHandling;


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

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.AddODataQueryFilter();
            config.EnableCors();
            config.Formatters.Add(new DictionaryXmlMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.EnsureInitialized();
        }
    }
}