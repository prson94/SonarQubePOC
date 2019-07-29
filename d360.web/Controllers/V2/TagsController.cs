using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/tags"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = false)
    ]
    public class TagsController : BaseV2ApiController
    {
        ITagRepository tagRepository;

        public TagsController(ICommunityContext community, ICompanyContext company, ITagRepository repository)
            : base(community, company)
        {
            this.tagRepository = repository;
        }


        /// <summary>
        /// Returns all tags that are defined in Govern.          
        /// </summary>        
        /// <returns>A list of tags</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("search/{name}"),
        ]
        public async Task<IHttpActionResult> Search(string name)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            try
            {
                var tags = tagRepository.SearchTags(name);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, tags));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while fetching tags", ex.Message);

            }
        }

        /// <summary>
        /// Returns all tags that are defined in Govern.          
        /// </summary>        
        /// <returns>A list of tags</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("uid", "The uid of a specific tag to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(List<TagApiModelWrapper>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
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
        /// Allows you to remove a tag based on its Uid.
        /// </summary>
        /// <param name="uid">The public identifier for the tag.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteById(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            try
            {

                if (!tagRepository.DeleteTag(uid))
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
        /// Adds one tag to Govern.
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
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to create tags."));

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
        /// Updates the specified tag.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>        
        /// <param name="model">The tag to be updated.</param>
        /// <returns>The updated tag.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult Put(Guid uid, TagApiModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update tags."));

            TagApiModel result = new TagApiModel();
            try
            {
                TagValidator.ValidateForPut(uid, model);

                var existingTag = tagRepository.GetTagByUid(uid);

                if (existingTag == null)
                {
                    throw new Exception("Invalid uid no tag exists with the specified uid.");
                }

                if (tagRepository.DoesTagExists(model))
                {
                    throw new Exception("Invalid tag specified [same tag already exists].");
                }

                result = tagRepository.UpdateTag(uid, model, existingTag);
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while updating tag", e.Message);
            }

            return ResponseMessage(Request.CreateResponse<TagApiModel>(HttpStatusCode.OK, result));
        }


        /// <summary>
        /// Allows you to remove a tag based on tag lists.
        /// </summary>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
        ]
        public IHttpActionResult BulkTagDelete(List<TagApiDeleteModel> model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
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
        /// <returns>A status for the POST request.</returns>
        [
            HttpPost,
            Route("consolidate/{parentUid}"),
            /// <param name="parentUid">The unique identifier of the parent tag.</param>        
            /// <param name="childrenUids">The list of children tags which we want to consolidate.</param>
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(TagApiModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
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



        [HttpGet, MapToApiVersion("2.0"), Route("{tagUid}/assetpath")]
        public IHttpActionResult GetAssetsPath(Guid tagUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                List<dynamic> result = tagRepository.GetAssetsPathForTag(tagUid);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting assets path", e.Message);
            }

        }

        [HttpPut, MapToApiVersion("2.0"), Route("settaggingstatus")]
        public IHttpActionResult SetTaggingStatus(TagStatusModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var result = tagRepository.SetTaggingStatus(model.IsTaggingEnabled);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

            }
            catch (Exception e)
            {

                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while getting assets path", e.Message);
            }

        }

    }
}
