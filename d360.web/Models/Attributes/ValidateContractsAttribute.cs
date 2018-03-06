using System;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateContractsAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public bool Ignore = false;
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (Ignore)
                return;
            try
            {
                bool contractsAccepted = filterContext.HttpContext.GetOwinContext().Get<bool>("ContractsAccepted");

                if (contractsAccepted == false)
                {
                    filterContext.HttpContext.Response.Redirect("/terms");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}