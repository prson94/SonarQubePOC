using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using Dapper;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
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
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<ArtifactTypeExportTemplate>))
        ]
        public async Task<IEnumerable<dynamic>> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return (await Company.QueryAsync<dynamic>("select ID, ArtifactTypeID, Name, Description,IncludeFields,ExportViewType,IncludeUrl,IncludeParent,UsageNotes,CASE WHEN templatefile IS NULL THEN 0 ELSE 1 END as HasTemplateFile from ArtifactTypeExportTemplate order by Name, ID"));            
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
        ]
        public async Task<HttpResponseMessage> DeleteById(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var res = await Company.Database.Connection.ExecuteAsync("delete ArtifactTypeExportTemplate where id = @id", new { id = id });

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
            Route("")
        ]
        public async Task<ArtifactTypeExportTemplate> Post(ArtifactTypeExportTemplate model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            //validate the model input
            if (model.ID > 0 || string.IsNullOrEmpty(model.Name) || model.ArtifactTypeID <= 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //validate the artifacttype id is a valid artifact typeid
            if(!Company.ArtifactTypes.Any(x=>x.ID == model.ArtifactTypeID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid existing Artifact Type."));

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("insert into ArtifactTypeExportTemplate (Name, Description, ArtifactTypeID, IncludeFields,ExportViewType,IncludeUrl,IncludeParent,UpdatedBy,UpdatedOn,UsageNotes) values(@n,@d,@atID,@i,@e,@url,@parent,@upd,@updOn,@notes)", new { atID = model.ArtifactTypeID, n = model.Name, d = model.Description, i = model.IncludeFields, e = model.ExportViewType, url = model.IncludeUrl, parent = model.IncludeParent, upd = Company.CurrentResourceID, updOn = DateTime.UtcNow, notes = model.UsageNotes });

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Item already exists"));
            }

            return model;
        }

        /// <summary>
        /// Uploads a new Export Template template file for the specified export template.  
        /// </summary>
        /// <returns>Http 200 if upload was successful.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("TemplateFile/{templateId}")
        ]
        public async Task<HttpResponseMessage> PostTemplateFile(int templateId)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if(!Company.ArtifactTypeExportTemplates.Any(x=>x.ID == templateId))
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

            var res = await Company.Database.Connection.ExecuteAsync("update ArtifactTypeExportTemplate  set TemplateFile = @t where ID = @id", new { @t = template, @id=templateId  });

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
        ]
        public async Task<ArtifactTypeExportTemplate> Put(int id, ArtifactTypeExportTemplate model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //validate the model input
            if (model.ID <= 0 || string.IsNullOrEmpty(model.Name) || model.ArtifactTypeID <= 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //check that there is a export template exists
            if(!Company.ArtifactTypeExportTemplates.Any(x=>x.ID==model.ID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid existing export template."));

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("update ArtifactTypeExportTemplate set Name = @name,Description = @desc, ExportViewType = @exp, IncludeUrl = @url, IncludeParent = @parent,IncludeFields = @incl, UsageNotes =@notes  where id = @id", new { url = model.IncludeUrl, parent= model.IncludeParent,  name = model.Name, id = id, desc = model.Description, exp = model.ExportViewType, incl = model.IncludeFields, notes = model.UsageNotes });

            if (res > 0) return model; // updated

            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Export Template not found to update."));
        }
    }
}
