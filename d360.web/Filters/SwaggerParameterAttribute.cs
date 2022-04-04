using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace d360.web.Filters
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class SwaggerParameterAttribute : Attribute
    {
        public SwaggerParameterAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; private set; }

        public string DataType { get; set; }

        public string ParameterType { get; set; }

        public string Description { get; private set; }

        public bool Required { get; set; } = false;

        public Type Enum { get; set; }
    }

    public class SwaggerParameterAttributeFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schema, ApiDescription description)
        {
            var attributes = description.ActionDescriptor.GetCustomAttributes<SwaggerParameterAttribute>();

            if (operation.parameters == null)
                operation.parameters = new List<Parameter>();

            foreach (var attribute in attributes)
            {
                operation.parameters.Add(new Parameter
                {
                    name = attribute.Name,
                    description = attribute.Description,
                    @in = attribute.ParameterType,
                    required = attribute.Required,
                    @type = attribute.DataType,
                    @enum = (attribute.Enum == null) ? null : Enum.GetNames(attribute.Enum).Cast<object>().ToList()
                });
            }

            if (operation.operationId.Equals("ExportTemplates_PostTemplateFile", StringComparison.InvariantCultureIgnoreCase))
            {
                if (operation.parameters == null)
                    operation.parameters = new List<Parameter>(1);
                
                operation.parameters.Add(new Parameter
                {
                    name = "File",
                    @in = "formData",
                    description = "Upload template file",
                    required = true,
                    type = "file"
                });
                operation.consumes.Add("multipart/form-data");
            }
        }
    }
}
