using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.model.DataAccessLayer.repositories;

using Dapper;

using Newtonsoft.Json;

namespace d360.model.DataAccessLayer
{
	public class TagRepository : BaseRepository, ITagRepository
	{
		private readonly ICompanyContext companyContext;
		private readonly ICommunityContext communityContext;
		public TagRepository(ICompanyContext company, ICommunityContext community)
			: base(company)
		{
			companyContext = company;
			communityContext = community;
		}

		public bool DeleteTags(List<TagApiDeleteModel> model)
		{
			IEnumerable<Guid> tagUids = model.Select(m => m.uid);

			List<Tag> tagsToDelete = companyContext.Tags.Where(x => tagUids.Contains(x.uid)).ToList();

			foreach (var item in model)
			{
				DeleteTag(item.uid, item.cascade, ref tagsToDelete);
			}

			var result = companyContext.SaveChanges() > 0;
			if (result)
			{
				AddTagAudit(tagsToDelete, "Delete");
			}

			return result;
		}

		private void DeleteTag(Guid uid, bool cascade, ref List<Tag> tagsToDelete)
		{
			var model = tagsToDelete.FirstOrDefault(i => i.uid == uid);

			if (model == null && model.State != State.Deleted)
			{
				throw new ArgumentNullException(string.Format(TagErrors.TagUidNotExists, uid.ToString()));
			}

			var anyAssetTagsForDeletion = companyContext.AssetTags.Any(x => x.TagID == model.ID);

			if (anyAssetTagsForDeletion && !cascade)
			{
				throw new ArgumentNullException(string.Format(TagErrors.DeleteCascadeTagRelateAsset, uid.ToString()));
			}

			model.State = State.Deleted;

			companyContext.Query<int>($@"
										INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
											select  distinct
													'Update', 'Tag', 0, [queue].WriteIndexXml('Update', A.Object, A.ObjectID, coalesce(@r, 0))
											from    AssetTag T
													inner join Asset A on A.ID = T.AssetID and T.TagID = @t;

										delete AssetTag where TagID = @t;", new { r = companyContext.CurrentResourceID, t = model.ID }).FirstOrDefault();
		}

		public async Task<TagApiModelWrapper> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			TagApiModelWrapper results = new TagApiModelWrapper();
			int pageSize = 250;
			int pageNum = 0;

			bool disablePaging = false;
			string searchPhrase = "";
			string orderByField = "Value";
			string direction = "ASC";
			List<string> validOrderFields = new List<string> { "uid", "value", "usecount", "createdon", "createdbyuid", "updatedon", "updatedbyuid" };
			bool includeTotal = true;
			var dbArgs = new DynamicParameters();

			var sql = @"select t.uid,
							t.Value,
							Tags.count as UseCount,
							t.CreatedOn,
							grc.uid as CreatedByUid,
							t.UpdatedOn,
							gru.uid as UpdatedByUid
						 from [tag] t
							left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
							left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID
							cross apply (select count(*) from AssetTag where TagId = t.Id)Tags (count)";

			var countSql = @"select count(1)
							from[tag] t
								left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
								left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID";


			List<string> queryFilters = new List<string>();

			dbArgs.Add("@state", State.Active);
			queryFilters.Add($"t.[state] = @state");

			#region QueryParams

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
			{
				Guid uid = new Guid();

				var tagUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
				if (Guid.TryParse(tagUidString, out uid))
				{
					dbArgs.Add("@uid", uid);
					queryFilters.Add($"t.[UID] = @uid");
				}

				if (uid == null || uid == Guid.Empty)
				{

					throw new ArgumentException(string.Format(TagErrors.InvalidTagUid, tagUidString), "uid");
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "id"))
			{
				int id = int.MinValue;
				var tagIdString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "id").Value;
				if (int.TryParse(tagIdString, out id))
				{
					dbArgs.Add("@id", id);
					queryFilters.Add($"t.[Id] = @id");
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagesize"))
			{

				if (int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value, out pageSize))
				{
					if (pageSize < 1)
					{
						pageSize = 1;
					}
				}
				if (pageSize > 250)
				{
					pageSize = 250; // max page size is 250 people.
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagenum"))
			{
				if (int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value, out pageNum))
				{
					if (pageNum < 1)
					{
						pageNum = 1;
					}
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "getall"))
			{
				bool.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "getall").Value, out disablePaging);
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_tag"))
			{
				searchPhrase = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_tag").Value.Trim();
				if (!string.IsNullOrEmpty(searchPhrase))
				{
					dbArgs.Add("@searchPhrase", $"%{searchPhrase}%");
					queryFilters.Add($"t.[Value] like @searchPhrase");
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_order"))
			{
				var orderByFieldVal = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value.Trim().ToLower();
				
				if (validOrderFields.Contains(orderByFieldVal))
				{
					orderByField = orderByFieldVal;
				}
				else
				{
					throw new ArgumentException(string.Format(TagErrors.InvalidOrderBy, orderByFieldVal), "_order");
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_direction"))
			{
				var directionValue = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_direction").Value.Trim();
				string[] allowedDirections = new string[] { "asc", "desc" };
				
				if (!allowedDirections.Contains(directionValue.Trim().ToLower()))
				{
					throw new ArgumentException(string.Format(TagErrors.InvalidDirection, directionValue), "_direction");
				}
				else
				{
					direction = allowedDirections.Contains(directionValue.Trim().ToLower()) ? directionValue : "asc";
				}
			}

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_includetotal"))
			{
				var totalValue = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value.Trim();

				if (!bool.TryParse(totalValue, out includeTotal))
				{
					throw new ArgumentException(string.Format(TagErrors.InvalueTotalValue, totalValue), "_includetotal");
				}
			}

			if (queryFilters.Count > 0)
			{
				sql += " where " + string.Join(" and ", queryFilters);
				countSql += " where " + string.Join(" and ", queryFilters);
			}
			#endregion


			sql += $" order by [{orderByField}] {direction}";

			if (pageSize < 1)
			{
				pageSize = 1;
			}

			if (pageNum < 1)
			{
				pageNum = 1;
			}

			if (!disablePaging)
			{
				sql += $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
			}

			results.pageNum = pageNum;
			results.pageSize = pageSize;

			if (includeTotal)
			{
				results.total = (await companyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout)).FirstOrDefault();
			}
			else
			{
				results.total = null;
			}

			results.items = (await companyContext.QueryAsync<TagApiModel>(sql, dbArgs, ApiTimeout));

			return results;
		}

		public async Task<dynamic> GetTagsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{

			var dbArgs = new DynamicParameters();
			List<string> whereClauses = new List<string>();
			string sortField = "";
			string sortOrder = "";
			string whereOperater = " and ";
			int useCount = 0;

			foreach (var qitem in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
			{
				switch (qitem.Key.ToLower())
				{
					case "globalsearch":
						dbArgs.Add("value", $"%{qitem.Value.ToLower()}%");
						whereClauses.Add("LOWER(t.Value) like @value");
						whereClauses.Add("STR(Tags.count) like @value");

						whereOperater = " or ";

						break;
					case "value":
						dbArgs.Add("value", $"%{qitem.Value.ToLower()}%");
						whereClauses.Add("LOWER(t.Value) like @value");

						break;
					case "usecount":
						if (int.TryParse(qitem.Value, out useCount))
						{
							dbArgs.Add("useCount", $"%{qitem.Value.ToLower()}%");
							whereClauses.Add("STR(Tags.count) like @useCount");
						}

						break;
					case "sortby":
						if (qitem.Value.ToLower() == "usecount")
						{
							sortField = "usecount";
						}
						if (qitem.Value.ToLower() == "value")
						{
							sortField = "t.value";
						}
						break;
					case "sortorder":
						int val = int.Parse(qitem.Value);
						if (val >= 0)
						{
							sortOrder = "ASC";
						}
						else
						{
							sortOrder = "DESC";
						}
						break;
				}
			}

			string sortClause = $"ORDER BY {sortField} {sortOrder}";

			string whereClause = $"WHERE t.State = 1";

			if (whereClauses.Count > 0)
			{
				whereClause += $" and ({string.Join(whereOperater, whereClauses)})";
			}

			var sql = $@"select t.uid,
							t.Value,
							Tags.count as UseCount,
							t.CreatedOn,
							grc.FirstName + ' ' +grc.LastName as CreatedBy,
							t.UpdatedOn,
							gru.FirstName + ' ' +gru.LastName as UpdatedBy
						 from [tag] t
							left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
							left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID
							cross apply (select count(*) from AssetTag where TagId = t.Id)Tags (count)
						{whereClause}
						{sortClause}";

			var countSql = $@"select count(1)
							from[tag] t
								left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
								left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID
								cross apply (select count(*) from AssetTag where TagId = t.Id)Tags (count)
							{whereClause}";

			return await companyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout);
		}

		public TagApiModel CreateTag(TagApiUpsertModel model)
		{
			var result = new TagApiModel
			{
				Value = model.Value
			};

			var tag = new Tag { Value = model.Value };
			companyContext.Add(tag);
			AddTagAudit(tag, "Add");

			var user = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == companyContext.CurrentResourceID);

			result.uid = tag.uid;
			result.UpdatedOn = tag.UpdatedOn.GetValueOrDefault();
			result.UpdatedByUid = user.Uid;
			result.CreatedOn = tag.CreatedOn.GetValueOrDefault();
			result.CreatedByUid = user.Uid;

			return result;
		}

		public TagApiModel UpdateTag(Guid uid, TagApiUpsertModel model, Tag existingTag)
		{
			var result = new TagApiModel();
			existingTag.Value = model.Value;
			companyContext.Update(existingTag);

			result.Value = model.Value;
			result.uid = existingTag.uid;
			result.UpdatedOn = existingTag.UpdatedOn.GetValueOrDefault();
			result.CreatedOn = existingTag.CreatedOn.GetValueOrDefault();
			result.UseCount = companyContext.Query<int>
				("select count(*) from AssetTag where TagId =  @ID",
				new DynamicParameters(new { existingTag.ID })).FirstOrDefault();
			// Send To Queue.Task Table
			AddTagAudit(existingTag, "Update");

			companyContext.Query<int>($@"
										INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
											select  distinct
													'Update', 'Tag', 0, [queue].WriteIndexXml('Update', A.Object, A.ObjectID, coalesce(@r, 0))
											from    AssetTag T
													inner join Asset A on A.ID = T.AssetID and T.TagID = @t", 
													new { r = companyContext.CurrentResourceID, t = existingTag.ID }).FirstOrDefault();

			var createUser = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == existingTag.CreatedBy);
			
			if (createUser != null)
			{
				result.CreatedByUid = createUser.Uid;
			}
			
			var updateUser = companyContext.GlobalReportingResources.First(x => x.ResourceID == companyContext.CurrentResourceID);
			
			if (updateUser != null)
			{
				result.UpdatedByUid = updateUser.Uid;
			}

			return result;
		}

		public Tag GetTagByUid(Guid uid)
		{
			return companyContext.Tags.FirstOrDefault(x => x.uid == uid);
		}

		public Tag GetTagByName(string name)
		{
			return companyContext.Tags.FirstOrDefault(x => x.Value == name && x.State == State.Active);
		}

		public Tag GetTagById(int tagId)
		{
			return companyContext.Tags.FirstOrDefault(x => x.ID == tagId);
		}
		public bool DoesTagExists(Guid tagUid)
		{
			return companyContext.Tags.Any(x => x.uid == tagUid);
		}

		public bool DoesTagExists(string value)
		{
			return companyContext.Tags.Any(x => x.Value == value && x.State == State.Active);
		}

		public bool DoesTagExists(Guid existingTagUid, TagApiUpsertModel model)
		{
			return companyContext.Tags.Any(x => x.Value == model.Value && x.uid != existingTagUid && x.State == State.Active);
		}

		public List<AssetTagList> GetAssetsPathForTag(Guid tagUid)
		{
			string sql = @"select D.DisplayValue ,
						A.uid,
						AST.Object,
						A.ObjectID as AssetId,
						AST.ObjectID as AssetTypeId,
						AST.[Class],
						AST.Name
						from Tag T
					inner join AssetTag AT on AT.TagId = T.Id
					inner join Asset A on A.ID = AT.AssetID
					inner join AssetType AST ON AST.ID = A.AssetTypeId
					left join dbo.GetAssetDisplayValue() D on D.ID = A.ID
				where t.uid = @uid";

			var result = companyContext.Query<dynamic>(sql, new { uid = tagUid }, ApiTimeout).ToList();

			var ret = new List<AssetTagList>();
			foreach (var item in result)
			{
				var atl = new AssetTagList();
				ret.Add(atl);

				atl.DisplayName = item.DisplayValue;
				switch (item.Object.ToString())
				{
					case "ArtifactType":
						atl.Breadcrumbs = $"{(item.Class == 1 ? CommonNames.AssetTypeClass_Business : CommonNames.AssetTypeClass_Technical)} <i class=\"fa fa-chevron-right\"></i> " + item.Name;
						atl.Url = $"/artifact/{item.AssetTypeId}/{item.AssetId}";
						break;
					case "PolicyType":
						atl.Breadcrumbs = $"{CommonNames.AssetTypeClass_Policy} <i class=\"fa fa-chevron-right\"></i> " + item.Name;
						atl.Url = $"/policy/{item.AssetTypeId};hierarchyId={item.AssetId}";
						break;
					case "TaxonomyType":
						atl.Breadcrumbs = $"{CommonNames.AssetTypeClass_Model} <i class=\"fa fa-chevron-right\"></i> " + item.Name;
						atl.Url = $"/model/{item.AssetTypeId};hierarchyId={item.AssetId}";
						break;
					case "RuleType":
						atl.Breadcrumbs = $"{CommonNames.AssetTypeClass_Rule} <i class=\"fa fa-chevron-right\"></i> " + item.Name;
						atl.Url = $"/quality/rule/{item.AssetTypeId}/{item.AssetId}";
						break;
					case "TaskType":
						atl.Breadcrumbs = $"{CommonNames.AssetTypeClass_Task} <i class=\"fa fa-chevron-right\"></i> " + item.Name;
						atl.Url = companyContext.GetDiagramUrlForDiagramAsset(item.uid);
						break;
				}

			}

			return ret;
		}

		public IEnumerable<TagApiModel> ConsolidateTags(string parentUid, List<string> childrenUids)
		{
			StringBuilder sqlUidParams = new StringBuilder();
			foreach (var uidString in childrenUids)
			{
				sqlUidParams.Append($"insert into @children values ('{uidString}')");
			}
			string sql = $@"
						declare @children TABLE (uid uniqueidentifier)
						{sqlUidParams.ToString()}                        
						declare @consolidateToId int = (select TOP 1 Id from Tag where uid = @parentUid)
						
						declare @assetTagPending Table (ID int, uid uniqueidentifier, AssetID int, NewTagID int, OldTagID int, AlreadyExists bit);
						insert into @assetTagPending (ID,uid,AssetID, NewTagID, OldTagID, AlreadyExists)
							select 
								AT.ID, 
								AT.uid, 
								AT.AssetID, 
								@consolidateToId, 
								AT.TagID,
								ATE.ID
								from AssetTag AT
							inner join Tag T on AT.TagID = T.ID
							inner join @children CH on CH.uid = T.uid
							left join AssetTag ATE on ATE.AssetID = AT.AssetID AND ATE.TagID = @consolidateToId

						merge dbo.AssetTag t
						using @assetTagPending s
						on t.ID = S.ID and s.alreadyexists is null
						WHEN MATCHED
							THEN update set 
								t.tagid = s.newtagid;
						
						delete from AssetTag where id in (select id from @assetTagPending where AlreadyExists = 1)

						;WITH cte AS (
						  SELECT Id, AssetID, NewTagID, 
							 row_number() OVER(PARTITION BY assetid, newtagid ORDER BY id) AS [rn]
						  FROM @assetTagPending where AlreadyExists is null
						)DELETE assettag where id in (select Id from cte where rn > 1)



						update Tag 
						set State = 3
						where uid in (select uid from @children)

						;with ConsolidateData as 
						(
							select 
							T.Id as FromId,
							T.Value as FromValue,
							Target.ID as TargetId,
							Target.Value as TargetValue
							from Tag T
							cross apply (select top 1 * from Tag where uid = @parentUid)Target
							WHERE T.uid in (select uid from @children)
						)
						INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
							select  'TagConsolidated', 
									'Tag', 
									FromId,
									[queue].WriteIndexXml('', 'Tag', TargetId, coalesce(@resourceId, 0)) 
							from    ConsolidateData;

						INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
							select  distinct
									'Update', 'Tag', 0, [queue].WriteIndexXml('Update', A.Object,  A.ObjectID, coalesce(@resourceId, 0))
							from    @assetTagPending P
									inner join Asset A on A.ID = P.AssetID;
						
						select  T.uid, 
								Items.count as UseCount 
						from    Tag T
								cross apply (select count(*) from AssetTag where TagId = T.Id)Items (count)
						where   T.uid = @parentUid or T.uid in (select uid from @children);";


			var result = companyContext.Query<TagApiModel>(sql, new { parentUid, resourceId = companyContext.CurrentResourceID });
			
			return result;
		}

		public List<dynamic> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			string value = "";
			Guid exceptUid = Guid.Empty;
			int maxNumberOfResults = 200;
			bool ignoreCounts = false;
			foreach (var queryitem in queryParams)
			{
				switch (queryitem.Key.ToLower())
				{
					case "exceptuid":
						try
						{
							exceptUid = Guid.Parse(queryitem.Value);
						}
						catch
						{
							exceptUid = Guid.Empty;
						}
						break;
					case "ignorecounts":
						if (queryitem.Value.ToLower() == "true")
						{
							ignoreCounts = true;
						}
						break;
					case "value":
						value = $"%{queryitem.Value.ToLower()}%";
						break;
					case "maxnumberofresults":
						int size = 200;
						if (int.TryParse(queryitem.Value, out size))
						{
							maxNumberOfResults = size;
						}
						else
						{
							throw new ArgumentNullException(TagErrors.InvalidPageSize);
						}
						break;
				}
			}

			string sql;

			if (!ignoreCounts)
			{
				sql = $@"select top {maxNumberOfResults} T.Value as name, T.uid as code, Results.count from Tag T 
							cross apply (select count(*) from AssetTag where TagID = T.ID)Results(count)
							where State = 1 and T.Value like @value and T.uid != @exceptUid";
			}
			else
			{
				sql = $@"select top {maxNumberOfResults} T.Value as name, T.uid as code from Tag T 
						where State = 1 and T.Value like @value and T.uid != @exceptUid";
			}

			return companyContext.Query<dynamic>(sql, new { value, exceptUid }, ApiTimeout).ToList();
		}


		private void AddTagAudit(List<Tag> tags, string action)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var tag in tags)
			{
				sb.AppendLine(GetTagAuditInsertSql(tag, action));
			}

			companyContext.Query<int>(sb.ToString()).FirstOrDefault();
		}

		private void AddTagAudit(Tag tag, string action)
		{
			string sql = GetTagAuditInsertSql(tag, action);
			companyContext.Query<int>(sql).FirstOrDefault();
		}

		private string GetTagAuditInsertSql(Tag tag, string action)
		{
			return $@"INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
						 VALUES ('{action}','Tag',{tag.ID},[queue].WriteIndexXml('', 'Tag', {tag.ID}, coalesce({companyContext.CurrentResourceID}, 0)))";
		}

		private string GetAssetTagAuditInsertSql(long assetId)
		{
			var asset = companyContext.GetById<Asset>(assetId);
			return $@"INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
						 VALUES ('Update', 'Tag', 0, [queue].WriteIndexXml('Update', '{asset.Object}', {asset.ObjectID}, coalesce({companyContext.CurrentResourceID}, 0)))";
		}

		public bool DoesAssetTagExists(int tagId, long assetId)
		{
			return companyContext.AssetTags.Any(x => x.TagID == tagId && x.AssetID == assetId);
		}
		public int? GetAssetTagDetails(int tagId, long assetId)
		{
			return companyContext.AssetTags.FirstOrDefault(x => x.TagID == tagId && x.AssetID == assetId).CreatedBy;
		}

		public AssetTag CreateAssetTag(int tagId, long assetId)
		{
			if (DoesAssetTagExists(tagId, assetId))
			{
				return null;
			}

			var assetTag = new AssetTag()
			{
				TagID = tagId,
				AssetID = assetId
			};

			companyContext.Add(assetTag);

			// Send To Queue.Task Table
			companyContext.Query<int>(GetAssetTagAuditInsertSql(assetId), null).FirstOrDefault();

			return assetTag;
		}

		public AssetTag GetAssetTag(int tagId, long assetId)
		{
			return companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
		}

		public IEnumerable<Tag> GetTagsForAsset(long assetId)
		{
			var assetTags = companyContext.AssetTags.Where(x => x.AssetID == assetId).ToList();
			List<Tag> tags = new List<Tag>();
			assetTags.ForEach(x =>
			{
				var tag = companyContext.Tags.FirstOrDefault(y => y.ID == x.TagID);
				if (tag != null)
				{
					tags.Add(tag);
				}
			});
			return tags;
		}

		public bool DeleteAssetTag(int tagId, long assetId)
		{
			bool deleted = companyContext.Delete<AssetTag>(x => x.TagID == tagId && x.AssetID == assetId);

			if (deleted)
			{
				// Send To Queue.Task Table
				companyContext.Query<int>(GetAssetTagAuditInsertSql(assetId)).FirstOrDefault();
			}

			return deleted;
		}

		public bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId)
		{
			bool hasPersmission = companyContext.CurrentResourceIsAdmin;
			
			if (!hasPersmission)
			{
				AssetTag tag = companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
				
				if (tag != null)
				{
					hasPersmission = tag.CreatedBy == companyContext.CurrentResourceID;
				}
			}

			if (!hasPersmission)
			{
				hasPersmission = companyContext.HasAssetPermission(assetId, Permission.EditAsset);
			}
			return hasPersmission;
		}

		public bool IsAuthorizedToEditTag(Guid tagUid)
		{
			var tag = GetTagByUid(tagUid);
			
			if (tag == null)
			{
				return false;
			}
			
			if (companyContext.CurrentResourceIsAdmin || companyContext.CurrentResourceID == tag.CreatedBy)
			{
				return true;
			}
			return false;
		}

		public TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			TagDetailApiModel result = new TagDetailApiModel
			{
				pageNum = 1,
				pageSize = 200
			};

			var dbArgs = new DynamicParameters();
			string whereConnector = " and ";
			string sortField = "DisplayValue";
			string sortOrder = "ASC";
			bool includeTotal = false;
			bool addtagasstingfilter = false;
			bool IsGlobalSearchException = false;
			List<string> whereClauses = new List<string>();

			bool hasGlobalSearch = queryParams.Any(x => x.Key.ToLower() == "globalsearch" && !string.IsNullOrEmpty(x.Value));

			foreach (var param in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
			{
				switch (param.Key.ToLower())
				{
					case "displayvalue":
						if (!hasGlobalSearch)
						{
							dbArgs.Add("displayvalue", $"%{param.Value.ToLower()}%");
							whereClauses.Add("(ADV.DisplayValue like @displayvalue or node.DisplayPath like @displayValue)");
						}
						else
						{
							IsGlobalSearchException = true;
						}
						break;
					case "assettype":
						if (!hasGlobalSearch)
						{
							dbArgs.Add("assetname", $"%{param.Value.ToLower()}%");
							whereClauses.Add("AST.Name like @assetname");
							AddAssetTypeParam(dbArgs, whereClauses, param.Value);
						}
						else
						{
							IsGlobalSearchException = true;
						}
						break;
					case "tagsasstring":
						if (!hasGlobalSearch)
						{
							addtagasstingfilter = true;
							dbArgs.Add("tagsasstring", $"%{param.Value.ToLower()}%");
							whereClauses.Add("AssetTags.Tags like @tagsasstring");
						}
						else
						{
							IsGlobalSearchException = true;
						}
						break;
					case "globalsearch":
						addtagasstingfilter = true;
						dbArgs.Add("globalsearch", $"%{param.Value.ToLower()}%");
						whereClauses.Add(@"(AssetTags.Tags like @globalsearch OR
											ADV.DisplayValue like @globalsearch OR
											node.DisplayPath like @globalsearch OR
											AST.Name like @globalsearch)");

						AddAssetTypeParam(dbArgs, whereClauses, param.Value);

						break;
					case "assettypeuid":
						dbArgs.Add("assettypeuid", $"{param.Value.ToLower()}");
						whereClauses.Add("AST.uid = @assettypeuid");
						break;
					case "_pagesize":
						int size = 0;

						if (int.TryParse(param.Value, out size))
						{
							result.pageSize = int.Parse(param.Value);
						}
						else
						{
							throw new ArgumentException(TagErrors.InvalidPageSize, "_pagesize");
						}

						break;
					case "_pagenum":
						int num = 0;

						if (int.TryParse(param.Value, out num))
						{
							result.pageNum = int.Parse(param.Value);
							
							if (result.pageNum <= 0)
							{
								result.pageNum = 1;
							}
						}
						else
						{
							throw new ArgumentNullException(TagErrors.InvalidPageNumber);
						}

						break;
					case "sortby":

						if (param.Value.ToLower() == "displayvalue")
						{
							sortField = "displayvalue";
						}
						else if (param.Value.ToLower() == "assettype")
						{
							sortField = "assettype";
						}
						else if (param.Value.ToLower() == "tagsasstring")
						{
							sortField = "AssetTags.Tags";
						}
						else if (param.Value.ToLower() == "assetid")
						{
							sortField = "assetid";
						}
						else
						{
							throw new ArgumentException(TagErrors.InvalidSortBy);
						}

						break;
					case "sortorder":
						int sortordervalue = 0;
						bool sortorderIsInt = int.TryParse(param.Value, out sortordervalue);

						if (sortorderIsInt)
						{
							if (sortordervalue >= 0)
							{
								sortOrder = "asc";
							}
							else
							{
								sortOrder = "desc";
							}
						}
						else
						{
							string[] allowedDirections = new string[] { "asc", "desc" };
							var order = param.Value;
							if (!allowedDirections.Contains(order.Trim().ToLower()))
							{
								throw new ArgumentException(TagErrors.InvalidSortOrder);
							}
							sortOrder = allowedDirections.Contains(order.Trim().ToLower()) ? order : "asc";
						}

						break;
					case "_includetotal":
						if (!bool.TryParse(param.Value, out includeTotal))
						{
							throw new ArgumentException(string.Format(TagErrors.Invalid_IncludeTotal, param.Value));
						}

						break;
				}
			}

			if (IsGlobalSearchException)
			{
				throw new ArgumentException(TagErrors.InvalidParaMeter, "globalSearch");
			}

			string sortClause = $"ORDER BY {sortField} {sortOrder}";

			dbArgs.Add("tagUid", tagUid);
			string whereClause = $"WHERE T.uid = @tagUid";
			
			if (whereClauses.Count > 0)
			{
				whereClause += $" and ({string.Join(whereConnector, whereClauses)})";
			}

			var ctestring = $@";with cte as (
						select AssetID, T.uid as TagUid, T.Value from AssetTag AT

							inner join Tag T on T.ID = at.TagID
						)";

			if (includeTotal)
			{
				var countSql = $@"{(addtagasstingfilter ? ctestring : "")} 
						select 
						count(1)
						from Tag T
							inner join AssetTag AT on AT.TagID = T.ID
							inner join Asset A ON A.ID = AT.AssetID
							inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
							inner join AssetType AST ON AST.Id = A.AssetTypeId
							cross apply dbo.GetAssetDisplayValueById(A.ID)ADV
							{(addtagasstingfilter ? " cross apply (select Value,TagUid as uid from cte where AssetId = A.Id order by Value for json path)AssetTags(Tags) " : "")}
						{whereClause}";

				result.total = companyContext.Query<int>(countSql, dbArgs).FirstOrDefault();

			}

			var pagingSql = $"OFFSET {result.pageSize * (result.pageNum - 1)} ROWS FETCH NEXT {result.pageSize} ROWS ONLY";

			var sql = $@"{ctestring}  
						select 
						ADV.*, 
						node.DisplayPath,
						A.Id as AssetID,
						A.[Uid] as AssetUid,
						AST.[Uid] as AssetTypeUid,
						CASE 
							WHEN AST.Object = 'TaxonomyType' THEN '{CommonNames.AssetTypeClass_Model.CleanForSql()} : '
							WHEN AST.Object = 'ArtifactType' and AST.[Class] = 1 THEN '{CommonNames.AssetTypeClass_Business.CleanForSql()} : '
							WHEN AST.Object = 'ArtifactType' and AST.[Class] = 8 THEN '{CommonNames.AssetTypeClass_Technical.CleanForSql()} : '
							WHEN AST.Object = 'PolicyType' THEN '{CommonNames.AssetTypeClass_Policy.CleanForSql()} : '
							WHEN AST.Object = 'RuleType' THEN '{CommonNames.AssetTypeClass_Rule.CleanForSql()} : '
							WHEN AST.Object = 'TaskType' THEN '{CommonNames.AssetTypeClass_Task.CleanForSql()} : '
							ELSE ''
						END + AST.Name AS AssetType, 
						A.Object,
						A.ObjectID,
						AssetTags.Tags as Tags
						from Tag T
							inner join AssetTag AT on AT.TagID = T.ID
							inner join Asset A ON A.ID = AT.AssetID
							inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
							inner join AssetType AST ON AST.Id = A.AssetTypeId
							cross apply dbo.GetAssetDisplayValueById(A.ID)ADV
							cross apply (select Value,TagUid as uid from cte where AssetId = A.Id order by Value for json path)AssetTags(Tags)
						{whereClause}
						{sortClause}
						{pagingSql}
						for json path";

			var data = string.Join("", companyContext.Query<string>(sql, dbArgs, ApiTimeout).ToList());

			result.items = JsonConvert.DeserializeObject<List<TagDetail>>(data);

			if (result.items == null)
			{
				result.items = new List<TagDetail>();
			}

			return result;
		}

		public IEnumerable<dynamic> GetTooltip(Guid tagUid, Guid? assetUid)
		{
			string sql;

			if (assetUid.HasValue)
			{
				sql = @"select	T.Value, 
									TA.CreatedOn, 
									ADV.DisplayValue as CreatedBy,
									T.Uid as TagUid
							from	AssetTag TA
									inner join Tag T on T.ID = TA.TagID
									inner join Asset A on A.ID = TA.AssetID
									inner join Asset R on R.Object = 'Resource' and R.ObjectID = TA.CreatedBy
									cross apply dbo.GetAssetDisplayValueById(R.ID)ADV
							where	T.[Uid] = @tagUid 
									and A.[Uid] = @assetUid";
			}
			else
			{
				sql = @"select  T.Value, 
								T.CreatedOn, 
								ADV.DisplayValue as CreatedBy 
						from    Tag T 
								inner join Asset R on R.Object = 'Resource' and R.ObjectID = T.CreatedBy
								cross apply dbo.GetAssetDisplayValueById(R.ID)ADV
						where   T.[Uid] = @tagUid";
			}


			var result = companyContext.Query<dynamic>(sql, new { tagUid, assetUid }, ApiTimeout);
			
			return result;
		}

		private static void AddAssetTypeParam(DynamicParameters dbArgs, List<string> whereClauses, string value)
		{
			string paramValue = "";

			if ("model".Contains(value.ToLower()))
			{
				paramValue = "TaxonomyType";
			}

			if ("glossary".Contains(value.ToLower()))
			{
				paramValue = "ArtifactType";
			}

			if ("policy".Contains(value.ToLower()))
			{
				paramValue = "PolicyType";
			}

			if ("rule".Contains(value.ToLower()))
			{
				paramValue = "RuleType";
			}

			if (!string.IsNullOrEmpty(paramValue))
			{
				dbArgs.Add("assettype", paramValue);
				whereClauses.Add("AST.Object = @assettype");
			}
		}

	}
}
