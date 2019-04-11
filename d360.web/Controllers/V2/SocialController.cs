using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core.entities;
using d360.core;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/social"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class SocialController : BaseV2ApiController
    {
        public SocialController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("FollowingBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters
        ]
        public async Task<HttpResponseMessage> FollowingBreakdownByResource(int id)
        {
            var query = await Company.QueryAsync<dynamic>(@"select  		                
        		                T.[Type], 
        		                T.TypeName,
        		                T.TypeID, 		
        		                T.[Count],
        		                coalesce(S.IconBackColor, '#000') as IconBackColor,
                                coalesce(S.IconForeColor, '#fff') as IconForeColor,
                                coalesce(S.IconText, substring(T.TypeName, 1, 2)) as IconText
                        from (
                        select 
        	                [Type], 
        	                TypeName, 
        	                TypeID, 
        	                count(1) as [Count]
                        from 
        	                FollowDetail
                        where 
        	                ResourceID = @r
        	                and ObjectType not in ('ArtifactType', 'PolicyType', 'ReferenceItemType', 'ResourceType', 'TaxonomyType')
                        group by Type, TypeName, TypeID) T
                        left join ObjectStyle S on  T.[Type] = S.ObjectType and T.TypeID = S.ObjectID order by TypeName", new { r = id });

            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("ResponsibilityBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByResource(int id, int? responsibilityTypeID = null)
        {

            var sql = $@"select  
        		                    {QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
        			                			                 R.Type,
        			                 R.TypeID,
        			                 R.[Count] * R.AssetCount as [Count]
        		                from AssetType T
        		                inner join (
        			                select 
        						                C.[Type],
        						                C.TypeID,
        						                count(1) as [Count],
        										A.Count as AssetCount
        			                from ResponsibilityDetail C
        							cross apply (
        								select 
        										case when C.ApplyToType = 1 and C.AssetID = 0 then 
        											(select count(*) from Asset where AssetTypeID = C.AssetTypeID) 
        										else 
        											1
        								end as [Count]
        							) A
        			                where		C.IsVisible = 1 
        						                 {(responsibilityTypeID.HasValue ? "and C.ResponsibilityTypeID = @rt" : "")}
        						                and C.ResourceID = @r
        			                group by C.[Type], C.TypeID, A.Count
        		                ) R on R.[Type] = T.Object and R.TypeID = T.ObjectID
        						";

            var query = await Company.QueryAsync<dynamic>(sql, new { r = id, rt = responsibilityTypeID });

            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("ResponsibilityBreakdownByGroup"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByGroup(int id)
        {
            var sql = $@"            
        select		RD.Type,
        			RD.TypeID,
        			{QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
        			count(1) as [Count]
        from		ResponsibilityDetail RD 
        			inner join AssetType T on T.ID = RD.AssetTypeID and RD.SecurityAsset = 'G' and RD.SecurityAssetID = @id and RD.IsVisible = 1
        group by    RD.Type, 
                    RD.TypeID, 
                    { QueryConstants.HighLevelTypeCaseStatement} + T.Name 
        order by    { QueryConstants.HighLevelTypeCaseStatement} + T.Name";

            var query = await Company.QueryAsync<dynamic>(sql, new { id });

            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("edit"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Editing a comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters
        ]
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

        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("comment"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Adding new comment.", typeof(Object)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters
        ]
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

        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("counts"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets comments count.", typeof(List<CommentCount>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters
        ]
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
                counts = Company.GetCommentCountByFollower(Company.CurrentResourceID, pageData.DateFilter, pageData.SearchFilter).ToList();
            }

            return counts;
        }

        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("vote"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Stores the up/down vote of a comment.", typeof(List<CommentVote>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters
        ]
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

        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("comments"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets comments.", typeof(List<CommentDetail>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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