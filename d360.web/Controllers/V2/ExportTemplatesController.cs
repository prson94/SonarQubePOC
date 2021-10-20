using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Resources;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/exporttemplates"), Authorize
    ]
    public class ExportTemplatesController : BaseV2ApiController
    {
        #region DI
        private IAssetRepository assetRepository;
        public ExportTemplatesController(CoreComponentSet set, IAssetRepository assetRepository): base(set)
        {
            this.assetRepository = assetRepository;
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
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            List<AssetTypeExportTemplate> templateList = (await assetRepository.GetExportTemplates());

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, templateList));
        }

        /// <summary>
        /// Returns all asset export templates for an asset type
        /// </summary>
        /// <returns>An array of asset export template records</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{assetTypeUID}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<AssetTypeExportTemplate>)),
            SwaggerResponse(HttpStatusCode.NotFound, "", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> Get(Guid assetTypeUID)
        {           
            List<AssetTypeExportTemplate> templateList = await assetRepository.GetExportTemplates(assetTypeUid: assetTypeUID);

            if (templateList.Count == 0)
            {
                return errorMessageResponse(HttpStatusCode.NotFound,ApiMessages.TemplateNotFound,  ApiMessages.TemplateNotFoundMessage);
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, templateList));
        }

        /// <summary>
        /// Returns export template for an given template Uid
        /// </summary>
        /// <returns>A export template record</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{templateUid}/details"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetTypeExportTemplate)),
            SwaggerResponse(HttpStatusCode.NotFound, "", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetTemplateByUid(Guid templateUid)
        {
            List<AssetTypeExportTemplate> templateList = await assetRepository.GetExportTemplates(exportTemplateUID: templateUid);

            if (templateList.Count == 0)
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound, ApiMessages.TemplateNotFoundMessage);
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, templateList.FirstOrDefault()));
        }

        /// <summary>
        /// Returns the id of an export template for an given template Uid
        /// </summary>
        /// <returns>A export template record</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{templateUid}/id"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetTypeExportTemplate)),
            SwaggerResponse(HttpStatusCode.NotFound, "", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetTemplateId(Guid templateUid)
        {
            List<AssetTypeExportTemplate> templateList = await assetRepository.GetExportTemplates(exportTemplateUID: templateUid);

            if (templateList.Count == 0)
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound, ApiMessages.TemplateNotFoundMessage);
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, templateList.FirstOrDefault().ID));
        }


        /// <summary>
        /// Deletes a Export Template based on the specified ID
        /// </summary>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{templateUid}"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Template succesfully deleted.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Template not found matching Uid Provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteTemplateByUid(Guid templateUid)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            var res = await Company.Database.Connection.ExecuteAsync("delete AssetTypeExportTemplate where uid = @uid", new { uid = templateUid });

            if (res > 0)
            {
                return successMessageResponse(HttpStatusCode.OK,ApiMessages.TemplateDeleted, ApiMessages.TemplateDeletedMessage); // deleted
            }else if (res == 0){
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound, ApiMessages.TemplateNotFoundMessage);
            }else
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.TemplateDeletedError, string.Format(ApiMessages.TemplateDeletedError, templateUid.ToString()));
            }
        }

        /// <summary>
        /// Creates a new Export Template record.  
        /// </summary>
        /// <returns>ExportTemplate model of the created item if item already exists http confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AssetTypeExportTemplateUpsertRequest), typeof(ExportTemplateUpsertExample)),
            SwaggerResponse(HttpStatusCode.OK, "Template Created Successfully", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Item already exists", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))            
        ]
        public async Task<IHttpActionResult> AddTemplate(AssetTypeExportTemplateUpsertRequest model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            //Validate and map asset type uid to to id
            if (string.IsNullOrEmpty(model.AssetTypeUID.ToString()) || model.AssetTypeUID == Guid.Empty)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidRequest, ActionApiMessages.InvalidAssetTypeUid);
            }

            AssetType assetType = Company.AssetTypes.FirstOrDefault(t => t.uid == model.AssetTypeUID);

            AssetTypeExportTemplate template = new AssetTypeExportTemplate { Name = model.Name, Description = model.Description, UsageNotes = model.UsageNotes, IncludeFieldTypes = model.IncludeFieldTypes, IncludeUrl = model.IncludeUrl, IncludeParent = model.IncludeParent, ExportViewType = model.ExportViewType, AssetTypeUID = model.AssetTypeUID };

            template.AssetTypeID = assetType?.ID ?? 0;

            var validationStatus = ValidateTemplate(template, assetType);
            if (validationStatus.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message)).ConfigureAwait(false);
            }

            var createExportTemplateSQL = $@"insert into AssetTypeExportTemplate 
                                                (Name, Description, AssetTypeID, ExportViewType, IncludeUrl, IncludeParent, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, UsageNotes) 
                                                OUTPUT INSERTED.Id
                                            values
                                                (@n,@d,@atID,@e,@url,@parent,@upd,@updOn,@upd,@updOn,@notes)";
          
            var templateId = await Company.Database.Connection.ExecuteScalarAsync<int>(createExportTemplateSQL, new { atID = template.AssetTypeID, n = template.Name, d = template.Description, e = template.ExportViewType, url = template.IncludeUrl, parent = template.IncludeParent, upd = Company.CurrentResourceID, updOn = DateTime.UtcNow, notes = template.UsageNotes });

            var res = templateId;
            if (templateId > 0 && model.IncludeFieldTypes != null && model.IncludeFieldTypes.Length > 0)
            {
                res = SetIncludedFieldTypes(templateId, model.IncludeFieldTypes, true);               
            }
            
            if (res <= 0)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.TemplateCreatingError,  INTERNAL_ERROR_MESSAGE);
            }
            
            var templateUid = Company.AssetTypeExportTemplates.FirstOrDefault(x => x.ID == templateId).Uid;

            var response = new AssetTypeSuccess { Uid = templateUid, Message = string.Format(ApiMessages.SuccessfullyAdded, model.Name), Success = true };

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IEnumerable<AssetTypeExportTemplateStyle> GetStyles(int templateId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }
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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<AssetTypeExportTemplateStyle> SaveTemplateStyle(AssetTypeExportTemplateStyle model)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden,  ApiMessages.AccessDenied));
            }
            if (!Company.AssetTypeExportTemplates.Any(x => x.ID == model.AssetTypeExportTemplateID))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound));
            }

            if (!string.IsNullOrEmpty(model.BgColor))
            {
                model.BackgroundColor = ColorTranslator.FromHtml(model.BgColor).ToArgb();
            }

            if (!string.IsNullOrEmpty(model.TextColor))
            {
                model.Color = ColorTranslator.FromHtml(model.TextColor).ToArgb();
            }

            Company.Add(model);
            await Company.SaveChangesAsync();

            return model;
        }

        /// <summary>
        /// Update  style for a template
        /// </summary>
        /// <param name="id">style id</param>
        /// <param name="model">AssetTypeExportTemplateStyle</param>
        /// <returns></returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("Style/{id}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<AssetTypeExportTemplateStyle> UpdateTemplateStyle(int id, AssetTypeExportTemplateStyle model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));

            //validate the model input
            if (model.ID <= 0 || model.AssetTypeExportTemplateID <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.ErrorInvalidDatasetMessage));
            }

            //check that there is a export template exists
            if (!Company.AssetTypeExportTemplateStyles.Any(x => x.ID == model.ID))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.ModelExportTemplateStyleNotFound));
            }
            var data = Company.AssetTypeExportTemplateStyles.FirstOrDefault(x => x.ID == model.ID);
            data.IsBold = model.IsBold;
            data.Column = model.Column;
            data.Row = model.Row;

            if (!string.IsNullOrEmpty(model.BgColor))
            {
                data.BackgroundColor = ColorTranslator.FromHtml(model.BgColor).ToArgb();
            }

            if (!string.IsNullOrEmpty(model.TextColor))
            {
                data.Color = ColorTranslator.FromHtml(model.TextColor).ToArgb();
            }

            Company.Entry(data).State = System.Data.Entity.EntityState.Modified;
            var res = await Company.SaveChangesAsync();
            if (res > 0)
            {
                return model; // updated
            }

            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFoundUpdate));

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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> DeleteStyle(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            var res = await Company.Database.Connection.ExecuteAsync("delete AssetTypeExportTemplateStyle where id = @id", new { id = id });

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // deleted
            }

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
            Route("TemplateFile/{templateUid}"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "Error while opening file.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PostTemplateFile(Guid templateUid)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;

            if (!Company.CurrentResourceIsAdmin)
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);

            if (!Company.AssetTypeExportTemplates.Any(x => x.Uid == templateUid))
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound, ApiMessages.TemplateNotFoundMessage);
            }

            byte[] template = null;
            try
            {
                if (context.Request.Files.Count > 0)
                {
                    var file = context.Request.Files[0];
                    if (file.FileName.EndsWith(".xls") || file.FileName.EndsWith(".xlsx"))
                    {
                        var target = new MemoryStream();
                        file.InputStream.CopyTo(target);
                        template = target.ToArray();
                    }
                    else
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidRequest,ApiMessages.TemplateFileTypeValidate);
                    }
                }                
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.ErrorCreateTemplate, ApiMessages.ErrorFileOpen);               
            }

            var res = await Company.Database.Connection.ExecuteAsync("update AssetTypeExportTemplate set TemplateFile = @t where uid = @uid", new { @t = template, @uid = templateUid });

            return successMessageResponse(HttpStatusCode.OK, ApiMessages.Success,ApiMessages.FileUploadMessage);
        }


        /// <summary>
        /// Updates the specified Export Template
        /// </summary>
        /// <returns>Http Status code OK and the item if updated, Http Status code of Not Found if item could not be updated</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{templateUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AssetTypeExportTemplateUpsertRequest), typeof(ExportTemplateUpsertExample)),
            SwaggerResponse(HttpStatusCode.OK, "Template Updated Successfully", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Export Template not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateTemplate(Guid templateUid, AssetTypeExportTemplateUpsertRequest model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage);
            }

            //Validate and map asset type uid to to id
            if (string.IsNullOrEmpty(model.AssetTypeUID.ToString()))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidAssetTypeUid);
            }

            AssetType assetType = Company.AssetTypes.FirstOrDefault(t => t.uid == model.AssetTypeUID);

            AssetTypeExportTemplate template = new AssetTypeExportTemplate { Name = model.Name, Description = model.Description, UsageNotes = model.UsageNotes, IncludeFieldTypes = model.IncludeFieldTypes, IncludeUrl = model.IncludeUrl, IncludeParent = model.IncludeParent, ExportViewType = model.ExportViewType, AssetTypeUID = model.AssetTypeUID, Uid = templateUid };

            template.AssetTypeID = assetType?.ID ?? 0;

            //check that there is a export template exists
            var currentTemplate = Company.AssetTypeExportTemplates.FirstOrDefault(x => x.Uid == templateUid);
            if (currentTemplate == null)
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.TemplateNotFound, ApiMessages.TemplateNotFoundMessage);
            }
            else
            {
                template.ID = currentTemplate.ID;
                template.Uid = templateUid;
            }

            var validationStatus = ValidateTemplate(template, assetType);
            if (validationStatus.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message));
            }

            var updateTemplateSQL = $@"update AssetTypeExportTemplate 
                                        set Name = @name,Description = @desc, 
                                            ExportViewType = @exp, 
                                            IncludeUrl = @url, 
                                            IncludeParent = @parent, 
                                            UsageNotes =@notes,
                                            AssetTypeID=@ty, 
                                            UpdatedOn=@updatedOn,
                                            UpdatedBy=@updatedBy   
                                        where 
                                            id = @id";

            var res = await Company.Database.Connection.ExecuteAsync(updateTemplateSQL, new { ty = template.AssetTypeID, url = model.IncludeUrl, parent = model.IncludeParent, name = model.Name, id = template.ID, desc = model.Description, exp = model.ExportViewType, notes = model.UsageNotes, updatedBy = Company.CurrentResourceID, updatedOn = DateTime.UtcNow });

            if (res > 0 && model.IncludeFieldTypes != null)
            {
                res = SetIncludedFieldTypes(template.ID, model.IncludeFieldTypes);
            }

            if (res >= 0)
            {
                var result = new AssetTypeSuccess { Uid = template.Uid, Message = string.Format(ApiMessages.SucessfullyUpdated, model.Name), Success = true };
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            else
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.ErrorUpdateTemplate, INTERNAL_ERROR_MESSAGE);                
            }            
        }
        
        /// <summary>
        /// Check if the asset has custom exports.
        /// </summary>
        /// <param name="uid">The uid of the Rule Type to check for custom export.</param>
        /// <returns>An excel sheet of the rules of the given rule type uid.</returns>
        [
            HttpGet,
            Route("hasCustomExport/{uid}"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Check if the asset has custom export templates.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult HasCustomExport(Guid uid)
        {
            var assettype = Company.AssetTypes.FirstOrDefault(x => x.uid == uid);
            var res = Company.AssetTypeExportTemplates.Any(x => x.AssetTypeID == assettype.ID);
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res));
        }

        private async Task<IEnumerable<dynamic>> GetRuleTypeFieldResults(Guid guid, List<FieldType> fieldTypes, AssetTypeExportTemplate template = null)
        {
            DynamicParameters dbArgs = new DynamicParameters();
            string selectSql = @"
                        SELECT 
		                        A.ID as 'ID',
		                        A.uid as 'AssetUid',
		                        AT.uid as 'AssetTypeUid',
		                        A.UpdatedOn,
		                        A.CreatedOn,
                                'asset/' +  + CAST(A.uid as varchar(36)) as 'Url'
	                         ";

            string joinsSql = "  ";

            string whereSQL = "WHERE AT.uid = @uid";
            dbArgs.Add("uid", guid);

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();



            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

            fieldTypes.Add(new FieldType { Type = "Number", Name = "AssetUid", FriendlyName = "Rule UID" });
            fieldTypes.Add(new FieldType { Type = "Number", Name = "ID", FriendlyName = "Rule ID" });
            if (template == null || (template != null && template.IncludeUrl))
            {
                fieldTypes.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });
            }

            foreach (var col in fieldColumns)
            {
                selectSql += "," + col;
            }

            foreach (var join in fieldJoins)
            {
                joinsSql += join;
            }
            var sql = $@"
                            {selectSql}
                            FROM dbo.[Rule] R
                                    LEFT JOIN dbo.RuleType RT on R.RuleTypeID = RT.ID
                                    INNER JOIN [dbo].Asset A on A.Object = 'Rule' and A.ObjectID = R.ID
                                    INNER JOIN [dbo].AssetType AT on AT.ID = a.AssetTypeID
                            {joinsSql} 
                            {whereSQL}";

            var results = await Company.QueryAsync<dynamic>(sql, dbArgs);
            return results;
        }

        private List<FieldType> GetRuleTypeFields(Guid guid)
        {
            var assetType = assetRepository.GetAssetTypeByUID(guid);
            var typesToAvoid = new List<string>() {
                    DataType.ComplexRelationLookup.ToString(),
                    DataType.DataTableSelect.ToString(),
                    DataType.OwnershipLookup.ToString()
                };

            var fieldTypes = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID)
                                .Where(x => !typesToAvoid.Contains(x.Type))
                                .OrderBy(x => x.ColumnOrder)
                                .ThenBy(i => i.FriendlyName).ToList();


            return fieldTypes;
        }
        private async Task<SLDocument> GetDefaultRuleDocument(Guid guid, AssetTypeExportTemplate template = null)
        {
            List<FieldType> fieldTypes = GetRuleTypeFields(guid);
            if (template != null)
            {
                UseTempleteFields(template, fieldTypes);
            }
            IEnumerable<dynamic> results = await GetRuleTypeFieldResults(guid, fieldTypes, template);

            SLDocument document = new SLDocument();
            document = GenerateDefaultSpreadsheet(fieldTypes, results, template, "Items");
            return document;
        }

        private async Task<SLDocument> GetPivotRuleDocument(Guid guid, AssetTypeExportTemplate template = null)
        {
            List<FieldType> fieldTypes = GetRuleTypeFields(guid);
            if (template != null)
            {
                UseTempleteFields(template, fieldTypes);
            }
            IEnumerable<dynamic> results = await GetRuleTypeFieldResults(guid, fieldTypes, template);

            SLDocument document = new SLDocument();
            document = GeneratePivotedSpreadsheet(fieldTypes, results, template, "Items");
            return document;
        }

        private async Task<SLDocument> GetGroupedRuleDocument(Guid guid, AssetTypeExportTemplate template = null)
        {
            List<FieldType> fieldTypes = GetRuleTypeFields(guid);
            if (template != null)
                UseTempleteFields(template, fieldTypes);
            IEnumerable<dynamic> results = await GetRuleTypeFieldResults(guid, fieldTypes, template);

            SLDocument document = new SLDocument();
            document = GenerateGroupedSpreadsheet(fieldTypes, results, template, "Items");
            return document;
        }

        private int SetIncludedFieldTypes(int templateId, string[] fieldTypes, bool isInsert = false)
        {
            var deleteIncludeFieldTypeList = $@"
                                            delete from AssetTypeExportTemplateField where TemplateID = @templateId";

            var createIncludeFieldTypeList = $@"                                          
                                            insert into AssetTypeExportTemplateField 
                                                (TemplateID, FieldTypeId, [Order])
                                            select 
                                                ATET.ID, FT.ID, @order
                                            from 
                                                AssetTypeExportTemplate ATET
                                                INNER JOIN
                                                FieldType FT on ATET.AssetTypeID = FT.AssetTypeID and ATET.ID = @templateId and FT.Name = @name";

            var parameters = new List<DynamicParameters>();

            for (int i = 0; i < fieldTypes.Length; i++)
            {
                var p = new DynamicParameters();
                p.Add("@templateId", templateId);
                p.Add("@order", i + 1);
                p.Add("@name", fieldTypes[i]);

                parameters.Add(p);
            }

            int result = 0;

            if (!isInsert)
            {
                result = Company.Database.Connection.ExecuteAsync(deleteIncludeFieldTypeList, new { templateId }).Result;
            }

            return result < 0 ? result : Company.Database.Connection.ExecuteAsync(createIncludeFieldTypeList, parameters).Result;
        }

        private WorkHttpStatus ValidateTemplate(AssetTypeExportTemplate template, AssetType assetType)
        {
            if(assetType ==null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest,  ApiMessages.InvalidRequest,ActionApiMessages.InvalidAssetTypeUid);
            }

            if (assetType.Class != AssetTypeClass.BusinessAsset
               && assetType.Class != AssetTypeClass.TechnicalAsset
               && assetType.Class != AssetTypeClass.Rule)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest,ApiMessages.AssetTypeNotSupportExport);
            }
            //validate the model input
            if (string.IsNullOrEmpty(template.Name))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.NameNotEmptyAndRequired);
            }
            else if (template.Name.Length > 250)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.NameMaxLength250Char);
            }

            if (Company.AssetTypeExportTemplates.Any(t => t.AssetTypeID == template.AssetTypeID && t.Name == template.Name && ((template.Uid == null || template.Uid == Guid.Empty) || (template.Uid != null && t.Uid!=template.Uid))))
            {
                return new WorkHttpStatus(HttpStatusCode.Conflict, ApiMessages.Conflict, string.Format(ApiMessages.TemplateNameDuplicate, template.Name, assetType.Name));
            }

            if (template.AssetTypeID <= 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest,ApiMessages.InvalidAssetTypeID);
            }            

            if(!Enum.IsDefined(typeof(ExportView), template.ExportViewType))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ExportViewMessage);
            }

            if (template.IncludeFieldTypes != null && template.IncludeFieldTypes.Length > 0)
            {
                foreach (string fieldName in template.IncludeFieldTypes)
                {
                    if (!Company.FieldTypes.Any(x => x.AssetTypeID == template.AssetTypeID && x.Name == fieldName))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.FieldValidateWithAssetType, fieldName));
                    }
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }
    }
}
