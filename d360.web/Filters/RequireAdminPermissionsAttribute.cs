using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using d360.model;
using d360.web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace d360.web.Filters
{
    public class RequireAdminPermissionsAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            using (var scope = actionContext.ControllerContext.Configuration.DependencyResolver.BeginScope())
            {
                var companyContext = (ICompanyContext) scope.GetService(typeof(ICompanyContext));
                if (companyContext.CurrentResourceIsAdmin == false)
                {
                    throw new ForbiddenBusinessLayerException();
                }
            }

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }
    }
}
