using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
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

		const string COMMENT_DETAIL_SQL = @"
select	C.*,
		AUrl.Url as Url,
		AP.DisplayPath as AssetPath,
		R.FirstName + ' ' + R.LastName as ResourceName,
		coalesce(
			(
			select	CR.AssetUid as Uid,
					AD.DisplayPath as [Path],
					CRA.TypeName,
					U.Url,
					CRA.BackColor as IconBackColor,
					CRA.ForeColor as IconForeColor
			from	CommentRelation CR
					inner join AssetDetail CRA on CRA.Uid = CR.AssetUid and CR.CommentID = C.ID
					inner join graph.AssetNodeDisplayPath AD on AD.ID = CRA.ID
					cross apply GetAssetUrlById(CRA.ID) U
			for		json path
			), '[]') as TagsJson,
		coalesce(
			(
			select	count(ResourceID) as [Count],
					Emoji
			from	CommentVote
			where	CommentID = C.ID
			group by Emoji
			for		json path
			), '[]') as EmojisJson,
		cast(IIF(R.Uid = C.AssetUid, 1, 0) as bit) as CreatorIsOwner
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatedBy
		inner join P ON C.ID = P.ID
		inner join Asset O on O.Uid = P.AssetUid
		inner join graph.AssetNodeDisplayPath AP on AP.ID = O.ID
		outer apply [dbo].[GetAssetUrlById](O.ID) AUrl
ORDER BY	C.ParentID, C.CreatedOn DESC";

		#endregion

		public async Task<CommentDetail> AddComment(CommentApiPostModel comment)
		{
			validateComment(comment);

			int? parentId = null;
			if (comment.ParentUid.HasValue && comment.ParentUid != Guid.Empty)
			{
				var parentComment = CompanyContext.Filter<Comment>(o => o.Uid == comment.ParentUid.Value).SingleOrDefault();
				if (parentComment == null)
				{
					throw new NotFoundException("parent comment");
				}
				else 
				{
					parentId = parentComment.ID;
					comment.AssetUid = parentComment.AssetUid;
				}
			}

			if (comment.AssetUid == Guid.Empty)
			{
				throw new GenericException(System.Net.HttpStatusCode.BadRequest, "", "You must provide a valid Uid for the AssetUid property.");
			}

			var dbComment = new Comment
			{
				CommentType = CommentType.Social,
				CreatedBy = CompanyContext.CurrentResourceID,
				CreatedOn = DateTime.UtcNow,
				IsDeleted = false,
				AssetUid = comment.AssetUid,
				Body = comment.Body,
				ParentID = parentId,
				Uid = Guid.NewGuid(),
				UpdatedBy = CompanyContext.CurrentResourceID,
				UpdatedOn = DateTime.UtcNow
			};
			var commentAdded = CompanyContext.Add(dbComment);

			if (commentAdded)
			{
				var commentRelations = new List<CommentRelation>();
				var commentId = dbComment.ID;
				foreach (var r in comment.Tags)
				{
					CompanyContext.CommentRelations.Add(new CommentRelation { CommentID = commentId, AssetUid = r });
				}
				await CompanyContext.SaveChangesAsync();
				
				var parameters = new List<SqlParameter>() { new SqlParameter("@CommentID", commentId) };
				CompanyContext.ExecuteNonQueryCommand("delete C from CommentRelation C left join Asset A on A.Uid = C.AssetUid where C.CommentID = @CommentID and A.ID is null", parameters);

				return await GetCommentDetailByUid(dbComment.Uid);
			}
			else
			{
				throw new GenericException(System.Net.HttpStatusCode.InternalServerError, "", "Comment was not successfully created.");
			}
		}

		public bool AddVote(Guid commentUid, int resourceId, Emoji emoji)
		{
			var comment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();
			if (comment != null)
			{
				var commentVoteExists = CompanyContext.Any<CommentVote>(o => o.CommentID == comment.ID && o.ResourceID == resourceId);
				if (!commentVoteExists)
				{
					if (CompanyContext.Add(new CommentVote { CommentID = comment.ID, ResourceID = resourceId, Emoji = emoji }))
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

		public bool DeleteComment(Guid commentUid)
		{
			var dbComment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();

			if (dbComment == null)
			{
				throw new NotFoundException("comment");
			}

			if (dbComment.CreatedBy != CompanyContext.CurrentResourceID || !CompanyContext.CurrentResourceIsAdmin)
			{
				throw new UnauthorizedException("You are not the creator of this comment or administrator and may not update it.", "You are not the creator of this comment or administrator and may not update it.");
			}

			dbComment.IsDeleted = true;
			dbComment.UpdatedBy = CompanyContext.CurrentResourceID;
			dbComment.UpdatedOn = DateTime.UtcNow;

			var commentUpdated = CompanyContext.Update(dbComment);

			if (commentUpdated)
			{
				return true;
			}
			else
			{
				throw new GenericException(System.Net.HttpStatusCode.InternalServerError, "", "Comment was not successfully removed.");
			}
		}

		public bool DeleteVote(Guid commentUid, int resourceId, Emoji emoji)
		{
			var comment = CompanyContext.Filter<Comment>(o => o.Uid == commentUid).SingleOrDefault();
			if (comment != null)
			{
				var commentVoteExists = CompanyContext.Any<CommentVote>(o => o.CommentID == comment.ID && o.ResourceID == resourceId);
				if (commentVoteExists)
				{
					if (CompanyContext.Delete(new CommentVote { CommentID = comment.ID, ResourceID = resourceId, Emoji = emoji }))
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
				throw new UnauthorizedException("You are not the creator of this comment and may not update it.", "You are not the creator of this comment and may not update it.");
			}
			

			dbComment.Body = comment.Body;
			dbComment.UpdatedBy = CompanyContext.CurrentResourceID;
			dbComment.UpdatedOn = DateTime.UtcNow;

			var commentUpdated = CompanyContext.Update(dbComment);

			if (commentUpdated)
			{
				var commentId = dbComment.ID;

				var parameters = new List<SqlParameter>() { new SqlParameter("@CommentID", commentId) };
				CompanyContext.ExecuteNonQueryCommand("delete CommentRelation CommentID = @CommentID", parameters);

				var commentRelations = new List<CommentRelation>();
				foreach (var r in comment.Tags)
				{
					CompanyContext.CommentRelations.Add(new CommentRelation { CommentID = commentId, AssetUid = r });
				}
				await CompanyContext.SaveChangesAsync();

				CompanyContext.ExecuteNonQueryCommand("delete C from CommentRelation C left join Asset A on A.Uid = C.AssetUid where C.CommentID = @CommentID and A.ID is null", parameters);

				return await GetCommentDetailByUid(dbComment.Uid);
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
					from	Follow F
							inner join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID 
							inner join CommentRelation O on O.AssetUid = A.Uid
					where	F.ResourceID = @resourceId
					union all
					select	ID 
					from	Comment 
					where	CreatedBy = @resourceId
					union all
					select	O.ID 
					from	Comment O
							inner join Asset A on A.Uid = O.AssetUid
							inner join ResponsibilityDetail R on R.ResourceID = @resourceId and R.AssetID = A.ID
					)
			AND C.IsDeleted = 0
			AND (
					(C.DateCreated between @rangeStart and @rangeEnd and @rangeStart is not null and @rangeEnd is not null) or
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

		public async Task<CommentDetail> GetCommentDetailByUid(Guid commentUid)
        {
            var sql = $@"
	with P as (
		select		ID,
					ParentID,
					AssetUid
		from		Comment
		where		Uid = @commentUid
		union all
		select		C.ID,
					C.ParentID,
					P.AssetUid
		from		Comment C
					inner join P on P.ID = C.ParentID
	)
	{COMMENT_DETAIL_SQL}";

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

		private void loadCommentDetailDescendants(List<CommentDetail> list, CommentDetail p)
		{
			foreach (var c in list.Where(c => c.ParentID == p.ID))
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

		/*

        public IQueryable<CommentDetail> GetCommentDetail(int id)
        {
            var comments = (
                    from c in Database.SqlQuery<CommentDetail>("GetCommentDetailByID @id", new SqlParameter("id", id)).ToList()
                    join r in Community.Resources on c.CreatedBy equals r.ID
                    select new CommentDetail
                    {
                        Body = c.Body,
                        Comments = c.Comments,
                        CommentType = c.CommentType,
                        CreatedBy = c.CreatedBy,
                        CreatedOn = c.CreatedOn,
                        ID = c.ID,
                        AssetUid = c.AssetUid,
                        AssetPath = c.AssetPath,
                        Url = c.Url,
                        ParentID = c.ParentID,
                        ResourceEmail = r.Email,
                        ResourceName = r.FormatDisplayName(),
                        TagsXml = c.TagsXml,
                        VotesXml = c.VotesXml,
                        CreatorIsOwner = c.CreatorIsOwner,
                        UpdatedOn = c.UpdatedOn,
                        IsDeleted = c.IsDeleted,
                        IsEditable = (CurrentResourceID == c.CreatedBy),
                        IsDeletable = (CurrentResourceIsAdmin || CurrentResourceID == c.CreatedBy)
                    }
                   );

            return comments.AsQueryable();
        }

        public IQueryable<CommentDetail> GetCommentDetailsByFollower(int resourceID, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "")
        {

            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;
            if (daysToGet == 0)
            {
                dateStart = new DateTime(2000, 1, 1);
            }
            else
            {
                dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
            }

            if (searchPhrase == null)
                searchPhrase = "";

            var comments =
                Query<CommentDetail>("GetCommentDetailsByFollower @resourceID, @skip, @take, @dateStart, @dateEnd, @commentTypeID, @searchPhrase",
                new
                {
                    resourceID = resourceID,
                    skip = skip,
                    take = take,
                    dateStart = dateStart,
                    dateEnd = dateEnd,
                    commentTypeID = commentType,
                    searchPhrase = searchPhrase.Replace("'", "''").Replace("--", "")
                });

            foreach (CommentDetail cd in comments)
            {
                cd.IsEditable = (CurrentResourceID == cd.CreatedBy
                        && !Any<Comment>(c => c.ParentID == cd.ID)
                        && DateTime.UtcNow.Subtract(cd.CreatedOn).Duration() < TimeSpan.FromMinutes(5));
                cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

            }

            return comments.AsQueryable();

        }

        public IQueryable<CommentDetail> GetCommentDetailsByID(int id)
        {
            var comments =
                Query<CommentDetail>("GetCommentDetailByID @id",
                new
                {
                    id = id
                });
            foreach (CommentDetail cd in comments)
            {
                cd.IsEditable = (CurrentResourceID == cd.CreatedBy
                        && !Any<Comment>(c => c.ParentID == cd.ID)
                        && DateTime.UtcNow.Subtract(cd.CreatedOn).Duration() < TimeSpan.FromMinutes(5));
                cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

            }

            return comments.AsQueryable();
        }

		public IQueryable<CommentDetail> GetCommentDetailsByType(SystemObjects type, int id, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "")
		{

			DateTime dateStart;
			DateTime dateEnd = DateTime.UtcNow;
			if (daysToGet == 0)
			{
				dateStart = new DateTime(2000, 1, 1);
			}
			else
			{
				dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
			}

			if (searchPhrase == null)
				searchPhrase = "";

			var comments =
				Query<CommentDetail>("GetCommentDetailsByType @type, @id, @skip, @take, @dateStart, @dateEnd, @commentTypeID, @searchPhrase",
				new
				{
					type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
					id = id,
					skip = skip,
					take = take,
					dateStart = dateStart,
					dateEnd = dateEnd,
					commentTypeID = commentType,
					searchPhrase = searchPhrase.Replace("'", "''").Replace("--", "")
				}).ToList();
			foreach (CommentDetail cd in comments)
			{
				cd.IsEditable = (CurrentResourceID == cd.CreatedBy
						&& !Any<Comment>(c => c.ParentID == cd.ID)
						&& DateTime.UtcNow.Subtract(cd.CreatedOn).Duration() < TimeSpan.FromMinutes(5));
				cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

			}

			return comments.AsQueryable();
		}




		*/
	}
}
