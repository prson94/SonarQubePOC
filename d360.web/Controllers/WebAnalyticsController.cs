using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers
{
    [RoutePrefix("webanalytics"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SWebAnalyticsController : BaseApiController
    {
        #region DI


        public D3SWebAnalyticsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif
        }

        #endregion

        public class WebActivityModel
        {
            public string Activity { get; set; }
            public int ObjectId { get; set; }
            public string ObjectName { get; set; }
        }

        
        [Route("LogActivity"), HttpPost()]
        public async Task PostLogActivity(WebActivityModel value)
        {
            //write the activity somewhere
            //Company.CurrentResourceID - current user
            //Company.CurrentCompanyID - current company
            //DateTime.UtcNow - current time
            //GetClientIp - client IP Address
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