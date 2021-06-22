using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.Entity;
using d360.core.enums;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/tags"),
        Authorize
    ]
    public class TagsController : BaseV2ApiController
    {
        ITagRepository tagRepository;
        IAssetRepository assetRepository;

        public TagsController(ICommunityContext community, ICompanyContext company, ITagRepository repository, IAssetRepository assetRep)
            : base(community, company)
        {
            this.tagRepository = repository;
            this.assetRepository = assetRep;
        }


        /// <summary>
        /// Returns all tags that are defined in Govern that match the search criteria.          
        /// </summary>        
        /// <returns>A list of tags</returns>
        [
            HttpGet, MapToApiVersion("2.0"),
            Route("search"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("Value", "The value of the tag that's to be searched.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Search for tags completed.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while fetching tags.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult Search()
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var tags = tagRepository.SearchTags(queryParams);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, tags));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while fetching tags", ex.Message);

            }
        }

        /// <summary>
        /// Retrieves a list of all tags.
        /// </summary>                
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("uid", "The Uid of a specific tag to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_tag", "Search term that filters on the name of the tag.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by tag name.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(List<TagApiModelWrapper>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid)).ConfigureAwait(false);
                }

                var tags = await tagRepository.GetTags(queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, tags));
            }
            catch(ArgumentException e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", e.Message);
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while fetching tags", ex.Message);

            }
        }

        /// <summary>
        /// Deletes a tag based on the provided Uid.
        /// </summary>
        /// <param name="tagUid">The uid of the tag to be removed.</param>
        /// <param name="cascade">Cascade, if true a tag that is applied to an asset will be deleted along with the association.  If false a tag that is in use will not be deleted.  The default is false for the cascade setting.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route("{tagUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag provided is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteById(Guid tagUid, bool cascade = false)
        {
            if (!tagRepository.DoesTagExists(tagUid))
            {
                return errorMessageResponse(HttpStatusCode.NotFound, "Error removing tag", $"Tag with uid {tagUid} not found.");
            }

            if (!tagRepository.IsAuthorizedToEditTag(tagUid))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access Denied"));
            }

            try
            {
                if (!tagRepository.DeleteTags(new List<TagApiDeleteModel>() { new TagApiDeleteModel { uid = tagUid, cascade = cascade } }))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error removing tag", "Tag not found.");
                }
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while deleting tag", ex.Message);

            }

            return successMessageResponse(HttpStatusCode.OK, "Tag removed.", "Tag successfully removed.");
        }


        /// <summary>
        /// Adds a tag with the properties provided in the model.
        /// </summary>        
        /// <param name="model">The tag to be created.</param>
        /// <returns>The created tag.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was saved, returns the properties of the created tag.", typeof(TagApiModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostTag(TagApiUpsertModel model)
        {
            if (model == null)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You have submitted an invalid or empty request please check your request and try again."));
            }

            TagApiModel result = new TagApiModel();
            try
            {
                TagValidator.ValidateForPost(model);

                model.Value = model.Value.Trim();

                //make sure no tag with the same name exists
                if (tagRepository.DoesTagExists(model.Value))
                {
                    throw new Exception("Invalid tag specified [same tag already exists].");
                }


                result = tagRepository.CreateTag(model);
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while creating tag", e.Message);
            }

            return ResponseMessage(Request.CreateResponse<TagApiModel>(HttpStatusCode.OK, result));
        }



        /// <summary>
        /// Updates the specified tag with the values provided in the model.
        /// </summary>
        /// <param name="tagUid">The Uid of the tag to be updated.</param>        
        /// <param name="model">The new definition of the tag to be used for the update.</param>
        /// <returns>A tag model representing the updated tag.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{tagUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult Put(Guid tagUid, TagApiUpsertModel model)
        {
            if (!tagRepository.DoesTagExists(tagUid))
                return errorMessageResponse(HttpStatusCode.NotFound, "Error updating tag", $"Tag with uid {tagUid} not found.");

            if (!tagRepository.IsAuthorizedToEditTag(tagUid))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            TagApiModel result = new TagApiModel();
            try
            {
                model.Value = model.Value.Trim();
                TagValidator.ValidateForPut(tagUid, model);

                var existingTag = tagRepository.GetTagByUid(tagUid);

                if (existingTag == null)
                {
                    throw new Exception("Invalid uid no tag exists with the specified uid.");
                }

                if (tagRepository.DoesTagExists(tagUid, model))
                {
                    throw new Exception("Invalid tag specified [same tag already exists].");
                }

                result = tagRepository.UpdateTag(tagUid, model, existingTag);
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while updating tag", e.Message);
            }

            return ResponseMessage(Request.CreateResponse<TagApiModel>(HttpStatusCode.OK, result));
        }


        /// <summary>
        /// Allows you to remove tags based on a tag list.
        /// </summary>
        /// <remarks>
        /// Use the cascade flag set to true to delete a tag that is applied to an asset that tag will be deleted along with the association.  If false a tag that is in use will not be deleted.  The default is false for the cascade setting.
        /// </remarks>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate an invalid model was provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteTags(List<TagApiDeleteModel> model)
        {
            if (model == null)
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error removing tags", $"Invalid request.");

            foreach (var item in model)
            {
                if (!tagRepository.DoesTagExists(item.uid))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error removing tag", $"Tag with uid {item.uid} not found.");
                }

                if (!tagRepository.IsAuthorizedToEditTag(item.uid))
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access Denied"));
                }
            }


            try
            {

                if (!tagRepository.DeleteTags(model))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error removing tags", "Tag not found.");
                }
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error while deleting tags", ex.Message);

            }


            return successMessageResponse(HttpStatusCode.OK, "Tags removed.", "Tags successfully removed.");
        }


        /// <summary>
        /// Consolidates tags
        /// </summary>
        /// <param name="parentUid">The unique identifier of the parent tag.</param>        
        /// <param name="childrenUids">The list of children tags which we want to consolidate.</param>
        /// <returns>A status for the POST request.</returns>
        [
            HttpPost,
            Route("consolidate/{parentUid}"),

            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<TagApiModel>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult ConsolidateTags(string parentUid, List<string> childrenUids)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            }

            try
            {

                if (Guid.Parse(parentUid) == Guid.Empty)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", $"{parentUid} is not valid uid!");
                }

                foreach (var item in childrenUids)
                {
                    if (Guid.Parse(item) == Guid.Empty)
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", $"{item} is not valid uid!");
                    }
                }

                if (childrenUids.Contains(parentUid))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", "Parent tag should not be included in children tags!");
                }

                IEnumerable<TagApiModel> result = tagRepository.ConsolidateTags(parentUid, childrenUids);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error while consolidating tags", ex.Message);

            }
        }


        [HttpGet, MapToApiVersion("2.0"), Route("{tagUid}/assetpath"), ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetAssetsPath(Guid tagUid)
        {
            try
            {

                List<AssetTagList> result = tagRepository.GetAssetsPathForTag(tagUid);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting assets path", e.Message);
            }

        }

        /// <summary>
        /// GET a list of tags.
        /// </summary>
        /// <returns>A excel file containing tags.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            ApiExplorerSettings(IgnoreApi = true),
            Route("export"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported tags to Excel.", typeof(List<TagApiModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ExportToExcel()
        {

            var queryParams = Request.GetQueryNameValuePairs();

            var tags = await tagRepository.GetTagsForExcel(queryParams);

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Uid");
            document.SetCellValue(1, index++, "Name");
            document.SetCellValue(1, index++, "Use Count");
            document.SetCellValue(1, index++, "Created On");
            document.SetCellValue(1, index++, "Created By");
            document.SetCellValue(1, index++, "Updated On");
            document.SetCellValue(1, index++, "Updated By");

            #endregion

            int rowNumber = 1;
            foreach (var row in tags)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.uid.ToString());
                document.SetCellValue(rowNumber, index++, row.Value.ToString());
                document.SetCellValue(rowNumber, index++, row.UseCount.ToString());
                document.SetCellValue(rowNumber, index++, row.CreatedOn.ToString());
                document.SetCellValue(rowNumber, index++, row.CreatedBy.ToString());
                document.SetCellValue(rowNumber, index++, row.UpdatedOn.ToString());
                document.SetCellValue(rowNumber, index++, row.UpdatedBy.ToString());
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.GetBuffer())
            };
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = string.Format("Tags {0}.xlsx", System.DateTime.Now.ToShortDateString())
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }


        /// <summary>
        /// Gets tag details.
        /// </summary>
        /// <param name="uid">The unique identifier of the tag.</param>        
        /// <returns>Tag details.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{uid}/details"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagDetailApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request is badly formatted or has failed validation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("sortorder", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("sortby", "The name of the field to order results [Allowed fields are displayvalue, assettype, tagsasstring, assetid]. By default the results are ordered by DisplayValue asc", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is false meaning the total count is not included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("DisplayValue", "Filter by Display Value.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetType", "Filter by Asset Type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("TagsasString", "Filter by Tags as string.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetTypeUid", "Filter by Asset Type Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("globalSearch", "Filter by DisplayValue or AssetType or TagsasString. When global search parameter use then filter specific parameter defined for DisplayValue, AssetType, TagsasString not consider", DataType = "string", ParameterType = "query", Required = false),
        ]
        public IHttpActionResult GetTagDetails(string uid)
        {

            try
            {

                Guid tagUid = Guid.Parse(uid);

                Guid AssetTypeUid = new Guid();

                if (!tagRepository.DoesTagExists(tagUid))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, "Invalid request", $"Tag with uid {tagUid} not found.");
                }

                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid);
                }

                if (queryParams.Any(q => q.Key.ToLower() == "assettypeuid"))
                {
                    if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value.ToLower(), out AssetTypeUid))
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid asset type uid.");
                    }
                    if (AssetTypeUid != null && AssetTypeUid != Guid.Empty)
                    {
                        var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == AssetTypeUid);
                        if (assetType == null)
                        {
                            return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid asset type uid.");
                        }
                    }
                    else
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid asset type uid.");
                    }
                }

                TagDetailApiModel results = tagRepository.GetDetails(tagUid, queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting tag details", e.Message);
            }


        }

        /// <summary>
        /// GET a list of tagged assets by tag Uid.
        /// </summary>
        /// <returns>A excel file containing tagged assets.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{tagUid}/export"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported tags to Excel.", typeof(List<TagApiModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult ExportTagToExcel(string tagUid)
        {

            Guid uid = Guid.Parse(tagUid);

            var tag = Company.Tags.FirstOrDefault(x => x.uid == uid);
            var queryParams = Request.GetQueryNameValuePairs();

            var tags = tagRepository.GetDetails(uid, queryParams);

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Asset");
            document.SetCellValue(1, index++, "Asset Type");
            document.SetCellValue(1, index++, "Tags");


            #endregion

            int rowNumber = 1;
            foreach (var row in tags.items)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.DisplayValue);
                document.SetCellValue(rowNumber, index++, $"{row.AssetType.ToString()}");
                document.SetCellValue(rowNumber, index++, $"{string.Join("|", row.Tags.Select(x => x.Value))}");
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.GetBuffer())
            };
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = string.Format("{1} {0}.xlsx", System.DateTime.Now.ToShortDateString(), tag.Value)
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }

        [HttpGet, MapToApiVersion("2.0"), Route("{tagUid}/tooltip"), ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetTagTooltipData(string tagUid, Guid? assetUid = null)
        {
            try
            {
                Guid tagGuid = Guid.Parse(tagUid);

                IEnumerable<dynamic> result = tagRepository.GetTooltip(tagGuid, assetUid);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting tag tooltip", e.Message);
            }

        }

        [HttpGet, MapToApiVersion("2.0"), Route("tooltipByName"), ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetTagTooltipByNameData(string tagName, Guid? assetUid = null)
        {
            try
            {
                tagName = tagName.Replace("&amp;", "&");
                var tag = tagRepository.GetTagByName(tagName);
                return GetTagTooltipData(tag.uid.ToString(), assetUid);

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting tag tooltip", e.Message);
            }

        }

        /// <summary>
        /// A check to see if a tag already exists or not.
        /// </summary>
        /// <param name="value">The name of the tag that's been checked if exists.</param>
        [HttpGet,
        Route("exists"),
        SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
        SwaggerResponse(HttpStatusCode.OK, "Tag does exist.", typeof(HttpStatusCode)),
        SwaggerResponse(HttpStatusCode.NotFound, "Tag doesn't exist.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.BadRequest, "Error while checking if tag exists.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))]
        public IHttpActionResult CheckIfTagExist(string value)
        {
            try
            {
                var result = tagRepository.GetTagByName(value);

                if(result == null)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound));
                }
                else
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK));
                }
            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while checking if tag exists", e.Message);
            }

        }
        [HttpGet,
        Route("AssetTagDetails"),
        ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult getAssetTagDetails(int tagID, Guid assetUID)
        {
            try
            {
                var asset = assetRepository.GetAssetByUID(assetUID);
                var result = tagRepository.GetAssetTagDetails(tagID, asset.ID);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting asset tag details", e.Message);
            }

        }

        [HttpGet,
        Route("permissions/{assetUid:Guid}"),
        ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult getAssetTagPermissions(Guid assetUid)
        {
            try
            {
                var result = new List<TagPermissionItem>();

                if (assetUid == null || assetUid == Guid.Empty)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
                }

                var asset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
                if (asset == null)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
                }
                List<AssetTag> assetTags = Company.AssetTags.Where(x => x.AssetID == asset.ID).ToList();
                int[] tagIDs = assetTags.Select(x => x.TagID).ToArray();
                var tags = Company.Tags.Where(x => tagIDs.Contains(x.ID)).ToList();

                if (Company.HasAssetPermission(asset.ID, Permission.AddAsset) || Company.HasAssetPermission(asset.ID, Permission.EditAsset) || Company.CurrentResourceIsAdmin)
                {
                    foreach (var tag in tags)
                    {
                        result.Add(new TagPermissionItem()
                        {
                            CanDelete = true,
                            uid = tag.uid,
                            Value = tag.Value
                        });
                    }
                }
                else
                {
                    foreach (var tag in tags)
                    {
                        result.Add(new TagPermissionItem()
                        {
                            CanDelete = tag.CreatedBy == Company.CurrentResourceID,
                            uid = tag.uid,
                            Value = tag.Value
                        });
                    }
                }


                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting asset tag permission details", e.Message);
            }

        }

    }
}

