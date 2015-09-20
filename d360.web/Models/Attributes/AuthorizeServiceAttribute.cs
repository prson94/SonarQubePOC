using d360.core;
using d360.core.entities;
using d360.model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
//using Dapper;

namespace d360.web.Models.Attributes
{
    public class AuthorizeServiceAttribute: AuthorizeAttribute
    {
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            try
            {
                IEnumerable<string> outValues;
                if(actionContext.Request.Headers.TryGetValues("Authorization", out outValues))
                {
                    var authValues = outValues.ToList()[0].Split(';');
                    if (authValues.Length == 2)
                    {
                        var c = System.Web.HttpContext.Current.Request.Url.DnsSafeHost;
                        var rawCompanyID = c.Substring(0, c.IndexOf(".data3sixty")).ToLower(); 
                        var key = authValues[0];
                        var secret = authValues[1];
                        var cnn = new CommunityContext(
                            new d360.extensions.caching.MemoryCachingProvider(), 
                            new d360.extensions.queue.DummyQueueSource(), 
                            new d360.extensions.info.UriSecurityContextProvider { 
                                RawCompanyID = rawCompanyID, UserIDType = UserIdentifierType.ApiKey, RawUserID = key 
                            }
                        );
                        var model = cnn.ValidateApiResource(key, secret);
                        if (model != null)
                        {
                            if (model.Companies.Contains(cnn.CurrentCompanyID))
                            {
                                actionContext.RequestContext.Principal = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(model.Username), null);
                            }
                        }
                    }
                    else
                    {
                        base.OnAuthorization(actionContext);
                    }
                }
                else
                {
                    base.OnAuthorization(actionContext);
                }
            }
            catch (Exception)
            {
                base.OnAuthorization(actionContext);
            }
        }
    }
}