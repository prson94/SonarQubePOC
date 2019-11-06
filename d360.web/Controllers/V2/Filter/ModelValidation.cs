using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Linq;

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

                    if (isJsonParsingError)
                        throw new Exception("You have not provided a valid JSON structure for this request.");
                }
            }
        }
    }
}