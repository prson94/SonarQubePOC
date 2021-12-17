using d360.core;
using d360.core.entities;
using d360.core.enums;
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
using System.Web.Http;
using System.Web.Http.Description;
using Resources;

namespace d360.web.Controllers.V2
{
    [ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/comments"), Authorize]
    public class CommentsController : BaseV2ApiController
    {
        #region DI

        readonly ICommentRepository Comments;

        public CommentsController(ICoreComponentSet set, ICommentRepository comments): base(set)
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
            try
            {               
                var detail = await Comments.AddComment(comment);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, detail));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.CommentCreatePermission}
                };
                return DetermineUnhandledException(
                    ex,
                    "Error adding comment",
                    messages,
                    new Dictionary<string, string> { { "Method Name", "AddComment" } }
                );
            }
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

            try
            {
                var created = Comments.AddVote(commentUid, Company.CurrentResourceID, emoji, toggle);
                if (created)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created));
                }
                else
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK));
                }
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = $"Comment with Uid {commentUid} does not exist." }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.VotingOnCommentUsingEmoji,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "AddVote" }, { "CommentUid", commentUid.ToString() }, { "Emoji", emoji.ToString() } }
                );
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
            try
            {
                if (Comments.DeleteComment(commentUid))
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK));
                }
                else
                {
                    return errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , CommentsAPIMessages.CommentRetryRemove);
                }
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.CommentCreatePermission },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(CommentsAPIMessages.CommentNotFound, commentUid.ToString()) }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorDeletingComment,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "DeleteComment" }, { "CommentUid", commentUid.ToString() } }
                );
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
            try {
                Comments.DeleteVote(commentUid, Company.CurrentResourceID, emoji);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(CommentsAPIMessages.CommentNotFound, commentUid.ToString()) }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.RestrictVoteRemove,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "DeleteVote" }, { "CommentUid", commentUid.ToString() }, { "Emoji", emoji.ToString() } }
                );
            }
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
            SwaggerResponse(HttpStatusCode.OK, "Editing a comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> EditComment(Guid commentUid, CommentApiPutModel comment)
        {
            try
            {
                var detail = await Comments.EditComment(commentUid, comment);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, detail));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.VotePermission },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(CommentsAPIMessages.CommentNotFound, commentUid) }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorUpdateComment,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "EditComment" }, { "CommentUid", commentUid.ToString() } }
                );
            }
        }

        /// <summary>
        /// Returns an array of comments along with the total number and the current page number and size.
        /// </summary>
        /// <remarks>
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
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
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var comments = await Comments.GetCommentDetails(queryParams);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, comments));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.CommentViewPermission }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorGetComment,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetComments" } }
                );
            }
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
            try
            {
                var model = await Comments.GetCommentVotersByCommentAndEmoji(commentUid, emoji);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.VotePermission },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(CommentsAPIMessages.CommentNotFound, commentUid.ToString()) }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorGetVoterBasedOnCommentEmoji,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetCommentVotersByEmoji" }, { "CommentUid", commentUid.ToString() }, { "Emoji", emoji.ToString() } }
                );
            }
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
            try
            {
                var model = await Comments.GetCommentVotesByCommentUid(commentUid);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.VotePermission},
                    new StatusCodeErrorMessage { Status = HttpStatusCode.NotFound, ErrorMessage = string.Format(CommentsAPIMessages.CommentNotFound, commentUid.ToString()) }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorGetVoterBasedOnComment,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetCommentVotes" }, { "CommentUid", commentUid.ToString() } }
                );
            }
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
            try
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
                    resourceId = Company.CurrentResourceID;
                }

                var counts = await Comments.GetCommentCountsByFollower(resourceId, null, dateStart, dateEnd);
                var items = new List<CountModel>();

                Func<CommentType, int> getCommentCategoryCount = delegate (CommentType ct)
                {
                    var commentsItem = (counts.FirstOrDefault(x => x.CommentType == ct));
                    return commentsItem == null ? 0 : commentsItem.Count;
                };

                //need to add a record for Discussion, Issue
                items.Add(new CountModel { Name = Resources.Core.CommentType_Social, Total = getCommentCategoryCount(CommentType.Social) });
                items.Add(new CountModel { Name = Resources.Core.CommentType_Action, Total = getCommentCategoryCount(CommentType.Issue) });

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, items));
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>
                {
                    new StatusCodeErrorMessage { Status = HttpStatusCode.BadRequest, ErrorMessage = ApiMessages.ErrorInvalidDatasetMessage },
                    new StatusCodeErrorMessage { Status = HttpStatusCode.Forbidden, ErrorMessage = CommentsAPIMessages.RollupCountPermission }
                };
                return DetermineUnhandledException(
                    ex,
                    CommentsAPIMessages.ErrorGetRollupCount,
                    messages,
                    new Dictionary<string, string> { { "Method Name", "GetCountsRolledUpByCommentType" }, { "resourceId", resourceId.ToString() } }
                );
            }
        }

    }
}
