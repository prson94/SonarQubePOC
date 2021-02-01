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

namespace d360.web.Controllers.V2
{
    [ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/comments"), Authorize]
    public class CommentsController : BaseV2ApiController
    {
        #region DI

        ICommentRepository Comments;

        public CommentsController(ICommunityContext community, ICompanyContext company, ICommentRepository comments)
            : base(community, company)
        {
            Comments = comments;
        }

        #endregion

        /*
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("comments"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets comments.", typeof(List<CommentDetail>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            NonNullableParameters
        ]
        public List<CommentDetail> GetComments(CommentRequestData pageData)
        {
            List<CommentDetail> comments = null;
            if (!string.IsNullOrEmpty(pageData.ObjectType) && pageData.ObjectID.HasValue)
            {
                if (pageData.ObjectType.ToUpper() == "COMMENT")
                {
                    comments = Company.GetCommentDetailsByID(pageData.ObjectID.Value).ToList();
                }
                else
                {
                    comments = Company.GetCommentDetailsByType(
                        (SystemObjects)Enum.Parse(typeof(SystemObjects), pageData.ObjectType),
                        pageData.ObjectID.Value,
                        pageData.Skip,
                        pageData.Take,
                        pageData.DateFilter,
                        pageData.TypeFilter,
                        pageData.SearchFilter
                        ).ToList();
                }
            }
            else
            {
                comments = Company.GetCommentDetailsByFollower(
                    pageData.ObjectID ?? Company.CurrentResourceID,
                    pageData.Skip,
                    pageData.Take,
                    pageData.DateFilter,
                    pageData.TypeFilter,
                    pageData.SearchFilter
                    ).ToList();
            }

            var list = getChildren(comments, null, pageData.IsNg);
            return list;
        }

        List<CommentDetail> getChildren(List<CommentDetail> fullList, int? currentParentID, bool isNg)
        {
            var listToLoad = new List<CommentDetail>();

            if (fullList != null)
            {
                List<CommentDetail> thisLevel;
                if (currentParentID.HasValue)
                {
                    thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderBy(i => i.CreatedOn).ToList();
                }
                else
                {
                    thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderByDescending(i => i.CreatedOn).ToList();
                }
                thisLevel.ForEach(c =>
                {
                    if (!string.IsNullOrEmpty(c.TagsXml))
                    {
                        c.ParseTagXml();
                    }
                    if (!string.IsNullOrEmpty(c.VotesXml))
                    {
                        c.ParseVoteXml();
                    }
                    listToLoad.Add(c);
                    if (fullList.Any(i => i.ParentID == c.ID))
                    {
                        c.Comments = getChildren(fullList, c.ID, isNg);
                    }
                });
            }
            return listToLoad;
        }         
         */

        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding new comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public IHttpActionResult AddComment(CommentApiPostModel comment)
        {
            const string ERROR_HEADER = "Error adding comment";
            try
            {               
                var detail = Comments.AddComment(comment);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, detail));
            }
            catch (Exception ex)
            {
                //TODO
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADER, ex.Message);
            }
        }
        
        [
            HttpPost,
            Route("{commentUid:Guid}/votes/{emoji}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding new comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public IHttpActionResult AddVote(Guid commentUid, Emoji emoji)
        {
            try
            {
                var returnValue = Comments.AddVote(commentUid, Company.CurrentResourceID, emoji);
                if (returnValue)
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
                //TODO.
                return errorMessageResponse(HttpStatusCode.InternalServerError, "error", ex.Message);
            }
        }
       
        [
            HttpDelete,
            Route("{commentUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Removing a comment.", typeof(Object)),
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
                    return errorMessageResponse(HttpStatusCode.InternalServerError, "error", "Not able to successfully remove comment. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                //TODO.
                return errorMessageResponse(HttpStatusCode.InternalServerError, "error", ex.Message);
            }
        }
        
        [
            HttpDelete,
            Route("{commentUid:Guid}/votes/{emoji}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding new comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public IHttpActionResult DeleteVote(Guid commentUid, Emoji emoji)
        {
            try {
                var returnValue = Comments.DeleteVote(commentUid, Company.CurrentResourceID, emoji);
                if (returnValue)
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
                //TODO.
                return errorMessageResponse(HttpStatusCode.InternalServerError, "error", ex.Message);
            }
        }

        [
            HttpPut,
            Route("{commentUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Editing a comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult EditComment(Guid commentUid, CommentApiPutModel comment)
        {
            const string ERROR_HEADER = "Error updating comment";
            try
            {
                var detail = Comments.EditComment(commentUid, comment);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, detail));
            }
            catch (Exception ex)
            {
                //TODO
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADER, ex.Message);
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
            //SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "A description", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by OwningAssetDisplayPath.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_sort", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerResponse(HttpStatusCode.OK, "Returns the rule results used to determine the data quality score for this score item based on a defined measure.", typeof(DataQualityScoreItemEvidenceViewModel)),
            //SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            //SwaggerResponse(HttpStatusCode.Conflict, CONFLICT_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetComments()
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var model = new { };// await ScoringRepository.GetEvidenceForDataQualityScoreItem(scoreItemUid, queryParams);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", ex.GetFullExceptionData(false)));
            }
        }

        /// <summary>
        /// Returns an array of votes for a specific comment.
        /// </summary>
        /// <returns>An array of votes.</returns>
        [
            HttpGet,
            Route("{commentUid:Guid}/votes/{emoji}/users"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            //SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "A description", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by OwningAssetDisplayPath.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_sort", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerResponse(HttpStatusCode.OK, "Returns the rule results used to determine the data quality score for this score item based on a defined measure.", typeof(DataQualityScoreItemEvidenceViewModel)),
            //SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            //SwaggerResponse(HttpStatusCode.Conflict, CONFLICT_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetCommentVotersByEmoji(Guid commentUid, string emoji)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var model = new { };// await ScoringRepository.GetEvidenceForDataQualityScoreItem(scoreItemUid, queryParams);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", ex.GetFullExceptionData(false)));
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
            //SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "A description", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by OwningAssetDisplayPath.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_sort", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "int", ParameterType = "query", Required = false),
            //SwaggerResponse(HttpStatusCode.OK, "Returns the rule results used to determine the data quality score for this score item based on a defined measure.", typeof(DataQualityScoreItemEvidenceViewModel)),
            //SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            //SwaggerResponse(HttpStatusCode.Conflict, CONFLICT_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetCommentVotes(Guid commentUid)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var model = new { };// await ScoringRepository.GetEvidenceForDataQualityScoreItem(scoreItemUid, queryParams);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", ex.GetFullExceptionData(false)));
            }
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("count/{resourceId:int}/{days:int}"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets counts for number of days and id.", typeof(List<CountModel>)),
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

    }
}
