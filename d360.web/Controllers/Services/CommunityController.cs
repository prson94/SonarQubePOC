using System;
using System.Linq;
using System.Web.Http;
using d360.core.entities;
using d360.model;
using d360.core;
using System.Collections.Generic;
using d360.web.Models;
using System.Xml.Linq;
using d360.web.Filters;
using Microsoft.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    [ApiVersion("1.0"), RoutePrefix("services/community"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class CommunityController : BaseApiController
    {
        #region DI

        public CommunityController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion
        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("edit")]
        public dynamic EditComment(CommentData comment)
        {
            var relations = new List<CommentRelation>();
            CommentDetail dtl = Company.GetCommentDetail(comment.Comment.ID).SingleOrDefault(c => c.ParentID == null);
            dtl.ParseTagXml();

            if (comment.Comment.ID != 0)
            {
                if (comment.Tags != null)
                {
                    foreach (var tag in comment.Tags)
                    {
                        relations.Add(new CommentRelation { ObjectType = tag.Object, ObjectID = tag.ObjectID, Date = DateTime.UtcNow });
                    }
                }
                else
                    comment.Tags = new List<CommentTag>();
                                
                if ((dtl.Body != comment.Comment.Body || comment.Tags.Count() != dtl.Tags.Count())
                    && !string.IsNullOrWhiteSpace(comment.Comment.Body)
                    && dtl.CreatingResourceID == Company.CurrentResourceID
                    && dtl.DateCreated.Subtract(DateTime.UtcNow).Duration() < TimeSpan.FromMinutes(5))
                {
                    comment.Comment.DateEdited = DateTime.UtcNow;
                }
                if (dtl.IsDeleted != comment.Comment.IsDeleted && !dtl.IsDeletable.Value)
                {
                    comment.Comment.IsDeleted = false;   
                }

                dtl = Company.EditComment(comment.Comment, relations).FirstOrDefault(i => i.ID == comment.Comment.ID);
                

                if (!string.IsNullOrEmpty(dtl.TagsXml))
                {
                    dtl.ParseTagXml();
                }
                if (!string.IsNullOrEmpty(dtl.VotesXml))
                {
                    dtl.ParseVoteXml();
                }
            }

            return dtl;
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("comment")]
        public dynamic AddComment(CommentData comment)
        {
            var relations = new List<CommentRelation>();

            var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow };


            if (comment.Comment.ParentID.HasValue)
            {
                var parent = Company.GetCommentDetail(comment.Comment.ParentID.Value).FirstOrDefault(i => i.ID == comment.Comment.ParentID.Value);
                if (parent != null)
                {
                    comment.Comment.OwnerObjectType = parent.ObjectType;
                    comment.Comment.OwnerObjectID = parent.ObjectID;
                    relations.Add(new CommentRelation { ObjectID = parent.ObjectID, ObjectType = parent.ObjectType });
                    resourceRelation.CommentID = parent.ID;
                }
            }
            else
            {
                if (comment.ObjectID.HasValue)
                {
                    relations.Add(new CommentRelation { ObjectID = comment.ObjectID.Value, ObjectType = comment.ObjectType.ToString(), Date = DateTime.UtcNow });
                    comment.Comment.OwnerObjectType = comment.ObjectType.ToString();
                    comment.Comment.OwnerObjectID = comment.ObjectID.Value;
                }
                else 
                {
                    comment.Comment.OwnerObjectType = SystemObjects.Resource.ToString();
                    comment.Comment.OwnerObjectID = Company.CurrentResourceID;
                }
            }

            relations.Add(resourceRelation);

            if (comment.Tags == null)
                comment.Tags = new List<CommentTag>();

            foreach (var tag in comment.Tags)
            {
                relations.Add(new CommentRelation { ObjectType = tag.Object, ObjectID = tag.ObjectID, Date = DateTime.UtcNow });
            }
            
            var dtl = Company.AddComment(comment.Comment, relations).FirstOrDefault(i => i.ID == comment.Comment.ID);
            
            if (!string.IsNullOrEmpty(dtl.TagsXml))
            {
                dtl.ParseTagXml();
            }
            if (!string.IsNullOrEmpty(dtl.VotesXml))
            {
                dtl.ParseVoteXml();
            }
            
            return dtl;
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("counts")]
        public List<CommentCount> GetCommentCounts(CommentRequestData pageData)
        {            
            List<CommentCount> counts = new List<CommentCount>();
            if (!string.IsNullOrEmpty(pageData.ObjectType) && pageData.ObjectID.HasValue)
            {
                if (pageData.ObjectType.ToUpper() == "COMMENT") return null;

                counts = Company.GetCommentCountByType((SystemObjects)Enum.Parse(typeof(SystemObjects), pageData.ObjectType), pageData.ObjectID.Value, pageData.DateFilter, pageData.SearchFilter).ToList();
            }
            else
            {
                counts = Company.GetCommentCountByFollower(Company.CurrentResourceID,pageData.DateFilter,pageData.SearchFilter).ToList();
            }
                
            return counts;
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost,Route("vote")]
        public List<CommentVote> VoteComment(CommentVote vote)
        {

            //should only be +/ -1
            if (vote.Vote < 0)
                vote.Vote = -1;
            else
                vote.Vote = 1;

            vote.ResourceID = Company.CurrentResourceID;

            var commentVotes = Company.VoteComment(vote.CommentID, vote.ResourceID, vote.Vote);
            return commentVotes.ToList();

        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("comments")]
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
                    thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderBy(i => i.DateCreated).ToList();
                }
                else
                {
                    thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderByDescending(i => i.DateCreated).ToList();
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
    }
}
