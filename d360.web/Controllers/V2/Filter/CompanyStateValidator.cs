using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Linq;
using d360.web.Models;
using d360.model;
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
            }

            if (!isCompanyActive)
            {
                throw new RestApiException(System.Net.HttpStatusCode.Forbidden, ApiMessages.CompanyInactive, "The Govern environment you requested is currently inactive. Please contact Infogix support for additional information.");
            }
        }
    }
}