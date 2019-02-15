using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using Resources;
using SpreadsheetLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("overlays"), Authorize, AiHandleError]
    public class OverlaysController : BaseController
    {
        #region DI

        public OverlaysController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [Route("MyApiCredentialsNg")]
        public JsonNetResult MyApiCredentialsNg()
        {
            if (!Company.CurrentResourceIsAdmin && !this.ShowAllUsersAPIKey())
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);



            var resource = Community.GetById<Resource>(Community.CurrentResourceID);

            return new JsonNetResult
            {
                Data = new
                {
                    PublicKey = resource.APIPublicKey,
                    PrivateKey = resource.APIPrivateKey
                },
                Formatting = Newtonsoft.Json.Formatting.None

            };
        }
    }        
}
