using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

using d360.web.Models;

using Resources;

namespace d360.web.Controllers.V2
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            //Check model validity on PUT, POST and DELETE method.
            //Throw error only if it is caused by JSON
            if (actionContext.Request.Method == HttpMethod.Post
                || actionContext.Request.Method == HttpMethod.Put
                || actionContext.Request.Method == HttpMethod.Delete)
            {
                if (!actionContext.ModelState.IsValid)
                {
                    bool isJsonParsingError = actionContext.ModelState.Values.SelectMany(x => x.Errors)
                        .Where(x => x.Exception != null && x.Exception.Source != null)
                        .Any(x => x.Exception.Source == "Newtonsoft.Json");

                    //"   at Newtonsoft.Json.Utilities.EnumUtils.ParseEnum(Type enumType, String value, Boolean disallowNumber)\r\n   at Newtonsoft.Json.Serialization.JsonSerializerInternalReader.EnsureType(JsonReader reader, Object value, CultureInfo culture, JsonContract contract, Type targetType)"
                    if (isJsonParsingError)
                    {
                        var errorTitle = ApiMessages.InvalidJson;
                        var errorMessage = ApiMessages.JSONValidMessage;

                        try
                        {
                            var errors = (from ms in actionContext.ModelState
                                          from ex in ms.Value.Errors
                                          where ex.Exception != null && ex.Exception.InnerException != null
                                          select new
                                          {
                                              IsEnumError = ex.Exception.StackTrace.Contains("d360.core.EnumConverter.ReadJson") || ex.Exception.InnerException.StackTrace.Contains("Newtonsoft.Json.Utilities.EnumUtils.ParseEnum"),
                                              ex.Exception.InnerException.Message,
                                              Field = ms.Key
                                          }).ToList();

                            if (errors.Count > 0)
                            {
                                errorTitle = ApiMessages.InvalidEnumValueInJson;
                                errorMessage = string.Join("; ", errors.Select(e => $"{e.Field} has error: {e.Message}"));
                            }
                        }
                        catch
                        {
                            //swallow exception here.
                        }

                        throw new RestApiException(System.Net.HttpStatusCode.BadRequest, errorTitle, errorMessage);
                    }
                }
            }
        }
    }
}
