using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core;
using d360.web.Filters;

namespace d360.web.Controllers
{
    [RoutePrefix("webanalytics"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SWebAnalyticsController : BaseApiController
    {
        #region DI

        public D3SWebAnalyticsController(CoreComponentSet set) : base(set)
        {
#if DEBUG
            Company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif
        }

        #endregion

        public class WebActivityEntity
        {
            public string Activity { get; set; }

            public int ObjectId { get; set; }

            public string ObjectName { get; set; }
        }

        [Route("LogActivity"), HttpPost()]
        [ValidateHttpAntiForgeryToken]
        public void PostLogActivity(WebActivityEntity value)
        {
            try
            {
                string IP = "0.0.0.0";

                try
                {
                    IP = GetClientIp(Request);
                }
                catch
                {
                    //swallow exception here.
                }

                Company.AddWebStatistic(
                    (SystemObjects)Enum.Parse(typeof(SystemObjects), value.ObjectName),
                    value.ObjectId,
                    IP,
                    HttpContext.Current.Request.UserAgent,
                    Company.CurrentCompanyDomain,
                    string.Join(",", HttpContext.Current.Request.UserLanguages),
                    value.Activity,
                    Company.CurrentResourceID,
                    DateTime.UtcNow
                );
            }
            catch (Exception e)
            {
                SendException(e, new Dictionary<string, string>());
                Console.WriteLine(e.Message);
            }
        }

        private string GetClientIp(HttpRequestMessage request = null)
        {
            try
            {
                request = request ?? Request;

                if (request.Properties.ContainsKey("MS_HttpContext"))
                {
                    return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
                }
                else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                {
                    RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)Request.Properties[RemoteEndpointMessageProperty.Name];
                    return prop.Address;
                }
                else if (HttpContext.Current != null)
                {
                    return HttpContext.Current.Request.UserHostAddress;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string>());
                throw;
            }
        }
    }
}
