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

        public TagsController(ICommunityContext community, ICompanyContext company, ITagRepository repository,IAssetRepository assetRep)
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
            ApiExplorerSettings(IgnoreApi = true)
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
        /// Retrieves a list of all tags defined in Govern.
        /// </summary>                
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("uid", "The Uid of a specific tag to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(List<TagApiModelWrapper>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                var tags = await tagRepository.GetTags(queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, tags));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while fetching tags", ex.Message);

            }
        }

        /// <summary>
        /// Deletes a tag from Govern.
        /// </summary>
        /// <param name="tagUid">The uid of the tag to be removed.</param>
        /// <param name="cascade">Cascade, if true a tag that is applied to an asset will be deleted along with the association.  If false a tag that is in use will not be deleted.  The default is false for the cascade setting.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route("{tagUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteById(Guid tagUid, bool cascade = false)
        {
            if (!tagRepository.IsAuthorizedToEditTag(tagUid))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

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
        /// Adds a tag to Govern with the properties provided in the model.
        /// </summary>        
        /// <param name="model">The tag to be created.</param>
        /// <returns>The created tag.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was saved, returns the properties of the created tag.", typeof(TagApiModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostTag(TagApiModel model)
        {

            if (model == null)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You have submitted an invalid or empty request please check your request and try again."));

            TagApiModel result = new TagApiModel();
            try
            {
                TagValidator.ValidateForPost(model);

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult Put(Guid tagUid, TagApiModel model)
        {
            if (!tagRepository.IsAuthorizedToEditTag(tagUid))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            TagApiModel result = new TagApiModel();
            try
            {

                TagValidator.ValidateForPut(tagUid, model);

                var existingTag = tagRepository.GetTagByUid(tagUid);

                if (existingTag == null)
                {
                    throw new Exception("Invalid uid no tag exists with the specified uid.");
                }

                if (tagRepository.DoesTagExists(model))
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
        /// Allows you to remove a tags based on tag lists.
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
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteTags(List<TagApiDeleteModel> model)
        {

            foreach (var item in model)
            {
                if (!tagRepository.IsAuthorizedToEditTag(item.uid))
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
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
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult ConsolidateTags(string parentUid, List<string> childrenUids)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {

                if (Guid.Parse(parentUid) == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", $"{parentUid} is not valid uid!");

                foreach (var item in childrenUids)
                {
                    if (Guid.Parse(item) == Guid.Empty)
                        return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", $"{item} is not valid uid!");
                }

                if (childrenUids.Contains(parentUid))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error while consolidating tags", "Parent tag should not be included in children tags!");

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
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
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagDetailApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
            ]
        public IHttpActionResult GetTagDetails(string uid)
        {

            try
            {

                Guid tagUid = Guid.Parse(uid);

                //make sure no tag with the same name exists
                if (tagRepository.DoesTagExists(uid))
                {
                    throw new Exception("Invalid tag specified [same tag already exists].");
                }

                var queryParams = Request.GetQueryNameValuePairs();

                TagDetailApiModel results = tagRepository.GetDetails(tagUid, queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while creating tag", e.Message);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting assets path", e.Message);
            }

        }

        /// <param name="tag">The tag to be created.</param>
        [HttpPost, 
        Route("exists"),
        ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult DoesTagExist(TagApiModel tag)
        {
            try
            {
                var result = tagRepository.GetTagByName(tag.Name);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while checking if tag exists", e.Message);
            }

        }
        [HttpGet,
        Route("getAssetTagDetails"),
        ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult getAssetTagDetails(int tagID, Guid assetUID)
        {
            try
            {
                var asset = assetRepository.GetAssetByUID(assetUID);
                var result = tagRepository.GetAssetTagDetails(tagID,asset.ID);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting asset tag details", e.Message);
            }

        }


    }
}

