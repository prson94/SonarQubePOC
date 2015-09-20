using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//using Microsoft.AspNet.SignalR;
//using Microsoft.AspNet.SignalR.Hubs;
using Autofac;
using d360.core.entities;
using d360.core;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using d360.extensions.info;
using d360.model;
using d360.extensions.caching;
using d360.extensions.storage;
using d360.extensions.queue;

namespace d360.web.Models.Hubs
{
    //public class CommentData
    //{
    //    public string ObjectType { get; set; }
    //    public int? ObjectID { get; set; }
    //    public Comment Comment { get; set; }
    //}
    //public class RequestData
    //{
    //    public string ObjectType { get; set; }

    //    public int? ObjectID { get; set; }

    //    public int Skip { get; set; }

    //    public int Take { get; set; }

    //    public int DateFilter { get; set; }

    //    public int TypeFilter { get; set; }
    //}

    //[HubName("social")]
    public class SocialHub: Hub
    {
        #region DI

        #endregion

        //public dynamic AddComment(CommentData comment)
        //{
        //    var Company = getService();

        //    var relations = new List<CommentRelation>();

        //    var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString() };

        //    if (comment.Comment.ParentID.HasValue)
        //    {
        //        var parent = Company.GetCommentDetail(comment.Comment.ParentID.Value).FirstOrDefault(i => i.ID == comment.Comment.ParentID.Value);
        //        if (parent != null)
        //        {
        //            comment.Comment.OwnerObjectType = parent.ObjectType;
        //            comment.Comment.OwnerObjectID = parent.ObjectID;
        //            relations.Add(new CommentRelation { ObjectID = parent.ObjectID, ObjectType = parent.ObjectType });
        //            resourceRelation.CommentID = parent.ID;
        //        }
        //    }
        //    else
        //    {
        //        relations.Add(new CommentRelation { ObjectID = comment.ObjectID.Value, ObjectType = comment.ObjectType });
        //        comment.Comment.OwnerObjectType = comment.ObjectType.ToString();
        //        comment.Comment.OwnerObjectID = comment.ObjectID.Value;
        //    }

        //    relations.Add(resourceRelation);

        //    var dtl = Company.AddComment(comment.Comment, relations).FirstOrDefault(i => i.ID == comment.Comment.ID);
        //    Company = null;

        //    if (comment.Comment.ParentID.HasValue) Clients.Others.newComment(dtl, comment.Comment.ParentID);

        //    return dtl;        
        //}

        //public List<CommentDetail> GetComments(RequestData pageData)
        //{
        //    var ResourceService = getService();
        //    List<CommentDetail> comments = null;
        //    if (!string.IsNullOrEmpty(pageData.ObjectType) && pageData.ObjectID.HasValue)
        //    {
        //        comments = ResourceService.GetCommentDetailsByType(
        //            (SystemObjects)Enum.Parse(typeof(SystemObjects), pageData.ObjectType), 
        //            pageData.ObjectID.Value, 
        //            pageData.Skip, 
        //            pageData.Take, 
        //            pageData.DateFilter,
        //            pageData.TypeFilter
        //            ).ToList();
        //    }
        //    else
        //    {
        //        comments = ResourceService.GetCommentDetailsByFollower(
        //            ResourceService.CurrentResourceID, 
        //            pageData.Skip,
        //            pageData.Take,
        //            pageData.DateFilter,
        //            pageData.TypeFilter
        //            ).ToList();
        //    }

        //    var list = getChildren(comments, null);
        //    return list;
        //}

        //CompanyContext getService()
        //{
        //    var queue = new AzureQueueSource();
        //    var cache = new MemoryCachingProvider();
        //    var storage = new AzureStorageProvider();
            
        //    var c = HttpContext.Current.Request.Url.DnsSafeHost;
        //    var u = HttpContext.Current.User.Identity.Name.ToLower();

        //    var sec = new UriSecurityContextProvider 
        //    {
        //        RawCompanyID = c.Substring(0, c.IndexOf(".")).ToLower(),
        //        RawUserID = u
        //    };
            
        //    var community = new CommunityContext(new MemoryCachingProvider(), new AzureQueueSource(), sec);
        //    return new CompanyContext(community, cache, queue, sec);
            
        //    //return new ResourceService(cache, sclProvider, storage, community, context, queue);
        //}

        //List<CommentDetail> getChildren(List<CommentDetail> fullList, int? currentParentID)
        //{
        //    var listToLoad = new List<CommentDetail>();

        //    if (fullList != null)
        //    {
        //        List<CommentDetail> thisLevel;
        //        if (currentParentID.HasValue)
        //        {
        //            thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderBy(i => i.DateCreated).ToList();
        //        }
        //        else
        //        {
        //            thisLevel = fullList.Where(i => i.ParentID == currentParentID).OrderByDescending(i => i.DateCreated).ToList();
        //        }
        //        thisLevel.ForEach(c =>
        //        {
        //            listToLoad.Add(c);
        //            if (fullList.Any(i => i.ParentID == c.ID))
        //            {
        //                c.Comments = getChildren(fullList, c.ID);
        //            }
        //        });
        //    }

        //    return listToLoad;
        //}
    }
}