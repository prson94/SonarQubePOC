using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

using d360.model;
using d360.web.Models;

using Resources;

namespace d360.web.Controllers.V2
{
    public class ValidateCompanyStateAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            bool isCompanyActive = true;

            try
            {
                var requestScope = actionContext.Request.GetDependencyScope();
                var communityContext = requestScope.GetService(typeof(ICommunityContext)) as ICommunityContext;
                isCompanyActive = communityContext.CurrentCompanySsoModel.IsCompanyActive;
            }
            catch
            {
                //swallow exception here.
            }

            if (!isCompanyActive)
            {
                throw new RestApiException(System.Net.HttpStatusCode.Forbidden, ApiMessages.CompanyInactive, OthersMessages.GovernEnvironmentInactive);
            }
        }
    }
}
