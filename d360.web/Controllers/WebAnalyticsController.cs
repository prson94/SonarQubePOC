using d360.model;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
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

        public class WebActivityEntity : TableEntity
        {           
            
            public string Activity { get; set; }

            public int ObjectId { get; set; }

            public string ObjectName { get; set; }

            public int ResourceID { get; set; }
            public string ResourceName { get; set; }
            public string IP { get; set; }
            public string UserAgent { get; set; }            
            public string Path { get; set; }
            public string Host { get; set; }
            public string BrowserLanguages { get; set; }
        }

        
        [Route("LogActivity"), HttpPost()]
        public async Task PostLogActivity(WebActivityEntity value)
        {
            //write the activity somewhere
            //Company.CurrentResourceID - current user
            //Company.CurrentCompanyID - current company
            //DateTime.UtcNow - current time
            //GetClientIp - client IP Address
                        
            value.ResourceID = Company.CurrentResourceID;            
            
            value.IP = GetClientIp(Request);
            value.UserAgent = HttpContext.Current.Request.UserAgent;
            value.Host = HttpContext.Current.Request.UrlReferrer.Host;
            value.Path = HttpContext.Current.Request.UrlReferrer.AbsolutePath;
            value.BrowserLanguages = string.Join(",",HttpContext.Current.Request.UserLanguages);
            value.RowKey = Guid.NewGuid().ToString();
            value.PartitionKey = value.ResourceID.ToString();

            try
            {
                var storageAccount = CloudStorageAccount.Parse(d360.core.constants.WEBJOBS_STORAGE_CONNECTION);

                var tableClient = storageAccount.CreateCloudTableClient();

                var table = tableClient.GetTableReference($"WebLogs{Company.CurrentCompanyID}");
                table.CreateIfNotExists();

                var insertOperation = TableOperation.Insert(value);

                await table.ExecuteAsync(insertOperation);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //var retrieveOperation = TableOperation.Retrieve<customerentity>("Harp", "Walter");
            //var result = await table.ExecuteAsync(retrieveOperation);
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