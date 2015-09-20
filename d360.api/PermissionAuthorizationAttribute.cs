using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Security;
using d360.core;
using d360.extensions;

namespace d360.api
{
    public class PermissionAuthorizationAttribute : ActionFilterAttribute
    {
        //Permission RequiredPermission;

        public PermissionAuthorizationAttribute()//(Permission requiredPermission)
        {
            //RequiredPermission = requiredPermission;
        }

        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            var c = filterContext.ControllerContext.Controller as BaseApiController;
            IEnumerable<string> authHeaders;
            filterContext.ControllerContext.Request.Headers.TryGetValues("Authorization", out authHeaders);
            string[] values = authHeaders.First().Split(';');

            if (values.Length != 3) throw new UnauthorizedAccessException();

            var resource = c.AuthenticationSource.ValidateResource(values[1], values[2]);

            if (resource == null)
            {
                filterContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized) { ReasonPhrase = "No permission to access resource." };
            }
            else
            {
                c.Request.Headers.Add("ResourceID", resource.ID.ToString());
                c.Request.Headers.Add("CompanyID", values[0]);
                FormsAuthentication.SetAuthCookie(resource.Username, false);
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
