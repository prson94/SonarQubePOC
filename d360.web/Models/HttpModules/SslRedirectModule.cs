using System;
using System.Web;

namespace d360.web.Models.HttpModules
{
    public class SslRedirectModule : IHttpModule
    {
        HttpApplication Context;

        public void Dispose()
        {
            //clean-up code here.
        }

        public void Init(HttpApplication context)
        {
            //context.LogRequest += new EventHandler(OnLogRequest);
            context.BeginRequest += context_BeginRequest;
            Context = context;
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            if (Context.Request.Headers["X-FORWARDED-PROTO"] != null)
            {
                if (Context.Request.Headers["X-FORWARDED-PROTO"].ToString().ToLower() != "https")
                {
                    Context.Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri.Replace("http://", "https://"));
                }
            }
        }

        public void OnLogRequest(Object source, EventArgs e)
        {
            //custom logging logic can go here
        }
    }
}
