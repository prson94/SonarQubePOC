using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace d360.web.Models
{
    public class ExamplesOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            SetRequestModelExamples(operation, schemaRegistry, apiDescription);
            SetPrimitives(schemaRegistry, apiDescription);
        }

        private void SetPrimitives(SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            RegisterPrimitiveType(typeof(int), 1);

            void RegisterPrimitiveType(Type type, int value)
            {
                object result = SerializeValue(value);

                var schema = schemaRegistry.GetOrRegister(type);
                schema.example = result;
                schemaRegistry.Definitions[schema.type] = schema;
            }

            object SerializeValue(int value)
            {
                var controllerSerializerSettings = apiDescription?.ActionDescriptor?.ControllerDescriptor?.Configuration?.Formatters?.JsonFormatter?.SerializerSettings;
                var serializerSettings = SerializerSettings(controllerSerializerSettings, null, null, ignoreNulls: true);
                var jsonString = JsonConvert.SerializeObject(value, serializerSettings);
                var result = JsonConvert.DeserializeObject(jsonString);
                return result;
            }
        }

        private static void SetRequestModelExamples(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var controllerSerializerSettings = apiDescription?.ActionDescriptor?.ControllerDescriptor?.Configuration?.Formatters?.JsonFormatter?.SerializerSettings;

            var requestAttributes = apiDescription.GetControllerAndActionAttributes<SwaggerRequestExampleAttribute>();

            foreach (var attr in requestAttributes)
            {
                var schema = schemaRegistry.GetOrRegister(attr.RequestType);

                var parameter = operation.parameters.FirstOrDefault(p => p.@in == "body" && (p.schema?.@ref == schema.@ref || p.schema?.items?.@ref == schema.@ref));

                if (parameter != null)
                {
                    var serializerSettings = SerializerSettings(controllerSerializerSettings, attr.ContractResolver, attr.JsonConverter, ignoreNulls: true);

                    var provider = (IExamplesProvider)Activator.CreateInstance(attr.ExamplesProviderType);

                    // name = attr.RequestType.Name; // this doesn't work for generic types, so need to to schema.ref split

                    var parts = schema.@ref?.Split('/');
                    if (parts == null)
                    {
                        continue;
                    }

                    var name = parts.Last();

                    if (schemaRegistry.Definitions.ContainsKey(name))
                    {
                        schemaRegistry.Definitions[name].example = FormatJson(provider, serializerSettings, false);
                    }
                }
            }
        }

        private static object FormatJson(IExamplesProvider provider, JsonSerializerSettings serializerSettings, bool includeMediaType)
        {
            object examples;
            if (includeMediaType)
            {
                examples = new Dictionary<string, object>
                {
                    {
                        "application/json", provider.GetExamples()
                    }
                };
            }
            else
            {
                examples = provider.GetExamples();
            }

            var jsonString = JsonConvert.SerializeObject(examples, serializerSettings);
            var result = JsonConvert.DeserializeObject(jsonString);
            return result;
        }

        private static JsonSerializerSettings SerializerSettings(JsonSerializerSettings controllerSerializerSettings, IContractResolver attributeContractResolver, JsonConverter attributeJsonConverter, bool ignoreNulls)
        {
            var serializerSettings = DuplicateSerializerSettings(controllerSerializerSettings);
            if (attributeContractResolver != null)
            {
                serializerSettings.ContractResolver = attributeContractResolver;
            }

            if (ignoreNulls)
            {
                serializerSettings.NullValueHandling = NullValueHandling.Ignore; // ignore nulls on any RequestExample properies because swagger does not support null objects https://github.com/OAI/OpenAPI-Specification/issues/229
            }

            if (attributeJsonConverter != null)
            {
                serializerSettings.Converters.Add(attributeJsonConverter);
            }

            return serializerSettings;
        }

        // Duplicate the controller's serializer settings because I don't want to overwrite them
        private static JsonSerializerSettings DuplicateSerializerSettings(JsonSerializerSettings controllerSerializerSettings)
        {
            if (controllerSerializerSettings == null)
            {
                return new JsonSerializerSettings();
            }

            return new JsonSerializerSettings
            {
                SerializationBinder = controllerSerializerSettings.SerializationBinder,
                Converters = new List<JsonConverter>(controllerSerializerSettings.Converters),
                CheckAdditionalContent = controllerSerializerSettings.CheckAdditionalContent,
                ConstructorHandling = controllerSerializerSettings.ConstructorHandling,
                Context = controllerSerializerSettings.Context,
                ContractResolver = controllerSerializerSettings.ContractResolver,
                Culture = controllerSerializerSettings.Culture,
                DateFormatHandling = controllerSerializerSettings.DateFormatHandling,
                DateFormatString = controllerSerializerSettings.DateFormatString,
                DateParseHandling = controllerSerializerSettings.DateParseHandling,
                DateTimeZoneHandling = controllerSerializerSettings.DateTimeZoneHandling,
                DefaultValueHandling = controllerSerializerSettings.DefaultValueHandling,
                Error = controllerSerializerSettings.Error,
                Formatting = controllerSerializerSettings.Formatting,
                MaxDepth = controllerSerializerSettings.MaxDepth,
                MissingMemberHandling = controllerSerializerSettings.MissingMemberHandling,
                NullValueHandling = controllerSerializerSettings.NullValueHandling,
                ObjectCreationHandling = controllerSerializerSettings.ObjectCreationHandling,
                PreserveReferencesHandling = controllerSerializerSettings.PreserveReferencesHandling,
                ReferenceLoopHandling = controllerSerializerSettings.ReferenceLoopHandling,
                TypeNameHandling = controllerSerializerSettings.TypeNameHandling,
            };
        }
    }

    public interface IExamplesProvider
    {
        object GetExamples();
    }

    public class InvalidTypeException : ArgumentException
    {
        public override string ParamName { get; }
        public Type InvalidType { get; }
        public Type ExpectedType { get; }

        public override string Message
        {
            get
            {
                return $"Expected {ParamName} to implement {ExpectedType}. {InvalidType} does not.";
            }
        }

        public InvalidTypeException(string paramName, Type invalidType, Type expectedType)
        {
            ParamName = paramName;
            InvalidType = invalidType;
            ExpectedType = expectedType;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SwaggerRequestExampleAttribute : Attribute
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="requestType">The type passed to the request</param>
        /// <param name="examplesProviderType">A type that inherits from IExamplesProvider</param>
        /// <param name="contractResolver">An optional json contract Resolver if you want to override the one you use</param>
        /// <param name="jsonConverter">An optional jsonConverter to use, e.g. typeof(StringEnumConverter) will render strings as enums</param>
        public SwaggerRequestExampleAttribute(Type requestType, Type examplesProviderType, Type contractResolver = null, Type jsonConverter = null)
        {
            if (examplesProviderType.GetInterface(nameof(IExamplesProvider)) == null)
            {
                throw new InvalidTypeException(
                    paramName: nameof(examplesProviderType),
                    invalidType: examplesProviderType,
                    expectedType: typeof(IExamplesProvider));
            }

            RequestType = requestType;
            ExamplesProviderType = examplesProviderType;
            JsonConverter = jsonConverter == null ? null : (JsonConverter)Activator.CreateInstance(jsonConverter);
            ContractResolver = contractResolver == null ? null : (IContractResolver)Activator.CreateInstance(contractResolver);
        }

        public Type ExamplesProviderType { get; }

        public JsonConverter JsonConverter { get; }

        public Type RequestType { get; }

        public IContractResolver ContractResolver { get; }
    }
}