using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace d360.web.Controllers.V2
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                throw new Exception("You have not provided a valid JSON structure for this request.");
            }
        }
    }
}