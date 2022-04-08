using System;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;

namespace d360.web.Filters
{
    public class StringEnumControllerAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            var formatter = controllerSettings.Formatters.JsonFormatter;
            controllerSettings.Formatters.Remove(formatter);
            formatter = new System.Net.Http.Formatting.JsonMediaTypeFormatter
            {
                SerializerSettings = {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
                    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'",
                }
            };

            formatter.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            formatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));

            controllerSettings.Formatters.Insert(0, formatter);
        }
    }
}
