using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

using Swashbuckle.Swagger;

namespace d360.web.Filters
{
    public class Consumes : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var attribute = apiDescription.GetControllerAndActionAttributes<SwaggerConsumesAttribute>().SingleOrDefault();

            if (attribute == null)
            {
                return;
            }

            operation.consumes.Clear();
            operation.consumes = attribute.ContentTypes.ToList();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SwaggerConsumesAttribute : Attribute
    {
        public SwaggerConsumesAttribute(params string[] contentTypes)
        {
            ContentTypes = contentTypes;
        }

        public IEnumerable<string> ContentTypes { get; }
    }

    public class Produces : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var attribute = apiDescription.GetControllerAndActionAttributes<SwaggerProducesAttribute>().SingleOrDefault();
            
            if (attribute == null)
            {
                return;
            }

            operation.produces.Clear();
            operation.produces = attribute.ContentTypes.ToList();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SwaggerProducesAttribute : Attribute
    {
        public SwaggerProducesAttribute(params string[] contentTypes)
        {
            ContentTypes = contentTypes;
        }

        public IEnumerable<string> ContentTypes { get; }
    }
}
