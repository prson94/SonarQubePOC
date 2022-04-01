using System;
using System.Reflection;
using System.Web.Mvc;

namespace d360.web.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class NonNullableParametersAttribute : ActionMethodSelectorAttribute
    {
        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
        {
            var methodParams = methodInfo.GetParameters();

            foreach (var parameterInfo in methodParams)
            {
                if (parameterInfo.HasDefaultValue)
                {
                    continue;
                }

                var paramType = parameterInfo.ParameterType;

                if (!IsSimpleType(paramType))
                {
                    continue;
                }

                var value = controllerContext.Controller.ValueProvider.GetValue(parameterInfo.Name);

                if (value == null || value.AttemptedValue == null || !CanParse(value.AttemptedValue, paramType, value.Culture))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSimpleType(Type t)
        {
            return t.IsPrimitive || t.IsEnum || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid);
        }

        private static bool CanParse(object rawValue, Type destinationType, IFormatProvider formatProvider)
        {
            try
            {
                if (destinationType.IsEnum)
                {
                    return (Enum.Parse(destinationType, rawValue.ToString()) != null);
                }
                else
                {
                    return (Convert.ChangeType(rawValue, destinationType, formatProvider) != null);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
