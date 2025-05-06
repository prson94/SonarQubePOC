using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.resources;
using Dapper;
using repositories.azure.extensions;

namespace repositories.azure
{
	public class Social : Repository, ISocial
	{
		public Social(DapperConnectionProvider provider) : base(provider)
		{
		}

		#region Common Sql

		private const string COMMENT_TABLE_COLUMNS = @"C.Uid, C.ID, C.ParentID, C.CommentType, iif(C.IsDeleted = 1, '[Comment removed]', C.Body) as Body, C.CreatedOn, C.CreatedBy, C.UpdatedBy, C.UpdatedOn, C.IsDeleted";
		private const string TAGS_JSON_SQL = @"coalesce(
			(
			select	AD.Uid as AssetUid,
					AD.AssetTypeUid,
					AP.DisplayPath as [Path],
					AD.TypeName,
					U.Url,
					AD.BackColor as IconBackColor,
					AD.ForeColor as IconForeColor
			from	CommentRelation CR
					inner join AssetDetail AD on AD.ID = CR.AssetID and CR.CommentID = C.ID
					inner join dbo.AssetPath AP on AD.ID = AP.ID
					cross apply GetAssetUrlById(AD.ID) U
			for		json path
			), '[]') as TagsJson";
		private const string EMOJIS_JSON_SQL = @"coalesce(
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
			using (var connection = ConnectionProvider.Connect(true))
			{
				connection.Open();
				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						validateComment(comment);

						long? commentedOnAssetId = null;
						int? parentId = null;
						long? assetId = null;
						Asset commentAsset = null;

						if (comment.ParentUid.HasValue && comment.ParentUid != Guid.Empty)
						{
							var sqlQuery = @"SELECT c.*, a.* FROM Comment c
												LEFT JOIN Asset a ON c.AssetId = a.Id
												WHERE c.Uid = @ParentUid";

							var parentComment = connection.Query<Comment, Asset, Comment>(
								sqlQuery,
								(comment, asset) =>
								{
									comment.Asset = asset;
									return comment;
								},
								new { ParentUid = comment.ParentUid.Value }, transaction,
								splitOn: "Id"
							).SingleOrDefault();

							if (parentComment == null)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.ParentCommentNotFound);
							}
							else
							{
								commentedOnAssetId = connection.Query<Asset>("SELECT ID FROM Asset WHERE Uid = @AssetUid", new { comment.AssetUid }, transaction).FirstOrDefault()?.ID;

								parentId = parentComment.ID;
								comment.AssetUid = parentComment.Asset.uid;
								assetId = parentComment.Asset.ID;

								commentAsset = parentComment.Asset;
							}
						}

						if (comment.AssetUid == Guid.Empty)
						{
							throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.BadRequest, Error.InvalidAssetUid);
						}

						if (!assetId.HasValue)
						{
							var assetQuery = @"
						SELECT a.*, t.*
						FROM Asset a
						JOIN AssetType t ON a.AssetTypeId = t.Id
						WHERE a.uid = @AssetUid";
							commentAsset = connection.Query<Asset, AssetType, Asset>(
								assetQuery,
								(asset, assetType) =>
								{
									asset.AssetType = assetType;
									return asset;
								},
								new { comment.AssetUid }, transaction,
								splitOn: "AssetTypeId"
							).FirstOrDefault();

							if (commentAsset == null)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.AssetUidNotFound);
							}

							if (!commentAsset.AssetType.Class.AsInfoModel().AllowCommentsOnAsset)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.RestrictedAssetUid);
							}
							assetId = commentAsset.ID;
						}

						if (commentAsset != null)
						{
							if (!HasAssetPermission(commentAsset.Object, commentAsset.ObjectID, Permission.ReadAsset))
							{
								throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.Forbidden, Error.CommentAddPermission);
							}
						}

						var dbComment = new Comment
						{
							CommentType = commentType,
							CreatedBy = CurrentUserId,
							CreatedOn = DateTime.UtcNow,
							IsDeleted = false,
							AssetID = assetId.Value,
							Body = comment.Body,
							ParentID = parentId,
							Uid = Guid.NewGuid(),
							UpdatedBy = CurrentUserId,
							UpdatedOn = DateTime.UtcNow
						};

						string query = "INSERT INTO Comment (CommentType, CreatedBy, CreatedOn, IsDeleted, AssetID, Body, ParentID, Uid, UpdatedBy, UpdatedOn) " +
								"VALUES (@CommentType, @CreatedBy , @CreatedOn, @IsDeleted, @AssetID,@Body,@ParentID,@Uid,@UpdatedBy,@UpdatedOn);SELECT CAST(SCOPE_IDENTITY() as int);";

						var commentAdded = connection.QuerySingle<int>(query, dbComment, transaction);

						if (commentAdded > 0)
						{
							var commentId = commentAdded;
							if (comment.Tags != null && comment.Tags.Count > 0)
							{
								var tags1 = comment.Tags;
								var query1 = "SELECT * FROM Asset WHERE uid IN @Tags";

								var taggedAssets = connection.Query<Asset>(query1, new { Tags = tags1 }, transaction).ToList();
								foreach (var r in taggedAssets)
								{
									string commentReln = "INSERT INTO CommentRelation (CommentID, AssetID) VALUES (@CommentID, @AssetID)";

									var parameters = new
									{
										CommentID = commentId,
										AssetID = r.ID
									};

									connection.Execute(commentReln, parameters, transaction);
								};
							}

							connection.Execute("delete C from CommentRelation C left join Asset A on A.ID = C.AssetID where C.CommentID = @commentId and A.ID is null", new { commentId }, transaction);
							transaction.Commit();

							return await GetCommentDetailByUid(dbComment.Uid).ConfigureAwait(false);
						}
						else
						{
							throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.InternalServerError, Error.UnableCreateComment);
						}
					}
					catch (Exception ex)
					{

						transaction.Rollback();
						throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.InternalServerError, Error.UnableCreateComment);
					}
				}
			}
		}
		public (int CommentId, Guid CommentUid, IDbTransaction Transaction) InsertComment(CommentApiPostModel comment, CommentType commentType = CommentType.Social)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				connection.Open();
				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						validateComment(comment);

						long? commentedOnAssetId = null;
						int? parentId = null;
						long? assetId = null;
						Asset commentAsset = null;

						if (comment.ParentUid.HasValue && comment.ParentUid != Guid.Empty)
						{
							var sqlQuery = @"SELECT c.*, a.* FROM Comment c
												LEFT JOIN Asset a ON c.AssetId = a.Id
												WHERE c.Uid = @ParentUid";

							var parentComment = connection.Query<Comment, Asset, Comment>(
								sqlQuery,
								(comment, asset) =>
								{
									comment.Asset = asset;
									return comment;
								},
								new { ParentUid = comment.ParentUid.Value }, transaction,
								splitOn: "Id"
							).SingleOrDefault();

							if (parentComment == null)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.ParentCommentNotFound);
							}
							else
							{
								commentedOnAssetId = connection.Query<Asset>("SELECT ID FROM Asset WHERE Uid = @AssetUid", new { comment.AssetUid }, transaction).FirstOrDefault()?.ID;

								parentId = parentComment.ID;
								comment.AssetUid = parentComment.Asset.uid;
								assetId = parentComment.Asset.ID;

								commentAsset = parentComment.Asset;
							}
						}

						if (comment.AssetUid == Guid.Empty)
						{
							throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.BadRequest, Error.InvalidAssetUid);
						}

						if (!assetId.HasValue)
						{
							var assetQuery = @"
						SELECT a.*, t.*
						FROM Asset a
						JOIN AssetType t ON a.AssetTypeId = t.Id
						WHERE a.uid = @AssetUid";
							commentAsset = connection.Query<Asset, AssetType, Asset>(
								assetQuery,
								(asset, assetType) =>
								{
									asset.AssetType = assetType;
									return asset;
								},
								new { comment.AssetUid }, transaction,
								splitOn: "AssetTypeId"
							).FirstOrDefault();

							if (commentAsset == null)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.AssetUidNotFound);
							}

							if (!commentAsset.AssetType.Class.AsInfoModel().AllowCommentsOnAsset)
							{
								throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.RestrictedAssetUid);
							}
							assetId = commentAsset.ID;
						}

						if (commentAsset != null)
						{
							if (!HasAssetPermission(commentAsset.Object, commentAsset.ObjectID, Permission.ReadAsset))
							{
								throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.Forbidden, Error.CommentAddPermission);
							}
						}

						// Define query with OUTPUT to return CommentId and CommentUid directly
						string query = @"
    INSERT INTO Comment (CommentType, CreatedBy, CreatedOn, IsDeleted, AssetID, Body, ParentID, Uid, UpdatedBy, UpdatedOn) 
    OUTPUT INSERTED.ID, INSERTED.Uid
    VALUES (@CommentType, @CreatedBy , @CreatedOn, @IsDeleted, @AssetID, @Body, @ParentID, @Uid, @UpdatedBy, @UpdatedOn);";

						var dbComment = new Comment
						{
							CommentType = commentType,
							CreatedBy = CurrentUserId,
							CreatedOn = DateTime.UtcNow,
							IsDeleted = false,
							AssetID = assetId.Value,
							Body = comment.Body,
							ParentID = parentId,
							Uid = Guid.NewGuid(),  // Generates a unique identifier before insertion
							UpdatedBy = CurrentUserId,
							UpdatedOn = DateTime.UtcNow
						};

						// Use a tuple to return both CommentId and CommentUid
						var commentAdded = connection.QuerySingle<(int CommentId, Guid CommentUid)>(query, dbComment, transaction);
						return (commentAdded.CommentId, commentAdded.CommentUid, transaction);

					}
					catch (Exception ex)
					{
						transaction.Rollback();
						throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.InternalServerError, Error.UnableCreateComment);
					}
				}
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
			using (var connection = ConnectionProvider.Connect(true))
			{
				var comment = connection.QuerySingleOrDefault<Comment>("SELECT * FROM Comment WHERE Uid = @commentUid", new { commentUid });

				if (comment != null)
				{
					string commentVoteQuery = "SELECT * FROM CommentVote WHERE CommentID = @commentId AND ResourceID = @resourceId AND Emoji IN @groupedEmojis";
					var commentVote = connection.QueryFirstOrDefault<CommentVote>(commentVoteQuery, new { commentId = comment.ID, resourceId, groupedEmojis });

					if (commentVote == null)
					{
						var newCommentVote = new
						{
							CommentID = comment.ID,
							ResourceID = resourceId,
							Emoji = (int)emoji
						};

						string insertQuery = "INSERT INTO CommentVote (CommentID, ResourceID, Emoji) VALUES (@CommentID, @ResourceID, @Emoji);SELECT CAST(SCOPE_IDENTITY() as int);";
						int newId = connection.QuerySingle<int>(insertQuery, newCommentVote);

						if (newId > 0)
						{
							return true;
						}
					}
					else if (commentVote.Emoji != emoji)
					{
						commentVote.Emoji = emoji;
						var updatedCommentVote = new
						{
							CommentID = comment.ID,
							ResourceID = resourceId,
							Emoji = (int)emoji
						};
						string updateQuery = "";

						updateQuery = $"UPDATE CommentVote SET Emoji = @emoji WHERE ID = @ID";
						connection.Open();
						using (var transaction = connection.BeginTransaction())
						{
							int count = connection.Execute(updateQuery, commentVote, transaction);
							transaction.Commit();
						}
						return true;
					}
					else if (toggle == true)
					{
						DeleteVote(commentUid, resourceId, emoji);
					}

					return false;
				}
				else
				{
					throw new NotFoundException(Error.comment);
				}
			}
		}

		public bool DeleteComment(Guid commentUid)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				connection.Open();

				using (var transaction = connection.BeginTransaction())
				{
					try
					{
						var sql = "SELECT * FROM Comment WHERE Uid = @CommentUid";
						var dbComment = connection.QuerySingleOrDefault<Comment>(sql, new { CommentUid = commentUid }, transaction);

						if (dbComment == null)
						{
							throw new StatusCodeException(System.Net.HttpStatusCode.NotFound);
						}

						if (dbComment.CreatedBy != CurrentUserId && !IsAdministrator)
						{
							throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.CommentUpdatePermissionAdmin, Error.CommentUpdatePermissionAdmin);
						}

						bool commentUpdated = false;
						var query = "SELECT COUNT(1) FROM Comment WHERE ParentID = @ParentID";
						var exists = connection.ExecuteScalar<bool>(query, new { ParentID = dbComment.ID }, transaction);

						if (exists)
						{
							dbComment.IsDeleted = true;
							dbComment.UpdatedBy = CurrentUserId;
							dbComment.UpdatedOn = DateTime.UtcNow;

							var sqlUpdate = @" UPDATE Comment SET IsDeleted = @IsDeleted, UpdatedBy = @UpdatedBy, UpdatedOn = @UpdatedOn WHERE Id = @ID";
							var affectedRows = connection.Execute(sqlUpdate, new { dbComment.IsDeleted, dbComment.UpdatedBy, dbComment.UpdatedOn, dbComment.ID }, transaction);
							commentUpdated = affectedRows > 0;
						}
						else
						{
							string checkCommentVote = @"SELECT 1 FROM CommentVote WHERE CommentId = @CommentId";

							var hasCommentVote = connection.QueryFirstOrDefault<int>(checkCommentVote, new { CommentId = dbComment.ID }, transaction);

							if (hasCommentVote > 0)
							{
								string deleteFromCommentVote = "DELETE FROM CommentVote WHERE CommentId = @CommentId";
								connection.Execute(deleteFromCommentVote, new { CommentId = dbComment.ID }, transaction);
							}

							var sqlDelete = "DELETE FROM Comment WHERE Id = @Id";
							var affectedDeleteRows = connection.Execute(sqlDelete, new { Id = dbComment.ID }, transaction);
							commentUpdated = affectedDeleteRows > 0;
							transaction.Commit();
						}

						if (commentUpdated)
						{
							return true;
						}
						else
						{
							throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.CommentNotRemoved);
						}
					}
					catch (Exception)
					{
						transaction.Rollback();
						connection.Close();
						throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.CommentNotRemoved);
					}
				}
			}
		}

		public bool DeleteVote(Guid commentUid, int resourceId, Emoji emoji)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				var sql = "SELECT * FROM Comment WHERE Uid = @CommentUid";
				var comment = connection.QuerySingleOrDefault<Comment>(sql, new { CommentUid = commentUid });

				if (comment != null)
				{
					var selectQuery = @"SELECT * FROM CommentVote WHERE CommentID = @CommentID AND ResourceID = @ResourceID AND Emoji = @Emoji";
					var commentVote = connection.QueryFirstOrDefault<CommentVote>(selectQuery, new
					{
						CommentID = comment.ID,
						ResourceID = resourceId,
						Emoji = emoji
					});

					if (commentVote != null)
					{
						var deleteQuery = "DELETE FROM CommentVote WHERE CommentID = @CommentID AND ResourceID = @ResourceID AND Emoji = @Emoji";
						var affectedRows = connection.Execute(deleteQuery, new
						{
							CommentID = commentVote.CommentID,
							ResourceID = commentVote.ResourceID,
							Emoji = commentVote.Emoji
						});

						var commentVoteDeleted = affectedRows > 0;

						if (commentVoteDeleted)
						{
							return true;
						}
					}
					return false;
				}
				else
				{
					throw new NotFoundException(Error.comment);
				}
			}
		}
		public async Task<CommentDetail> EditComment(Guid commentUid, CommentApiPutModel comment)
		{
			validateComment(comment);

			using (var connection = ConnectionProvider.Connect(true))
			{
				try
				{
					var selectQuery = "SELECT * FROM Comment WHERE Uid = @CommentUid";
					var dbComment = connection.QuerySingleOrDefault<Comment>(selectQuery, new { CommentUid = commentUid });

					if (dbComment == null)
					{
						throw new NotFoundException(Error.comment);
					}

					if (dbComment.CreatedBy != CurrentUserId)
					{
						throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.CommentUpdatePermission, Error.CommentUpdatePermission);
					}

					dbComment.Body = comment.Body;
					dbComment.UpdatedBy = CurrentUserId;
					dbComment.UpdatedOn = DateTime.UtcNow;

					var sql = @"UPDATE Comment SET Body = @Body,UpdatedBy = @UpdatedBy,UpdatedOn = @UpdatedOn WHERE Id = @Id";

					var affectedRows = connection.Execute(sql, new
					{
						Body = dbComment.Body,
						UpdatedBy = dbComment.UpdatedBy,
						UpdatedOn = dbComment.UpdatedOn,
						Id = dbComment.ID
					});

					var commentUpdated = affectedRows > 0;

					if (commentUpdated)
					{
						var commentId = dbComment.ID;

						connection.Execute("delete CommentRelation where CommentID = @commentId", new { commentId });
						var taggedAssets = new List<Asset>();
						if (comment.Tags != null)
						{
							if (comment.Tags.Count > 0)
							{
								taggedAssets = connection.Query<Asset>("SELECT * FROM Asset WHERE Uid IN @Tags", new { Tags = comment.Tags }).ToList();

								foreach (var r in taggedAssets)
								{
									var insertCommentRelation = @"INSERT INTO CommentRelation (CommentID, AssetID) VALUES (@CommentID, @AssetID)";
									var relationParams = new { CommentID = commentId, AssetID = r.ID };
									connection.Execute(insertCommentRelation, relationParams);
								}
							}
							connection.Execute("delete C from CommentRelation C left join Asset A on A.id = C.Assetid where C.CommentID = @commentId and A.ID is null", new { commentId });
						}
						var detail = await GetCommentDetailByUid(dbComment.Uid).ConfigureAwait(false);
						detail.TaggedAssets = taggedAssets;
						return detail;
					}
					else
					{
						throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.InternalServerError, Error.CommentNotUpdated);
					}
				}
				catch (Exception)
				{
					throw new GenericException(System.Net.HttpStatusCode.InternalServerError, Error.InternalServerError, Error.CommentNotUpdated);
				}
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
			using (var connection = ConnectionProvider.Connect(true))
			{
				var request = await connection.QueryAsync<CommentCount>(sql, new { resourceId, searchPhrase, rangeStart, rangeEnd });
				var counts = request.ToList();

				return counts;
			}
		}

		public async Task<CommentDetails> GetCommentDetails(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var count = 0;
			var returnedComments = new List<CommentDetail>();
			List<string> whereStatements = new List<string>();
			var queryFieldOptions = new List<FilterColumnOption>
			{
				new FilterColumnOption("Body", "C.Body", SqlFieldType.Text),
				new FilterColumnOption("Uid", "C.Uid", SqlFieldType.Guid),
				new FilterColumnOption("CreatedOn", "C.CreatedOn", SqlFieldType.DateTime),
				new FilterColumnOption("UpdatedOn", "C.UpdatedOn", SqlFieldType.DateTime),
				new FilterColumnOption("Url", "AUrl.Url", SqlFieldType.Text),
				new FilterColumnOption("AssetPath", "AP.DisplayPath", SqlFieldType.Text),
				new FilterColumnOption("ResourceName", "R.FirstName + ' ' + R.LastName", SqlFieldType.Text)
			};

			var validOrderFields = new List<SortColumnOption> {
				new SortColumnOption("Body", "C.Body"),
				new SortColumnOption("Uid", "C.Uid"),
				new SortColumnOption("CreatedOn", "C.CreatedOn"),
				new SortColumnOption("UpdatedOn","C.UpdatedOn"),
				new SortColumnOption("Url",  "AUrl.Url"),
				new SortColumnOption("AssetPath", "AP.DisplayPath"),
				new SortColumnOption("ResourceName", "R.FirstName + ' ' + R.LastName")
			};

			string sortColumn = queryParams.CheckForSortColumn(
				[
					new SortColumnOption("baseType", "S.BaseType"),
					new SortColumnOption("description", "S.Description"),
					new SortColumnOption("effectiveDate", "S.EffectiveDate"),
					new SortColumnOption("headerRegExpConfidence", "S.HeaderFilterConfidence"),
					new SortColumnOption("matchType", "S.MatchType"),
					new SortColumnOption("maximum", "S.Maximum"),
					new SortColumnOption("minimum", "S.Minimum"),
					new SortColumnOption("minSamples", "S.MinimumSamples"),
					new SortColumnOption("minMaxPresent", "S.MinMaxPresent"),
					new SortColumnOption("name", "S.Name"),
					new SortColumnOption("priority", "S.Priority"),
					new SortColumnOption("qualifier", "S.Qualifier"),
					new SortColumnOption("status", "StatusString"),
					new SortColumnOption("threshold", "S.Threshold"),
					new SortColumnOption("isDisabled", "case when S.EffectiveDate < S.UpdatedOn then 1 else 0 end")
				], "S.Qualifier");

			var advancefilters =  queryParams.ParseODataFilters();
			var (dbArgs, wheres) = advancefilters.ConvertToSqlFilters(queryFieldOptions);
			
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

			var orderColumn = queryParams.CheckForSortColumn(validOrderFields, "C.CreatedOn");
			string orderDirection = queryParams.CheckForSortDirection();
			var orderBySql = $" order by {orderColumn} {orderDirection} ";

			// Parse page size and offset and load into arguments for SQL.
			int pageNumber = queryParams.CheckForPageNumber();
			int pageSize = queryParams.CheckForPageSize();
			dbArgs.LoadOffsetDatabaseParameter(pageNumber, pageSize);
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
				using (var connection = ConnectionProvider.Connect(true))
				{
					var query = @"
			SELECT a.*, at.*
			FROM Asset a
			INNER JOIN AssetType at ON a.AssetTypeId = at.Id
			WHERE a.uid = @AssetUid";

					var asset = connection.Query<Asset, AssetType, Asset>(
						query,
						(asset, assetType) =>
						{
							asset.AssetType = assetType;
							return asset;
						},
						new { AssetUid = assetUid }).FirstOrDefault();

					if (asset == null)
					{
						throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.AssetUidNotFound);
					}

					if (
						!HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset) &&
						!HasAssetTypePermission(asset.AssetType.Object, asset.AssetType.ObjectID, Permission.ReadAsset)
						)
					{
						throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.InvalidRequestHttpErrorTitle, Error.RestrictReadAssettype);
					}

					dbArgs.Add("@assetId", asset.ID);
					baseCommentWheres.Add(@"( (C.AssetID = @assetId) or (C.ID in (select coalesce(ic.ParentID, ic.ID) from CommentRelation ir inner join Comment ic on ic.ID = ir.CommentID and ir.AssetID = @assetId)) )");


					if (assetTypeUidPresent)
					{
						var assetType = connection.Query<AssetType>("SELECT * FROM AssetType WHERE uid = @assetTypeUid",
							new { assetTypeUid }).FirstOrDefault();

						if (assetType == null)
						{
							throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.AssetUidNotFound);
						}

						if (!HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
						{
							throw new GenericException(System.Net.HttpStatusCode.Forbidden, Error.InvalidRequestHttpErrorTitle, Error.RestrictReadAssettype);
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
						var selectQuery = @"SELECT TOP 1 * FROM GlobalReportingResources WHERE Uid = @FollowerUid";
						var parameters = new { FollowerUid = followerUid };
						var follower = connection.QueryFirstOrDefault<GlobalReportingResource>(selectQuery, parameters);

						if (follower == null)
						{
							throw new GenericException(System.Net.HttpStatusCode.NotFound, Error.NotFound, Error.UserUidNotFound);
						}
						else
						{
							followerresourceID = follower.ResourceID;
						}
					}
					else if (followerCurrResUidPresent)
					{
						followerresourceID = CurrentUserId;
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

					dbArgs.Add("@currentUser", CurrentUserId);
					whereStatements.Add($@"O.ID not in (select AssetID from dbo.UserAssetPermissions(@currentUser,T.ID) where (PermissionsBitMask & {(int)Permission.ReadAsset}) = 0 and AssetID is not null)");
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
							)";

					var whereSql = (whereStatements.Count > 0) ? "where " + string.Join(" and ", whereStatements) : "";

					var tableSql = @"from	Comment C
								inner join reporting.Global_Resource R on R.ResourceID = C.CreatedBy
								inner join P ON C.ID = P.ID
								inner join Asset O on O.ID = P.AssetID
								inner join AssetType T on T.ID = O.AssetTypeID
								inner join dbo.AssetPath AP on AP.ID = O.ID
								outer apply [dbo].[GetAssetUrlById](O.ID) AUrl";

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
							{tableSql} {whereSql} {orderBySql}";

					var countRequest = await connection.QueryAsync<int>(countSql, dbArgs);

					count = countRequest.Single();

					var request = await connection.QueryAsync<CommentDetail>(sql, dbArgs);
					var flatComments = request.ToList();
					var rootComments = flatComments.Where(c => !c.ParentID.HasValue);


					foreach (var commentDetail in rootComments)
					{
						loadCommentDetailDescendants(flatComments, commentDetail);
						returnedComments.Add(commentDetail);
					}
				}
			}

			return new CommentDetails
			{
				count = count,
				page = pageNumber,
				pageSize = pageSize,
				comments = returnedComments
			};
		}

		public async Task<CommentDetail> GetCommentDetailByUid(Guid commentUid)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				var sql = $@"
						with P as (
							select		ID,
										ParentID,
										AssetID,
										CreatedBy
							from		Comment
							where		Uid = @commentUid
							union all
							select		C.ID,
										C.ParentID,
										P.AssetID,
										C.CreatedBy
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
								inner join dbo.AssetPath AP on AP.ID = O.ID
								outer apply [dbo].[GetAssetUrlById](O.ID) AUrl
						ORDER BY	C.ParentID, C.CreatedOn DESC";

				var request = await connection.QueryAsync<CommentDetail>(sql, new { commentUid });

				var flatComments = request.ToList();
				var commentDetail = flatComments.SingleOrDefault(c => c.Uid == commentUid);

				if (commentDetail != null)
				{
					loadCommentDetailDescendants(flatComments, commentDetail);
					return commentDetail;
				}
				else
				{
					throw new NotFoundException(Error.comment);
				}
			}
		}

		public async Task<List<CommentVoteDetail>> GetCommentVotesByCommentUid(Guid commentUid)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				string query = "SELECT COUNT(1) FROM Comment WHERE Uid = @Uid";
				var count = await connection.ExecuteScalarAsync<int>(query, new { Uid = commentUid });

				if (count > 0)
				{
					var sql = $@"
							select	V.Emoji as emoji, 
									R.Uid as resourceUid, 
									R.FirstName + ' ' + R.LastName as userDisplayName 
							from	CommentVote V 
									inner join Comment C on C.ID = V.CommentID and C.Uid = @commentUid 
									inner join reporting.Global_Resource R on R.ResourceID = V.ResourceID 
							order by V.Emoji";

					var request = await connection.QueryAsync<CommentVoteDetail>(sql, new { commentUid });
					return request.ToList();
				}
				else
				{
					throw new NotFoundException(Error.comment);
				}
			}
		}

		public async Task<List<CommentVoterDetail>> GetCommentVotersByCommentAndEmoji(Guid commentUid, Emoji emoji)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				string query = "SELECT COUNT(1) FROM Comment WHERE Uid = @Uid";

				var count = await connection.ExecuteScalarAsync<int>(query, new { Uid = commentUid });

				if (count > 0)
				{
					var sql = $@"
							select	R.Uid as resourceUid, 
									R.FirstName + ' ' + R.LastName as userDisplayName 
							from	CommentVote V 
									inner join Comment C on C.ID = V.CommentID and C.Uid = @commentUid  and V.Emoji = @emoji
									inner join reporting.Global_Resource R on R.ResourceID = V.ResourceID 
							order by V.Emoji";

					var request = await connection.QueryAsync<CommentVoterDetail>(sql, new { commentUid, emoji = (int)emoji });
					return request.ToList();
				}
				else
				{
					throw new NotFoundException(Error.comment);
				}
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
				throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.BadRequest, Error.NoContentProvided);
			}
			if (string.IsNullOrEmpty(comment.Body))
			{
				throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.BadRequest, Error.BodyNotEmpty);
			}

			if (comment.Tags != null && comment.Tags.Count > 50)
			{
				throw new GenericException(System.Net.HttpStatusCode.BadRequest, Error.BadRequest, Error.CommentTagMaxLimit);
			}
		}

		public bool ProcessWithQueue(List<Asset> taggedAssets, CommentApiPutModel comment)
		{
			if (taggedAssets.Any(a => a.Object == SystemObjects.Resource.ToString() || a.Object == SystemObjects.Group.ToString()))
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					var commentCreator = connection.Query<string>("Select GR.FirstName + ' ' + GR.LastName as ResourceName from reporting.Global_Resource GR where resourceId = @commentBy", new { commentBy = comment.CreatedBy }).FirstOrDefault();
					if (commentCreator != null)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				return false;
			}
		}
	}
}
		