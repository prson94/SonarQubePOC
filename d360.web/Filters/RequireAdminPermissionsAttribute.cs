using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;

using d360.web.Services;
using Resources;

namespace d360.web.Filters
{
	public class RequireAdminPermissionsAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
		public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
			var owin = HttpContext.Current.GetOwinContext();

			if (owin.Get<bool>("IsAdministrator") == false)
			{
				actionContext.Response = actionContext.Request.CreateErrorResponse(System.Net.HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
			}

			return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }
    }
}
