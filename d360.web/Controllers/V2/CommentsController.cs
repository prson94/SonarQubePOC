using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using repositories;
using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
	[ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/comments"), Authorize]
    public class CommentsController : BaseV2ApiController
    {
        #region DI

        private readonly ISocial Comments;
		
		public CommentsController(ICoreComponentSet set, ISocial comments) : base(set)
        {
            Comments = comments;
        }
		#endregion
		private async Task<bool> commentsDisabled()
		{
			bool commentsDiabled = await Community.ReadSettingValueAsync<bool>(SecurityContext.CompanyID, Setting.DisableCommunityPosting);
			return commentsDiabled;
		}

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
			SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> AddComment(CommentApiPostModel comment)
		{
			var commentData = await Comments.AddComment(comment);

			if (!commentData.IsSuccess)
			{
				return errorMessageResponse((HttpStatusCode)commentData.StatusCode, commentData.Message);
			}

			if (comment.Tags != null && comment.Tags.Count > 0)
			{
				if (commentData.Data.Tags.Count > 0)
				{
						await Queue.CreateMessageAsync(constants.Queue.Notification, new QueueMessage<int>
						{
							CompanyId = SecurityContext.CompanyID,
							CompanyPrefix = SecurityContext.CompanyPrefix,
							Payload = commentData.Data.ID
						});
				}
			}
			return Created("", commentData.Data);
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
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddVote(Guid commentUid, Emoji emoji)
        {
			if (await commentsDisabled())
			{
				return errorMessageArgumentResponse(string.Format(Error.DisabledByEnvironmentSetting, Setting.DisableCommunityPosting));
			}
			else
			{
				var queryParams = Request.GetQueryNameValuePairs();
				bool toggle = true;

				if (queryParams.Any(qp => qp.Key.ToLower() == "toggle"))
				{
					var toggleString = queryParams.FirstOrDefault(x => x.Key.ToLower() == "toggle").Value;
					bool.TryParse(toggleString, out toggle);
				}

				var created = Comments.AddVote(commentUid, SecurityContext.ResourceID, emoji, toggle);
				if (created.IsSuccess)
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created));
				}
				else
				{
					return Ok();
				}
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
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteComment(Guid commentUid)
        {
			if (await commentsDisabled())
			{
				return errorMessageArgumentResponse(string.Format(Error.DisabledByEnvironmentSetting, Setting.DisableCommunityPosting));
			}
			else
			{
				if (Comments.DeleteComment(commentUid).IsSuccess)
				{
					return Ok();
				}
				else
				{
					return errorMessageResponse(HttpStatusCode.InternalServerError, Error.UnknownError, Error.CommentRetryRemove);
				}
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
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteVote(Guid commentUid, Emoji emoji)
        {
			if (await commentsDisabled())
			{
				return errorMessageArgumentResponse(string.Format(Error.DisabledByEnvironmentSetting, Setting.DisableCommunityPosting));
			}
			else
			{
				var delete = Comments.DeleteVote(commentUid, SecurityContext.ResourceID, emoji);
				if(delete.IsSuccess)
				{
					return Ok(); 
				}
				else
				{
					return errorMessageResponse((HttpStatusCode)delete.StatusCode, delete.Message);
				}
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
			SwaggerResponse(HttpStatusCode.OK, "Editing a comment.", typeof(object)),
			SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> EditComment(Guid commentUid, CommentApiPutModel comment)
		{
			if (await commentsDisabled())
			{
				return errorMessageArgumentResponse(string.Format(Error.DisabledByEnvironmentSetting, Setting.DisableCommunityPosting));
			}
			else
			{
				var detail = await Comments.EditComment(commentUid, comment);

				if (!detail.Item1.IsSuccess)
				{
					return errorMessageResponse((HttpStatusCode)detail.Item1.StatusCode, detail.Item1.Message);
				}

				if (detail.Item1.Data?.Tags.Count > 0)
				{
					await SendCommentNotificationAsync(detail.Item2,detail.Item1.Data.ID);
				}
					return Ok(detail.Item1.Data);
			}
		}

		private async Task SendCommentNotificationAsync(List<Asset> taggedAssets, int commentId)
		{
			if(Comments.ProcessWithQueue(taggedAssets))
			{
						await Queue.CreateMessageAsync(constants.Queue.Notification, new QueueMessage<int>
						{
							CompanyId = SecurityContext.CompanyID,
							CompanyPrefix = SecurityContext.CompanyPrefix,
							Payload = commentId
						});
			}
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
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetComments()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var comments = await Comments.GetCommentDetails(queryParams);

            return Ok(comments.Data);
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
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
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
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))
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
