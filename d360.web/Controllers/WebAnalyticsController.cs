using d360.core;
using d360.model;
using d360.web.Filters;
using Microsoft.Web.Http;
using System;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers
{
    [ApiVersion("1.0"), RoutePrefix("webanalytics"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SWebAnalyticsController : BaseApiController
    {
        #region DI


        public D3SWebAnalyticsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
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
        public async Task PostLogActivity(WebActivityEntity value)
        {
            var IP = GetClientIp(Request);            
            
            try
            {
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
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)this.Request.Properties[RemoteEndpointMessageProperty.Name];
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
    }
    
}