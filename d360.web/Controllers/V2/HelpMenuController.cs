using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/helpmenu"),
        Authorize,
        StringEnumController
    ]
    public class HelpMenuController : BaseV2ApiController
    {
        readonly IAssetRepository assetRepository;
        public HelpMenuController(CoreComponentSet set, IAssetRepository assetRepository)
            : base(set)
        {
            this.assetRepository = assetRepository;
        }


        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route(""),
           ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetHelpMenuItems()
        {
            const string supportUrl = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern";
            const string aboutUrl = "about";
            var baseUrl = System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"].ToString();

            var items = Company.HelpResources.ToList();           

            foreach (var item in items)
            {
                if(item.isSystem && (item.Url != aboutUrl && item.Url != supportUrl))
                {
                    item.Url = baseUrl + item.Url;
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, items);
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
        }

        [
           HttpPost,
           MapToApiVersion("2.0"),
           Route(""),
           SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
           ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> UpdateHelpMenuItems([FromBody] HelpMenuModel model)
        {
            var deleteRecords = model.Deletes;
            var records = model.Adds;

            if (deleteRecords.Count != 0)
            {
                foreach (var item in deleteRecords)
                {
                    var helpItem = Company.HelpResources.Where(x => x.ID == item.ID).FirstOrDefault();
                    if (helpItem != null && helpItem.isSystem)
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorDeletingDefaultHelpItem));
                    }
                    if (helpItem != null && !helpItem.isSystem)
                    {
                        Company.HelpResources.Remove(helpItem);
                    }
                }
            }
            if (records.Count != 0)
            {
                foreach (var item in records)
                {
                    HelpResource helpItem = Company.HelpResources.Where(x => x.ID == item.ID).FirstOrDefault();

                    if (item.Name.Trim() == "")
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidHelpName));
                    }
                    if (item.Name.Length > 500)
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidHelpNameLength));
                    }
                    if (item.Url.Trim() == "")
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidHelpUrl));
                    }
                    if (item.Url.Length > 2000)
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidHelpUrlLength));
                    }

                    if (helpItem == null)
                    {
                        var uid = Guid.NewGuid();
                        Company.HelpResources.Add(new HelpResource { Name = item.Name, Description = item.Description, 
                        Url = item.Url,uid = uid, isEditable = item.isEditable, visibilty = item.visibilty, 
                        order = item.order,isSystem = item.isSystem});
                    }
                    else
                    {
                        helpItem.Description = item.Description;
                        helpItem.Name = item.Name;
                        helpItem.order = item.order;
                        helpItem.visibilty = item.visibilty;
                        if (!helpItem.isSystem)
                        {
                            helpItem.Url = item.Url;
                            helpItem.isEditable = item.isEditable;
                        }
                    }
                }
            }
            Company.SaveChanges();
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
        }
    }
}