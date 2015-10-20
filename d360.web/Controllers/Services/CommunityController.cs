using System;
using System.Linq;
using System.Web.Http;
using d360.core.entities;
using d360.model;
using d360.core;
using System.Collections.Generic;
using d360.web.Models;
using System.Xml.Linq;
using d360.workflow;

namespace d360.web.Controllers.Services
{
    [RoutePrefix("services/community"), Authorize]
    public class CommunityController : BaseApiController
    {
        #region DI

        public CommunityController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [HttpPost, Route("comment")]
        public dynamic AddComment(CommentData comment)
        {
            var relations = new List<CommentRelation>();

            var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString() };

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
                    relations.Add(new CommentRelation { ObjectID = comment.ObjectID.Value, ObjectType = comment.ObjectType.ToString() });
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

            foreach (var tag in comment.Tags)
            {
                relations.Add(new CommentRelation { ObjectType = tag.Object, ObjectID = tag.ObjectID });
            }

            var dtl = Company.AddComment(comment.Comment, relations).FirstOrDefault(i => i.ID == comment.Comment.ID);

            if (!string.IsNullOrEmpty(dtl.TagsXml))
            {
                dtl.ParseTagXml();
            }

            if (dtl.CommentTypeID == core.enums.CommentType.Issue)
            {
                var processor = new Processor();
                var dictionary = new Dictionary<string, object>();
                dictionary.Add("CompanyID", Company.CurrentCompanyID);
                dictionary.Add("CommentID", dtl.ID);
                processor.CreateNewWorkflowInstance(WorkflowVersionMap.WorkIssue_vCurrent, dictionary);
            }

            //if (comment.ParentID.HasValue) Clients.Others.newComment(dtl, comment.ParentID);

            return dtl;
        }

        [HttpPost, Route("comments")]
        public List<CommentDetail> GetComments(CommentRequestData pageData)
        {
            List<CommentDetail> comments = null;
            if (!string.IsNullOrEmpty(pageData.ObjectType) && pageData.ObjectID.HasValue)
            {
                comments = Company.GetCommentDetailsByType(
                    (SystemObjects)Enum.Parse(typeof(SystemObjects), pageData.ObjectType),
                    pageData.ObjectID.Value,
                    pageData.Skip,
                    pageData.Take,
                    pageData.DateFilter,
                    pageData.TypeFilter
                    ).ToList();
            }
            else
            {
                comments = Company.GetCommentDetailsByFollower(
                    Company.CurrentResourceID,
                    pageData.Skip,
                    pageData.Take,
                    pageData.DateFilter,
                    pageData.TypeFilter
                    ).ToList();
            }

            var list = getChildren(comments, null);
            return list;
        }

        List<CommentDetail> getChildren(List<CommentDetail> fullList, int? currentParentID)
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
                    listToLoad.Add(c);
                    if (fullList.Any(i => i.ParentID == c.ID))
                    {
                        c.Comments = getChildren(fullList, c.ID);
                    }
                });
            }

            return listToLoad;
        }
    }
}
