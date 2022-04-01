using System;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            //Required to set no-cache headers on web controller calls. Without this the header will be overwritten with "private" before it is sent to the client
            if (filterContext.HttpContext.Request.HttpMethod == "GET")
            {
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            base.OnResultExecuting(filterContext);
        }
    }
}
