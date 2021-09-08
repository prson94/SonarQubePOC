using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
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
        readonly ICompanyContext _company;
        readonly IAssetRepository assetRepository;
        public HelpMenuController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository)
            : base(community, company)
        {
            _company = company;
            this.assetRepository = assetRepository;
        }


        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route(""),
        ]
        public async Task<IHttpActionResult> GetHelpMenuItems()
        {
            var baseUrl = System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"].ToString();
            var items = Company.HelpResources.ToList();

            foreach (var item in items)
            {
                if(item.isSystem && (item.Url != "about" && item.Url != "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern"))
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
                    var helpItem = _company.HelpResources.Where(x => x.ID == item.ID).FirstOrDefault();
                    if(helpItem != null)
                    {
                        _company.HelpResources.Remove(helpItem);
                    }
                }
            }
            if (records.Count != 0)
            {
                foreach (var item in records)
                {
                    HelpResource helpItem = _company.HelpResources.Where(x => x.ID == item.ID).FirstOrDefault();
                    if (helpItem == null)
                    {
                        var uid = Guid.NewGuid();
                        _company.Query<int>(@"
                    insert into [dbo].[HelpResource]([Name],[Description],[Url],[uid],[isEditable],[visibilty],[order],[isSystem])
                    values(@name,'',@url,@uid,@iseditable,@visibilty,@order,@issystem)
                    ", new { item.Name, item.Url, uid, item.isEditable, item.visibilty, item.order,item.isSystem }).FirstOrDefault();
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
            _company.SaveChanges();
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
        }
    }
}