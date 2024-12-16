using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using repositories;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
	[ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/comments"), Authorize]
    public class CommentsController : BaseV2ApiController
    {
        #region DI

        private readonly ICommentRepository Comments;

        public CommentsController(ICoreComponentSet set, ICommentRepository comments) : base(set)
        {
            Comments = comments;
        }

        #endregion

        /// <summary>
        /// Provides support for adding a comment to an asset. You must have read permission to this asset in order to add a comment.
        /// </summary>
        /// <param name="comment">The body of the comment to add.</param>
        /// <returns>An enriched comment, containing the text of the comment as well as information about the asset you attached the comment to.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding new comment.", typeof(CommentDetail)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> AddComment(CommentApiPostModel comment)
        {
            var detail = await Comments.AddComment(comment);
            return Created("", detail);
        }

        /// <summary>
        /// Use this endpoint to register your vote for a particular comment using one of the available emoji.
        /// </summary>
        /// <param name="commentUid">The Uid of the comment to vote on.</param>
        /// <param name="emoji">The emoji to vote with.</param>
        /// <returns>An HTTP status code : Created / OK</returns>
        [
            HttpPost,
            Route("{commentUid:Guid}/votes/{emoji}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("toggle", "If true the vote will be removed if it already exists.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.Created, "The request was accepted and the vote was registered.", null),
            SwaggerResponse(HttpStatusCode.OK, "The request was accepted but the user already used this emoji on the comment.", null),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public IHttpActionResult AddVote(Guid commentUid, Emoji emoji)
        {
            var queryParams = Request.GetQueryNameValuePairs();
            bool toggle = true;

            if (queryParams.Any(qp => qp.Key.ToLower() == "toggle"))
            {
                var toggleString = queryParams.FirstOrDefault(x => x.Key.ToLower() == "toggle").Value;
                bool.TryParse(toggleString, out toggle);
            }

            var created = Comments.AddVote(commentUid, SecurityContext.ResourceID, emoji, toggle);
            if (created)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created));
            }
            else
            {
				return Ok();
            }
        }

        /// <summary>
        /// Use this endpoint to remove the comment from the asset's Board. Depending on whether this comment has replies, it will either be completed removed (if no replies) 
        /// or hidden (if there are replies). You must either have created the comment to begin with or be an administrator in this environment. 
        /// </summary>
        /// <param name="commentUid">The Uid of the comment to remove.</param>
        /// <returns>An Http Status code: OK</returns>
        [
            HttpDelete,
            Route("{commentUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Removed the comment.", null),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteComment(Guid commentUid)
        {
            if (Comments.DeleteComment(commentUid))
            {
                return Ok();
            }
            else
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, Error.UnknownError, Error.CommentRetryRemove);
            }
        }

        /// <summary>
        /// Use this endpoint to unregister your vote for a particular comment.
        /// </summary>
        /// <param name="commentUid">The Uid of the comment.</param>
        /// <param name="emoji">The Emoji-based vote to unregister.</param>
        /// <returns>An Http Status code: OK</returns>
        [
            HttpDelete,
            Route("{commentUid:Guid}/votes/{emoji}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Unregister your vote for a comment.", null),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public IHttpActionResult DeleteVote(Guid commentUid, Emoji emoji)
        {
            Comments.DeleteVote(commentUid, SecurityContext.ResourceID, emoji);
            return Ok();
        }

        /// <summary>
        /// Provides support for updating a comment on an asset. You must have created the comment to begin with in order to successfully call this endpoint.
        /// </summary>
        /// <param name="commentUid">The Uid of the comment.</param>
        /// <param name="comment">The comment body to use when updating.</param>
        /// <returns>An enriched comment, containing the text of the comment as well as information about the asset.</returns>
        [
            HttpPut,
            Route("{commentUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Editing a comment.", typeof(object)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> EditComment(Guid commentUid, CommentApiPutModel comment)
        {
            var detail = await Comments.EditComment(commentUid, comment);
            return Ok(detail);
        }

        /// <summary>
        /// Returns an array of comments along with the total number and the current page number and size.
        /// </summary>
        /// <remarks>
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
		///     
		///     Example :
		///     
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
        /// 
        /// </remarks>
        /// <returns>The object containing comments.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("assetUid", "The asset unique identifier to filter by", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("assetTypeUid", "The asset type unique identifier to filter by", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("followerUid", "The user's unique identifier to filter by", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CreatedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_sort", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered desc.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the rule results used to determine the data quality score for this score item based on a defined measure.", typeof(List<CommentDetail>)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetComments()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var comments = await Comments.GetCommentDetails(queryParams);

            return Ok(comments);
        }

        /// <summary>
        /// Returns an array of votes for a specific comment by emoji.
        /// </summary>
        /// <returns>An array of votes.</returns>
        [
            HttpGet,
            Route("{commentUid:Guid}/votes/{emoji}/users"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of voters using a particular emoji on a specific comment.", typeof(List<CommentVoterDetail>)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetCommentVotersByEmoji(Guid commentUid, Emoji emoji)
        {
            var model = await Comments.GetCommentVotersByCommentAndEmoji(commentUid, emoji);
            return Ok(model);
        }

        /// <summary>
        /// Returns an array of votes for a specific comment.
        /// </summary>
        /// <returns>An array of votes.</returns>
        [
            HttpGet,
            Route("{commentUid:Guid}/votes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of votes on a specific comment.", typeof(List<CommentVoteDetail>)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetCommentVotes(Guid commentUid)
        {
            var model = await Comments.GetCommentVotesByCommentUid(commentUid);
            return Ok(model);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("count/{resourceId:int}/{days:int}"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets counts for number of days and id.", typeof(List<CountModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetCountsRolledUpByCommentType(int resourceId, int days)
        {
            days *= -1;

            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;

            if (days == 0)
            {
                dateStart = new DateTime(2000, 1, 1);
            }
            else
            {
                dateStart = (days < 0) ? dateEnd.AddDays(days) : dateEnd.AddDays(-days);
            }

            if (resourceId == 0)
            {
                resourceId = SecurityContext.ResourceID;
            }

            var counts = await Comments.GetCommentCountsByFollower(resourceId, null, dateStart, dateEnd);
            var items = new List<CountModel>();

            Func<CommentType, int> getCommentCategoryCount = delegate (CommentType ct)
            {
                var commentsItem = counts.FirstOrDefault(x => x.CommentType == ct);

                return commentsItem == null ? 0 : commentsItem.Count;
            };

            //need to add a record for Discussion, Issue
            items.Add(new CountModel { Name = Label.CommentType_Social, Total = getCommentCategoryCount(CommentType.Social) });
            items.Add(new CountModel { Name = Label.CommentType_Action, Total = getCommentCategoryCount(CommentType.Issue) });

            return Ok(items);
        }

    }
}
