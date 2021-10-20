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
        readonly ICompanyContext _company;
        readonly IAssetRepository assetRepository;
        public HelpMenuController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            _company = company;
            this.assetRepository = assetRepository;
        }

        /// <summary>
        /// Gets help menu items.
        /// </summary>
        /// <returns></returns>
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route(""),
           SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Gets help menu items.", typeof(List<HelpResource>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetHelpMenuItems()
        {
            const string supportUrl = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern";
            const string aboutUrl = "about";
            var baseUrl = System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"].ToString();

            try
            {
                var items = Company.HelpResources.ToList();

                foreach (var item in items)
                {
                    if (item.isSystem && (item.Url != aboutUrl && item.Url != supportUrl))
                    {
                        item.Url = baseUrl + item.Url;
                    }
                }

                var response = Request.CreateResponse(HttpStatusCode.OK, items);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates help menu items.
        /// </summary>
        /// <param name="model">The List of items to be add/updated or deleted. Use the Adds to add new or update existing items and Deletes to remove existing itmes.</param>
        /// <returns></returns>
        [
           HttpPost,
           MapToApiVersion("2.0"),
           Route(""),
           SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Help Menu items updated.", typeof(int)),
           SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateHelpMenuItems([FromBody] HelpMenuModel model)
        {
            try
            {
                var deleteRecords = model.Deletes;
                var records = model.Adds;

                if (deleteRecords.Count != 0)
                {
                    foreach (var item in deleteRecords)
                    {
                        var helpItem = _company.HelpResources.Where(x => x.uid == item.uid).FirstOrDefault();
                        if (helpItem != null && helpItem.isSystem)
                        {
                            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorDeletingDefaultHelpItem));
                        }
                        if (helpItem != null && !helpItem.isSystem)
                        {
                            _company.HelpResources.Remove(helpItem);
                        }
                    }
                }
                if (records.Count != 0)
                {
                    foreach (var item in records)
                    {
                        HelpResource helpItem = _company.HelpResources.Where(x => x.uid == item.uid).FirstOrDefault();

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
                            _company.HelpResources.Add(new HelpResource
                            {
                                Name = item.Name,
                                Description = item.Description,
                                Url = item.Url,
                                uid = uid,
                                isEditable = item.isEditable,
                                visibilty = item.visibilty,
                                order = item.order,
                                isSystem = item.isSystem
                            });
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
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }
    }
}