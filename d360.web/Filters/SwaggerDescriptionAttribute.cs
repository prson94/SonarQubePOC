using Resources;
using Swashbuckle.Swagger;
using System;
using System.Linq;
using System.Web.Http.Description;

namespace d360.web.Filters
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class SwaggerDescriptionAttribute : Attribute
    {
        public SwaggerDescriptionAttribute(string descriptionkey)
        {
            DescriptionKey = descriptionkey;
        }

        public string DescriptionKey { get; private set; }
    }

    public class SwaggerDescriptionAttributeFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schema, ApiDescription description)
        {
            var httpParameters = description.ActionDescriptor.GetParameters();

            foreach (var httpParameter in httpParameters)
            {
                var attribute = GetAttribute(httpParameter);
                if (attribute == null)
                {
                    continue;
                }

                var existingSwaggerParameter = GetSwaggerParameter(operation, httpParameter);
                existingSwaggerParameter.description = GetDescription(attribute);
            }
        }

        private static string GetDescription(SwaggerDescriptionAttribute attribute)
        {
            return Swagger.ResourceManager.GetString(attribute.DescriptionKey);
        }

        private static SwaggerDescriptionAttribute GetAttribute(System.Web.Http.Controllers.HttpParameterDescriptor httpParameter)
        {
            var attributes = httpParameter.GetCustomAttributes<SwaggerDescriptionAttribute>();
            if (attributes.Count > 1)
            {
                throw new InvalidOperationException("Impossible to have several SwaggerDescriptionAttribute");
            }

            return attributes.FirstOrDefault();
        }

        private static Parameter GetSwaggerParameter(Operation operation, System.Web.Http.Controllers.HttpParameterDescriptor httpParameter)
        {
            var existingSwaggerParameters = operation.parameters.Where(p => p.name == httpParameter.ParameterName).ToList();
            if (existingSwaggerParameters.Count > 1)
            {
                throw new InvalidOperationException($"Found several swagger parameters named {httpParameter.ParameterName}");
            }

            if (existingSwaggerParameters.Count == 0)
            {
                throw new InvalidOperationException($"Failed to find swagger parameter named {httpParameter.ParameterName}");
            }

            return existingSwaggerParameters.First();
        }
    }
}