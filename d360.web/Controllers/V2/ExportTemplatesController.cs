using d360.core.entities;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    [ApiExplorerSettings(IgnoreApi = false)]
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/exporttemplates"), Authorize
    ]
    public class ExportTemplatesController : BaseApiController
    {
        #region DI

        public ExportTemplatesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {

        }

        #endregion

        /// <summary>
        /// Returns all asset export templates
        /// </summary>
        /// <returns>An array of asset export template records</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<AssetTypeExportTemplate>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse))
        ]
        public async Task<IEnumerable<dynamic>> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return (await Company.QueryAsync<dynamic>("" +
                "select t.ID, t.AssetTypeID, a.uid as AssetTypeUID, t.Name, t.Description,t.IncludeFields,t.ExportViewType,t.IncludeUrl,t.IncludeParent,t.UsageNotes,CASE WHEN t.templatefile IS NULL THEN 0 ELSE 1 END as HasTemplateFile " +
                "from AssetTypeExportTemplate t " +
                "left join AssetType a ON t.AssetTypeID = a.ID " +
                "order by t.Name, t.ID"));
        }

        /// <summary>
        /// Returns all asset export templates for an asset type
        /// </summary>
        /// <returns>An array of asset export template records</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{assetTypeUID}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<AssetTypeExportTemplate>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse))
        ]
        public async Task<IEnumerable<dynamic>> Get(Guid assetTypeUID)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return (await Company.QueryAsync<dynamic>("" +
                "select t.ID, t.AssetTypeID, a.uid as AssetTypeUID, t.Name, t.Description,t.IncludeFields,t.ExportViewType,t.IncludeUrl,t.IncludeParent,t.UsageNotes,CASE WHEN t.templatefile IS NULL THEN 0 ELSE 1 END as HasTemplateFile " +
                "from AssetTypeExportTemplate t " +
                "left join AssetType a ON t.AssetTypeID = a.ID " +
                "where a.uid = @assetTypeUID " +
                "order by t.Name, t.ID ",  new { assetTypeUID = assetTypeUID.ToString() }));
        }

        /// <summary>
        /// Deletes a Export Template based on the specified ID
        /// </summary>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{id}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetTypeExportTemplate)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> DeleteById(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var res = await Company.Database.Connection.ExecuteAsync("delete AssetTypeExportTemplate where id = @id", new { id = id });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Creates a new Export Template record.  
        /// </summary>
        /// <returns>ExportTemplate model of the created item if item already exists http confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route(""),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Item already exists", typeof(ErrorResponse)),
        ]
        public async Task<AssetTypeExportTemplate> Post(AssetTypeExportTemplate model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //Validate and map asset type uid to to id
            if (string.IsNullOrEmpty(model.AssetTypeUID.ToString()))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));
            model.AssetTypeID = Company.AssetTypes.FirstOrDefault(t => t.uid == model.AssetTypeUID)?.ID ?? 0;

            //validate the model input
            if (model.ID > 0 || string.IsNullOrEmpty(model.Name) || model.AssetTypeID <= 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //validate the assettype id is a valid asset typeid
            if (!Company.AssetTypes.Any(x => x.uid == model.AssetTypeUID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid existing Asset Type."));

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("insert into AssetTypeExportTemplate (Name, Description, AssetTypeID, IncludeFields,ExportViewType,IncludeUrl,IncludeParent,UpdatedBy,UpdatedOn,UsageNotes) values(@n,@d,@atID,@i,@e,@url,@parent,@upd,@updOn,@notes)", new { atID = model.AssetTypeID, n = model.Name, d = model.Description, i = model.IncludeFields, e = model.ExportViewType, url = model.IncludeUrl, parent = model.IncludeParent, upd = Company.CurrentResourceID, updOn = DateTime.UtcNow, notes = model.UsageNotes });

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Item already exists"));
            }

            return model;
        }

        /// <summary>
        /// Get all styles for the specified template
        /// </summary>
        /// <param name="templateId">template Id</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("Styles/{templateId:int}"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse))
        ]
        public IEnumerable<AssetTypeExportTemplateStyle> GetStyles(int templateId)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            var styles = Company.AssetTypeExportTemplateStyles.Where(x => x.AssetTypeExportTemplateID == templateId).ToList();
            styles.ForEach(x =>
            {
                x.BgColor = x.BackgroundColor.HasValue ? ColorTranslator.ToHtml(Color.FromArgb(x.BackgroundColor.Value)) : "#FFFFFF";
                x.TextColor = x.Color.HasValue ? ColorTranslator.ToHtml(Color.FromArgb(x.Color.Value)) : "#000000";
            });
            return styles;

        }

        /// <summary>
        /// Create new  syle for the template
        /// </summary>
        /// <param name="model">AssetTypeExportTemplateStyle</param>
        /// <returns></returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("Style"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<AssetTypeExportTemplateStyle> SaveTemplateStyle(AssetTypeExportTemplateStyle model)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            if (!Company.AssetTypeExportTemplates.Any(x => x.ID == model.AssetTypeExportTemplateID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Template not found"));

            if (!string.IsNullOrEmpty(model.BgColor))
                model.BackgroundColor = ColorTranslator.FromHtml(model.BgColor).ToArgb();

            if (!string.IsNullOrEmpty(model.TextColor))
                model.Color = ColorTranslator.FromHtml(model.TextColor).ToArgb();

            Company.Add(model);
            await Company.SaveChangesAsync();

            return model;
        }

        /// <summary>
        /// Update  style for a template
        /// </summary>
        /// <param name="id">style id</param>
        /// <param name="model">ArtifactTypeExportTemplateStyle</param>
        /// <returns></returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("Style/{id}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<AssetTypeExportTemplateStyle> UpdateTemplateStyle(int id, AssetTypeExportTemplateStyle model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //validate the model input
            if (model.ID <= 0 || model.AssetTypeExportTemplateID <= 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //check that there is a export template exists
            if (!Company.AssetTypeExportTemplateStyles.Any(x => x.ID == model.ID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid existing export template style."));
            var data = Company.GetById<AssetTypeExportTemplateStyle>(model.ID);
            data.IsBold = model.IsBold;
            data.Column = model.Column;
            data.Row = model.Row;

            if (!string.IsNullOrEmpty(model.BgColor))
                data.BackgroundColor = ColorTranslator.FromHtml(model.BgColor).ToArgb();

            if (!string.IsNullOrEmpty(model.TextColor))
                data.Color = ColorTranslator.FromHtml(model.TextColor).ToArgb();

            Company.Entry(data).State = System.Data.Entity.EntityState.Modified;
            var res = await Company.SaveChangesAsync();
            if (res > 0) return model; // updated

            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Export Template Style not found to update."));

        }

        /// <summary>
        /// Deletes a Style based on the specified ID
        /// </summary>
        /// <param name="id">style id</param>
        /// <returns></returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("Style/{id}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> DeleteStyle(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var res = await Company.Database.Connection.ExecuteAsync("delete AssetTypeExportTemplateStyle where id = @id", new { id = id });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Uploads a new Export Template template file for the specified export template.  
        /// </summary>
        /// <returns>Http 200 if upload was successful.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("TemplateFile/{templateId}"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "Error while opening file.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> PostTemplateFile(int templateId)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (!Company.AssetTypeExportTemplates.Any(x => x.ID == templateId))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Template not found"));

            byte[] template = null;
            try
            {
                if (context.Request.Files.Count > 0)
                {
                    var file = context.Request.Files[0];
                    var target = new MemoryStream();
                    file.InputStream.CopyTo(target);
                    template = target.ToArray();
                }
            }
            catch
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error while opening file"));
            }

            var res = await Company.Database.Connection.ExecuteAsync("update AssetTypeExportTemplate  set TemplateFile = @t where ID = @id", new { @t = template, @id = templateId });

            return Request.CreateResponse(HttpStatusCode.OK, "updated");
        }


        /// <summary>
        /// Updates the specified Export Template
        /// </summary>
        /// <returns>Http Status code OK and the item if updated, Http Status code of Not Found if item could not be updated</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{id}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Export Template not found to update.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "AssetType does not support Export Template.", typeof(ErrorResponse))
        ]
        public async Task<AssetTypeExportTemplate> Put(int id, AssetTypeExportTemplate model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //Validate and map asset type uid to to id
            if (string.IsNullOrEmpty(model.AssetTypeUID.ToString()))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));
            AssetType assetType = Company.AssetTypes.FirstOrDefault(t => t.uid == model.AssetTypeUID);
            model.AssetTypeID = assetType?.ID ?? 0;
            if(assetType.Class != core.enums.AssetTypeClass.Glossary)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "AssetType does not support Export Template."));

            //validate the model input
            if (model.ID <= 0 || string.IsNullOrEmpty(model.Name) || model.AssetTypeID <= 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //check that there is a export template exists
            if (!Company.AssetTypeExportTemplates.Any(x => x.ID == model.ID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid existing export template."));

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("update AssetTypeExportTemplate set Name = @name,Description = @desc, ExportViewType = @exp, IncludeUrl = @url, IncludeParent = @parent,IncludeFields = @incl, UsageNotes =@notes,AssetTypeID=@ty  where id = @id", new { ty = model.AssetTypeID, url = model.IncludeUrl, parent = model.IncludeParent, name = model.Name, id = id, desc = model.Description, exp = model.ExportViewType, incl = model.IncludeFields, notes = model.UsageNotes });

            if (res > 0) return model; // updated

            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Export Template not found to update."));
        }
    }
}
