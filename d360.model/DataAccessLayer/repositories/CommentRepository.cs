using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.core.exceptions;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class CommentRepository : BaseRepository, ICommentRepository
    {
        #region DI

        internal ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        internal ICommunityContext Community;

        public CommentRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
            : base(companyContext)
        {
            this.CompanyContext = companyContext;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
            this.Community = community;
        }

        #endregion DI

        #region Common Sql

        const string COMMENT_TABLE_COLUMNS = @"C.Uid, C.ID, C.ParentID, C.CommentType, iif(C.IsDeleted = 1, '[Comment removed]', C.Body) as Body, C.CreatedOn, C.CreatedBy, C.UpdatedBy, C.UpdatedOn, C.IsDeleted";

        const string TAGS_JSON_SQL = @"coalesce(
			(
			select	CRA.Uid as AssetUid,
					CRA.AssetTypeUid,
					AD.DisplayPath as [Path],
					CRA.TypeName,
					U.Url,
					CRA.BackColor as IconBackColor,
					CRA.ForeColor as IconForeColor
			from	CommentRelation CR
					inner join AssetDetail CRA on CRA.ID = CR.AssetID and CR.CommentID = C.ID
					inner join graph.AssetNodeDisplayPath AD on AD.ID = CRA.ID
					cross apply GetAssetUrlById(CRA.ID) U
			for		json path
			), '[]') as TagsJson";

        const string EMOJIS_JSON_SQL = @"coalesce(
			(
			select	count(ResourceID) as [Count],
					Emoji
			from	CommentVote
			where	CommentID = C.ID
			group by Emoji
			for		json path
			), '[]') as EmojisJson";

        #endregion

		public async Task<CommentDetail> AddComment(CommentApiPostModel comment, CommentType commentType = CommentType.Social)
		{
			validateComment(comment);

            long? commentedOnAssetId = null;

            int? parentId = null;
            long? assetId = null;
            Asset commentAsset = null;
            if (comment.ParentUid.HasValue && comment.ParentUid != Guid.Empty)
            {
                var parentComment = CompanyContext.Filter<Comment>(o => o.Uid == comment.ParentUid.Value, o => o.Asset).SingleOrDefault();
                if (parentComment == null)
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Parent comment not found");
                }
                else
                {
                    commentedOnAssetId = CompanyContext.Filter<Asset>(a => a.uid == comment.AssetUid).FirstOrDefault().ID;                    
                    parentId = parentComment.ID;
                    comment.AssetUid = parentComment.Asset.uid;
                    assetId = parentComment.Asset.ID;

                    commentAsset = parentComment.Asset;
                }
            }

            if (comment.AssetUid == Guid.Empty)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, "", "You must provide a valid Uid for the AssetUid property.");
            }

            if (!assetId.HasValue)
            {
                commentAsset = CompanyContext.Filter<Asset>(a => a.uid == comment.AssetUid, a => a.AssetType).FirstOrDefault();
                if (commentAsset == null)
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Asset with provided Uid does not exist.");
                }
                if (!commentAsset.AssetType.Class.AsInfoModel().AllowCommentsOnAsset)
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Comments may not be created on asset with provided Uid.");
                }
                assetId = commentAsset.ID;
            }

            if (commentAsset != null)
            {
                if (!CompanyContext.HasAssetPermission(commentAsset.Object, commentAsset.ObjectID, Permission.ReadAsset))
                {
                    throw new GenericException(System.Net.HttpStatusCode.Forbidden, "", "You do not have permissions to add a comment to this asset.");
                }
            }

			var dbComment = new Comment
			{
				CommentType = commentType,
				CreatedBy = CompanyContext.CurrentResourceID,
				CreatedOn = DateTime.UtcNow,
				IsDeleted = false,
				AssetID = assetId.Value,
				Body = comment.Body,
				ParentID = parentId,
				Uid = Guid.NewGuid(),
				UpdatedBy = CompanyContext.CurrentResourceID,
				UpdatedOn = DateTime.UtcNow
			};
			var commentAdded = CompanyContext.Add(dbComment);

			if (commentAdded)
			{
				var commentId = dbComment.ID;
				if (comment.Tags != null && comment.Tags.Count > 0)
				{
					var taggedAssets = CompanyContext.Filter<Asset>(o => comment.Tags.Contains(o.uid)).ToList();
					foreach (var r in taggedAssets)
					{
						CompanyContext.CommentRelations.Add(new CommentRelation { CommentID = commentId, AssetID = r.ID });						
					}

					await CompanyContext.SaveChangesAsync();

					SendCommentNotification(taggedAssets, dbComment, commentedOnAssetId);
				}
				CompanyContext.Connection.Execute("delete C from CommentRelation C left join Asset A on A.ID = C.AssetID where C.CommentID = @commentId and A.ID is null", new { commentId });

                return await GetCommentDetailByUid(dbComment.Uid).ConfigureAwait(false);
            }
            else
            {
                throw new GenericException(System.Net.HttpStatusCode.InternalServerError, "", "Comment was not successfully created.");
            }
        }

        public bool AddVote(Guid commentUid, int resourceId, Emoji emoji, bool toggle = true)
        {
            var emojiGroup = emoji.GetGroupName();
            var groupedEmojis = new List<int>();

            if (!string.IsNullOrEmpty(emojiGroup))
            {
                groupedEmojis = Emoji.ThumbsDown
                    .GetEmojiInfoList()
                    .Where(e => e.Group == emojiGroup)
                    .Select(e => e.ID)
                    .ToList();
            }
            else
            {
                groupedEmojis.Add((int)emoji);
            }

            var comment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();
            if (comment != null)
            {
                var commentVote = CompanyContext.Filter<CommentVote>(o => o.CommentID == comment.ID && o.ResourceID == resourceId && groupedEmojis.Contains((int)o.Emoji)).FirstOrDefault();
                if (commentVote == null)
                {
                    if (CompanyContext.Add(new CommentVote { CommentID = comment.ID, ResourceID = resourceId, Emoji = emoji }))
                    {
                        return true;
                    }
                }
                else if (commentVote.Emoji != emoji)
                {
                    commentVote.Emoji = emoji;
                    return CompanyContext.Update(commentVote);
                }
                else if (toggle == true)
                {
                    DeleteVote(commentUid, resourceId, emoji);
                }

                return false;
            }
            else
            {
                throw new NotFoundException("comment");
            }
        }

        public bool DeleteComment(Guid commentUid)
        {
            var dbComment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();

            if (dbComment == null)
            {
                throw new StatusCodeException(System.Net.HttpStatusCode.NotFound);
            }

            if (dbComment.CreatedBy != CompanyContext.CurrentResourceID && !CompanyContext.CurrentResourceIsAdmin)
            {
                throw new GenericException(System.Net.HttpStatusCode.Forbidden, "You are not the creator of this comment or administrator and may not update it.", "You are not the creator of this comment or administrator and may not update it.");
            }

            bool commentUpdated = false;
            if (CompanyContext.Any<Comment>(c => c.ParentID == dbComment.ID))
            {
                dbComment.IsDeleted = true;
                dbComment.UpdatedBy = CompanyContext.CurrentResourceID;
                dbComment.UpdatedOn = DateTime.UtcNow;

                commentUpdated = CompanyContext.Update(dbComment);
            }
            else
            {
                commentUpdated = CompanyContext.Delete(dbComment);
            }

            if (commentUpdated)
            {
                return true;
            }
            else
            {
                throw new GenericException(System.Net.HttpStatusCode.InternalServerError, "Comment was not successfully removed.");
            }
        }

        public bool DeleteVote(Guid commentUid, int resourceId, Emoji emoji)
        {
            var comment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();
            if (comment != null)
            {
                var commentVote = CompanyContext.Filter<CommentVote>(o => o.CommentID == comment.ID && o.ResourceID == resourceId && o.Emoji == emoji).FirstOrDefault();
                if (commentVote != null)
                {
                    if (CompanyContext.Delete(commentVote))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                throw new NotFoundException("comment");
            }
        }

        public async Task<CommentDetail> EditComment(Guid commentUid, CommentApiPutModel comment)
        {
            validateComment(comment);

            var dbComment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();

            if (dbComment == null)
            {
                throw new NotFoundException("comment");
            }

            if (dbComment.CreatedBy != CompanyContext.CurrentResourceID)
            {
                throw new GenericException(System.Net.HttpStatusCode.Forbidden, "You are not the creator of this comment and may not update it.", "You are not the creator of this comment and may not update it.");
            }


            dbComment.Body = comment.Body;
            dbComment.UpdatedBy = CompanyContext.CurrentResourceID;
            dbComment.UpdatedOn = DateTime.UtcNow;

            var commentUpdated = CompanyContext.Update(dbComment);

            if (commentUpdated)
            {
                var commentId = dbComment.ID;

                CompanyContext.Connection.Execute("delete CommentRelation where CommentID = @commentId", new { commentId });

				if (comment.Tags != null)
				{
					if ( comment.Tags.Count > 0)
					{
						var taggedAssets = CompanyContext.Filter<Asset>(o => comment.Tags.Contains(o.uid)).ToList();
						foreach (var r in taggedAssets)
						{
							CompanyContext.CommentRelations.Add(new CommentRelation { CommentID = commentId, AssetID = r.ID });
						}

						await CompanyContext.SaveChangesAsync();

						SendCommentNotification(taggedAssets, dbComment);
					}

                    CompanyContext.Connection.Execute("delete C from CommentRelation C left join Asset A on A.id = C.Assetid where C.CommentID = @commentId and A.ID is null", new { commentId });
                }

                return await GetCommentDetailByUid(dbComment.Uid).ConfigureAwait(false);
            }
            else
            {
                throw new GenericException(System.Net.HttpStatusCode.InternalServerError, "", "Comment was not successfully updated.");
            }
        }

        public async Task<List<CommentCount>> GetCommentCountsByFollower(int resourceId, string searchPhrase = null, DateTime? rangeStart = null, DateTime? rangeEnd = null)
        {
            var sql = @"
	SELECT	i.CommentType, 
			u.[Count], 
			u.CommentTypeName 
	FROM	(
			select	count(1) as [All],
					sum(case when C.CommentType = 2 then 1 else 0 end) as Discussions,
					sum(case when C.CommentType = 5 then 1 else 0 end) as Issues
			from	Comment C
			where	C.ID in	(
					select	O.CommentID as ID
					from	FollowDetail F
							inner join CommentRelation O on O.AssetID = F.AssetID
					where	F.ResourceID = @resourceId
					union all
					select	ID 
					from	Comment 
					where	CreatedBy = @resourceId
					union all
					select	O.ID 
					from	Comment O
							inner join Asset A on A.ID = O.AssetID
							inner join ResponsibilityDetail R on R.ResourceID = @resourceId and R.AssetID = A.ID
					)
			AND C.IsDeleted = 0
			AND (
					(C.CreatedOn between @rangeStart and @rangeEnd and @rangeStart is not null and @rangeEnd is not null) or
					(@rangeStart is null and @rangeEnd is null)
				)
			AND C.ParentID is null
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
			AND iif(C.CreatedBy = @resourceID, 1, 0) = 1
		) t
		UNPIVOT
			(	[Count]
				for [CommentTypeName] in ([All], Discussions, Issues)
			) u
			inner join
			(
			select	* 
			from	(
					select	0 as [All],
							2 as Discussions,
							5 as Issues
					)	t2
						unpivot
						(
						CommentType for CommentTypeName in ([All], Discussions, Issues)
						) u2
			) i on i.CommentTypeName = u.CommentTypeName
order by u.CommentTypeName";

            var request = await CompanyContext.QueryAsync<CommentCount>(sql, new { resourceId, searchPhrase, rangeStart, rangeEnd });
            var counts = request.ToList();
            return counts;
        }

		public async Task<CommentDetails> GetCommentDetails(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();
			var queryFieldOptions = new List<DefaultFilter>
			{
				new DefaultFilter("Body", "C.Body", SqlFieldType.Text),
				new DefaultFilter("Uid", "C.Uid", SqlFieldType.Guid),
				new DefaultFilter("CreatedOn", "C.CreatedOn", SqlFieldType.DateTime),
				new DefaultFilter("UpdatedOn", "C.UpdatedOn", SqlFieldType.DateTime),
				new DefaultFilter("Url", "AUrl.Url", SqlFieldType.Text),
				new DefaultFilter("AssetPath", "AP.DisplayPath", SqlFieldType.Text),
				new DefaultFilter("ResourceName", "R.FirstName + ' ' + R.LastName", SqlFieldType.Text)
			};

            DynamicParameters advFilterArgs = null;
            List<string> advFilterStatements = null;
            CompanyContext.ParseAdvancedFilterQueryParameter(queryParams, queryFieldOptions, out advFilterArgs, out advFilterStatements);
            if (advFilterArgs != null && advFilterStatements != null)
            {
                dbArgs.AddDynamicParams(advFilterArgs);
                whereStatements.AddRange(advFilterStatements);
            }

            Guid assetUid = Guid.Empty;
            bool assetUidPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "assetuid"))
            {
                var asset = queryParams.FirstOrDefault(x => x.Key.ToLower() == "assetuid").Value;
                assetUidPresent = Guid.TryParse(asset, out assetUid);
            }
            Guid assetTypeUid = Guid.Empty;
            bool assetTypeUidPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "assettypeuid"))
            {
                var assetType = queryParams.FirstOrDefault(x => x.Key.ToLower() == "assettypeuid").Value;
                assetTypeUidPresent = Guid.TryParse(assetType, out assetTypeUid);
            }
            Guid followerUid = Guid.Empty;
            bool followerUidPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "followeruid"))
            {
                var follower = queryParams.FirstOrDefault(x => x.Key.ToLower() == "followeruid").Value;
                followerUidPresent = Guid.TryParse(follower, out followerUid);
            }

            #region "Ng additional filter: set variable"

            var followerCurrResUidPresent = false;

            if (queryParams.Any(qp => qp.Key.ToLower() == "followeruidiscurrentresourceuid"))
            {
                var followerCurrentResourceUid = queryParams.FirstOrDefault(x => x.Key.ToLower() == "followeruidiscurrentresourceuid").Value;
                if (followerCurrentResourceUid.ToLower() == "true")
                {
                    followerCurrResUidPresent = true;
                }
            }

            int CommentTypeID = 0;
            bool CommentTypeIDPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "commenttypeid"))
            {
                var CommentTypeIDValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "commenttypeid").Value;
                CommentTypeIDPresent = int.TryParse(CommentTypeIDValue, out CommentTypeID);
            }

            bool IsShowDeleteComment = true;
            bool DeletedCommentPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "showdeletecomment"))
            {
                var ShowDeleteCommentValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "showdeletecomment").Value;
                DeletedCommentPresent = bool.TryParse(ShowDeleteCommentValue, out IsShowDeleteComment);
            }

            int Days = 0;
            bool daysToLookBackPresent = false;
            if (queryParams.Any(qp => qp.Key.ToLower() == "daystolookback"))
            {
                var daysToLookBackValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "daystolookback").Value;
                daysToLookBackPresent = int.TryParse(daysToLookBackValue, out Days);
            }
            #endregion

            var orderColumn = CompanyContext.ParseOrderColumn(queryParams, queryFieldOptions, "C.CreatedOn");
            var orderDirection = CompanyContext.ParseOrderDirection(queryParams, "desc");
            var orderBySql = $" order by {orderColumn} {orderDirection} ";

            int pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            int pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(pageNum, pageSize);

            var baseCommentWheres = new List<string> { "C.ParentID is null" };

            #region "Ng additional Filter : Apply"
            if (CommentTypeIDPresent)
            {
                dbArgs.Add("@CommentTypeID", CommentTypeID);
                baseCommentWheres.Add(@"(C.CommentType = @CommentTypeID)");
            }

            if (DeletedCommentPresent)
            {
                baseCommentWheres.Add(@"(C.IsDeleted = 0)");
            }

            if (followerCurrResUidPresent)
            {
                baseCommentWheres.Add(@"(iif(C.CreatedBy = @currentUser, 1, 0) = 1)");
            }

            if (daysToLookBackPresent)
            {
                DateTime dateStart;
                DateTime dateEnd = DateTime.UtcNow;
                Days *= -1;
                if (Days == 0)
                {
                    dateStart = new DateTime(2000, 1, 1);
                }
                else
                {
                    dateStart = (Days < 0) ? dateEnd.AddDays(Days) : dateEnd.AddDays(-Days);
                }

                dbArgs.Add("@rangeStart", dateStart);
                dbArgs.Add("@rangeEnd", dateEnd);

                baseCommentWheres.Add(@"(C.CreatedOn between @rangeStart and @rangeEnd)");
            }
            #endregion

            if (assetUidPresent)
            {
                var asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();
                if (asset == null || !CompanyContext.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Asset with provided Uid does not exist.");
                }
                var assetType = CompanyContext.Filter<AssetType>(o => o.ID == asset.AssetTypeID).FirstOrDefault();

                if (!CompanyContext.CurrentResourceIsAdmin
                    && !CompanyContext.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
                {
                    throw new GenericException(System.Net.HttpStatusCode.Forbidden, "Invalid request", "You do not have permissions to read the specified asset type.");
                }

                dbArgs.Add("@assetId", asset.ID);
                baseCommentWheres.Add(@"( (C.AssetID = @assetId) or (C.ID in (select coalesce(ic.ParentID, ic.ID) from CommentRelation ir inner join Comment ic on ic.ID = ir.CommentID and ir.AssetID = @assetId)) )");
            }
            if (assetTypeUidPresent)
            {
                var assetType = CompanyContext.Filter<AssetType>(o => o.uid == assetTypeUid).FirstOrDefault();
                if (assetType == null)
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Asset Type with provided Uid does not exist.");
                }

                if (!CompanyContext.CurrentResourceIsAdmin
                    && !CompanyContext.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
                {
                    throw new GenericException(System.Net.HttpStatusCode.Forbidden, "Invalid request", "You do not have permissions to read the specified asset type.");
                }

                dbArgs.Add("@assetTypeId", assetType.ID);
                baseCommentWheres.Add(@"( 
					(C.ID in ( 
						     select ic.ID 
							 from	Comment ic 
									inner join Asset ia on ia.ID = ic.AssetID 
									inner join AssetType iat on iat.ID = ia.AssetTypeID and iat.ID = @assetTypeId
							 )
					) 
					or (C.ID in (
							select	coalesce(ic.ParentID, ic.ID) 
							from	CommentRelation ir 
									inner join Comment ic on ic.ID = ir.CommentID 
									inner join Asset ia on ia.ID = ir.AssetID 
									inner join AssetType iat on iat.ID = ia.AssetTypeID and iat.ID = @assetTypeId
							)
					) 
				)");
            }
            int followerresourceID = -1;
            if (followerUidPresent)
            {
                var follower = CompanyContext.Filter<GlobalReportingResource>(o => o.Uid == followerUid).FirstOrDefault();
                if (follower == null)
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "User with provided Uid does not exist.");
                }
                else
                {
                    followerresourceID = follower.ResourceID;
                }
            }
            else if (followerCurrResUidPresent)
            {
                followerresourceID = CompanyContext.CurrentResourceID;
            }

            if (followerresourceID > -1)
            {
                dbArgs.Add("@followerId", followerresourceID);

                baseCommentWheres.Add(@"(
(exists (select f.AssetID from FollowDetail f where f.ResourceID = @followerId and f.AssetID = C.AssetID  union all select r.AssetID from ResponsibilityDetail r where r.ResourceID = @followerId and r.AssetID = C.AssetID)) 
or (exists (select cp.ParentID from Comment cp where cp.ParentID is not null and cp.CreatedBy = @followerId and cp.ParentID = C.ID ))
or (C.ID in (select ID from Comment where CreatedBy = @followerId))
)");
            }

            dbArgs.Add("@currentUser", CompanyContext.CurrentResourceID);
            whereStatements.Add($@"O.ID not in (select AssetID from dbo.UserAssetPermissions(@currentUser,T.ID) where ((PermissionsBitMask & {(int)Permission.ReadAsset})) = 0)");
            whereStatements.Add(@"T.ID not in (select AssetTypeID from dbo.AssetTypesUserCantRead(@currentUser))");

            var cteSql = $@"
with P as (
	select		C.ID,
				C.ParentID,
				C.AssetID
	from		Comment C 
	where		{string.Join(" and ", baseCommentWheres)}
	union all
	select		C.ID,
				C.ParentID,
				P.AssetID
	from		Comment C
				inner join P on P.ID = C.ParentID
) ";

            var whereSql = (whereStatements.Count > 0) ? "where " + string.Join(" and ", whereStatements) : "";

            var tableSql = @"from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatedBy
		inner join P ON C.ID = P.ID
		inner join Asset O on O.ID = P.AssetID
		inner join AssetType T on T.ID = O.AssetTypeID
		inner join graph.AssetNodeDisplayPath AP on AP.ID = O.ID
		outer apply [dbo].[GetAssetUrlById](O.ID) AUrl ";

            var countWhereSql = whereSql + (string.IsNullOrEmpty(whereSql) ? "where " : " and ") + "C.ParentID is null";
            var countSql = $@"
{cteSql}
select	count(1) as [Count]
{tableSql} {countWhereSql}";

            var sql = $@"
{cteSql}
select	{COMMENT_TABLE_COLUMNS},
		O.Uid as AssetUid,
		T.Uid as AssetTypeUid,
		AUrl.Url as Url,
		AP.DisplayPath as AssetPath,
		R.FirstName + ' ' + R.LastName as ResourceName,
		{TAGS_JSON_SQL},
		{EMOJIS_JSON_SQL} 
{tableSql} {whereSql} {orderBySql} {offset}";

            var countRequest = await CompanyContext.QueryAsync<int>(countSql, dbArgs);

            var count = countRequest.Single();

            var request = await CompanyContext.QueryAsync<CommentDetail>(sql, dbArgs);
            var flatComments = request.ToList();
            var rootComments = flatComments.Where(c => !c.ParentID.HasValue);
            var returnedComments = new List<CommentDetail>();
            foreach (var commentDetail in rootComments)
            {
                loadCommentDetailDescendants(flatComments, commentDetail);
                returnedComments.Add(commentDetail);
            }
            return new CommentDetails
            {
                count = count,
                page = pageNum,
                pageSize = pageSize,
                comments = returnedComments
            };
        }

        public async Task<CommentDetail> GetCommentDetailByUid(Guid commentUid)
        {
            var sql = $@"
with P as (
	select		ID,
				ParentID,
				AssetID
	from		Comment
	where		Uid = @commentUid
	union all
	select		C.ID,
				C.ParentID,
				P.AssetID
	from		Comment C
				inner join P on P.ID = C.ParentID
)
select	{COMMENT_TABLE_COLUMNS},
		O.Uid as AssetUid,
		T.Uid as AssetTypeUid,
		AUrl.Url as Url,
		AP.DisplayPath as AssetPath,
		R.FirstName + ' ' + R.LastName as ResourceName,
		{TAGS_JSON_SQL},
		{EMOJIS_JSON_SQL} 
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatedBy 
		inner join P ON C.ID = P.ID
		inner join Asset O on O.ID = P.AssetID
		inner join AssetType T on T.ID = O.AssetTypeID
		inner join graph.AssetNodeDisplayPath AP on AP.ID = O.ID
		outer apply [dbo].[GetAssetUrlById](O.ID) AUrl
ORDER BY	C.ParentID, C.CreatedOn DESC";

            var request = await CompanyContext.QueryAsync<CommentDetail>(sql, new { commentUid });
            var flatComments = request.ToList();

            var commentDetail = flatComments.SingleOrDefault(c => c.Uid == commentUid);
            if (commentDetail != null)
            {
                loadCommentDetailDescendants(flatComments, commentDetail);
                return commentDetail;
            }
            else
            {
                throw new NotFoundException("comment");
            }
        }

        public async Task<List<CommentVoteDetail>> GetCommentVotesByCommentUid(Guid commentUid)
        {
            if (CompanyContext.Any<Comment>(c => c.Uid == commentUid))
            {
                var sql = $@"
select	V.Emoji as emoji, 
		R.Uid as resourceUid, 
		R.FirstName + ' ' + R.LastName as userDisplayName 
from	CommentVote V 
		inner join Comment C on C.ID = V.CommentID and C.Uid = @commentUid 
		inner join reporting.Global_Resource R on R.ResourceID = V.ResourceID 
order by V.Emoji";

                var request = await CompanyContext.QueryAsync<CommentVoteDetail>(sql, new { commentUid });
                return request.ToList();
            }
            else
            {
                throw new NotFoundException("comment");
            }
        }

        public async Task<List<CommentVoterDetail>> GetCommentVotersByCommentAndEmoji(Guid commentUid, Emoji emoji)
        {
            if (CompanyContext.Any<Comment>(c => c.Uid == commentUid))
            {
                var sql = $@"
select	R.Uid as resourceUid, 
		R.FirstName + ' ' + R.LastName as userDisplayName 
from	CommentVote V 
		inner join Comment C on C.ID = V.CommentID and C.Uid = @commentUid  and V.Emoji = @emoji
		inner join reporting.Global_Resource R on R.ResourceID = V.ResourceID 
order by V.Emoji";

                var request = await CompanyContext.QueryAsync<CommentVoterDetail>(sql, new { commentUid, emoji = (int)emoji });
                return request.ToList();
            }
            else
            {
                throw new NotFoundException("comment");
            }
        }

        private void loadCommentDetailDescendants(List<CommentDetail> list, CommentDetail p)
        {
            foreach (var c in list.Where(c => c.ParentID == p.ID).OrderByDescending(c => c.CreatedOn))
            {
                if (p.Comments == null)
                {
                    p.Comments = new List<CommentDetail>();
                }
                loadCommentDetailDescendants(list, c);
                p.Comments.Add(c);
            }
        }

        private void validateComment(IApiComment comment)
        {
            if (comment == null)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, "", "No content provided to create a comment with.");
            }
            if (string.IsNullOrEmpty(comment.Body))
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, "", "You must provide a value for the Body property.");
            }

            if (comment.Tags != null && comment.Tags.Count > 50)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, "", "You may not provide more than 50 tags on this comment.");
            }
        }

		private void SendCommentNotification(List<Asset> taggedAssets, Comment comment, long? commentedOnAssetId = null)
        {		
			if (taggedAssets.Any(a => a.Object == core.SystemObjects.Resource.ToString() || a.Object == core.SystemObjects.Group.ToString()))
			{
				var commentCreator = CompanyContext.Connection.Query<string>("Select GR.FirstName + ' ' + GR.LastName as ResourceName from reporting.Global_Resource GR where resourceId = @commentBy", new { commentBy = comment.CreatedBy }).FirstOrDefault();

				if (commentCreator != null)
				{
                    
                    var assetDetail = CompanyContext.Connection.Query<AssetDetail>("Select * from AssetDetail A where A.ID = @AssetID", new { AssetID = commentedOnAssetId ?? comment.AssetID }).FirstOrDefault();

                    if (assetDetail != null)
                    {                    
						string resourceSQL = $@"select distinct * from (Select 
                                                                        GR.*
                                                                    from 
	                                                                    CommentRelation CR 
	                                                                    inner join 
	                                                                    Asset A on A.ID = CR.AssetID 
	                                                                    inner join 
	                                                                    reporting.Global_Resource GR on A.ObjectID = GR.ResourceID 
                                                                    where 
	                                                                    CommentID = @commentID 
	                                                                    and 
	                                                                    A.Object = 'Resource'
                                                                    Union
                                                                    Select 
                                                                        GR.*
                                                                    from 
	                                                                    CommentRelation CR 
	                                                                    inner join 
	                                                                    Asset A on A.ID = CR.AssetID
	                                                                    inner Join 
	                                                                    ResourceGroup RG on A.ObjectID = RG.GroupID
	                                                                    inner join 
	                                                                    reporting.Global_Resource GR on RG.ResourceID = GR.ResourceID 
                                                                    where 
	                                                                    CommentID = @commentID 
	                                                                    and 
	                                                                    A.Object = 'Group') A";

					var resourcesToNotify = CompanyContext.Connection.Query<GlobalReportingResource>(resourceSQL, new { commentID = comment.ID }).ToList();

					CommentNotification notification = new CommentNotification {
						CommenterName = commentCreator,
						Subject = string.Format(Notifications.TaggedCommentMailSubject, commentCreator, assetDetail.DisplayValue),
						IsHtml = true,
                        CommentedOnAssetId = commentedOnAssetId
                    };

					resourcesToNotify.ForEach(r =>
					{
						notification.RecipientEmail = r.Email;
						notification.RecipientName = r.FullName;

						var commentUrl = $"/sidebar/comments/{assetDetail.uid}";
						var assetUrl = $"/asset/{assetDetail.uid}";

						if (!CompanyContext.HasUserReadPermission(assetDetail.Object, assetDetail.ObjectID, assetDetail.AssetTypeID, r.ResourceID))
						{
							commentUrl = assetUrl = $"/home";
						}

						notification.AssetUrl = assetUrl;
						notification.CommentUrl = commentUrl;

						CompanyContext.Connection.Execute("insert into [queue].[task]([Action], [Object], [ObjectID], [Custom]) values('Notify', 'TaggedComment', @id, @notification)", new { id = comment.ID, notification = JsonConvert.SerializeObject(notification) });
					});
				}
				}			
			}
		} 

	}
}
