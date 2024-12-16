using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.featureflags;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;

using Dapper;
using MoreLinq;
using Newtonsoft.Json;
using repositories;

namespace d360.model.DataAccessLayer
{
	public class TagRepository : BaseRepository, ITagRepository
	{
		internal IQueueSource Queue;

		public TagRepository(ICompanyContext company, ISecurityContextProvider securityContext, IFeatureFlagService ff, IQueueSource queue) : base(company, securityContext, ff)
		{
			Queue = queue;
		}

		public async Task<dynamic> GetTagsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			List<string> whereClauses = new List<string>();
			string sortField = "t.value";
			string sortOrder = "asc";
			string whereOperater = " and ";
			int useCount = 0;

			foreach (var qitem in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
			{
				switch (qitem.Key.ToLower())
				{
					case "_filter":
						{
							var value = qitem.Value;
							if (!string.IsNullOrEmpty(value))
							{
								var filterDataProvider = new FilterDataProvider(CompanyContext);
								var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.Tags);
								var sqlParams = new Dictionary<string, object>();
								whereClauses.Add(filterExpressionParser.Parse(value, out sqlParams, out _));
								foreach (var item in sqlParams)
								{
									dbArgs.Add(item.Key, item.Value);
								}
							}
						}
						break;
					case "globalsearch":
						dbArgs.Add("value", $"%{qitem.Value.ToLower()}%");
						List<string> simpleSearchWheres = new List<string>();
						simpleSearchWheres.Add("LOWER(t.Value) like @value");
						simpleSearchWheres.Add("STR(Tags.count) like @value");
						simpleSearchWheres.Add("concat(grc.FirstName, ' ', grc.LastName) like @value");

						whereClauses.Add($"({string.Join(" or ", simpleSearchWheres)})");

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
						if (qitem.Value.ToLower() == "createdon")
						{
							sortField = "t.CreatedOn";
						}
						if (qitem.Value.ToLower() == "createdby")
						{
							sortField = "grc.FirstName + ' ' +grc.LastName";
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

			return await CompanyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout);
		}

		public string CheckTagAssetbyUids(List<Guid> uids)
		{
			string retvalue = null;

			string sql = $@"
select top 1 lower(cast(t.uid as nvarchar(50))) uid
from tag t
inner join assettag att on t.id = att.TagID
where t.uid in @uids
";

			string taguid = CompanyContext.Query<string>(sql, new { uids} , ApiTimeout).FirstOrDefault();

			if (!string.IsNullOrEmpty(taguid))
			{
				retvalue = string.Format(Error.DeleteCascadeTagRelateAsset, taguid);
			}

			return retvalue;
		}

		public Tag GetTagByUid(Guid uid)
		{
			return CompanyContext.Tags.FirstOrDefault(x => x.uid == uid);
		}

		public Tag GetTagByName(string name)
		{
			return CompanyContext.Tags.FirstOrDefault(x => x.Value == name && x.State == State.Active);
		}

		public Tag GetTagById(int tagId)
		{
			return CompanyContext.Tags.FirstOrDefault(x => x.ID == tagId);
		}
		
		public bool DoesTagExists(Guid tagUid)
		{
			return CompanyContext.Tags.Any(x => x.uid == tagUid);
		}

		public bool DoesTagExists(string value, Guid? tagTypeUid)
		{
			var tagTypeId = GetTagTypeByUid(tagTypeUid).ID;
			return CompanyContext.Tags.Any(x => x.Value == value && x.TagTypeID == tagTypeId && x.State == State.Active);
		}

		public bool DoesAssetTagExists(int tagId, long assetId)
		{
			return CompanyContext.AssetTags.Any(x => x.TagID == tagId && x.AssetID == assetId);
		}

		public int? GetAssetTagDetails(int tagId, long assetId)
		{
			return CompanyContext.AssetTags.FirstOrDefault(x => x.TagID == tagId && x.AssetID == assetId).CreatedBy;
		}

		public AssetTag GetAssetTag(int tagId, long assetId)
		{
			return CompanyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
		}

		public IEnumerable<Tag> GetTagsForAsset(long assetId)
		{

			string sql = $@"
							select t.*
							from AssetTag atg
							inner join tag t on atg.TagID = t.ID
							where atg.assetid = @assetId
							";
			List<Tag> tags = CompanyContext.Query<Tag>(sql, new { assetId }, ApiTimeout).ToList();
			return tags;
		}

		public bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId)
		{
			bool hasPersmission = SecurityContext.IsAdministrator;
			
			if (!hasPersmission)
			{
				AssetTag tag = CompanyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
				
				if (tag != null)
				{
					hasPersmission = tag.CreatedBy ==  SecurityContext.ResourceID;
				}
			}

			if (!hasPersmission)
			{
				hasPersmission = CompanyContext.HasAssetPermission(assetId, Permission.EditAsset);
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
			
			if (SecurityContext.IsAdministrator ||  SecurityContext.ResourceID == tag.CreatedBy)
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
					case "_filter":
						{
							var value = param.Value;
							if (!string.IsNullOrEmpty(value))
							{
								var filterDataProvider = new FilterDataProvider(CompanyContext);
								var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.TagDetails);
								var sqlParams = new Dictionary<string, object>();
								whereClauses.Add(filterExpressionParser.Parse(value, out sqlParams, out _));
								foreach (var item in sqlParams)
								{
									dbArgs.Add(item.Key, item.Value);
								}
							}
						}
						break;
					case "displaypath":
						if (!hasGlobalSearch)
						{
							dbArgs.Add("displaypath", $"%{param.Value.ToLower()}%");
							whereClauses.Add("(node.DisplayPath like @displaypath)");
						}
						else
						{
							IsGlobalSearchException = true;
						}
						break;
					case "displayvalue":
						if (!hasGlobalSearch)
						{
							dbArgs.Add("displayvalue", $"%{param.Value.ToLower()}%");
							whereClauses.Add("(adv.DisplayValue like @displayvalue)");
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
							throw new ArgumentException(Error.InvalidPageSize, "_pagesize");
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
							throw new ArgumentNullException(Error.InvalidPageNumber);
						}

						break;
					case "sortby":

						if (param.Value.ToLower() == "displaypath" || param.Value.ToLower() == "displayvalue")
						{
							sortField = "node.displaypath";
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
						else if (param.Value.ToLower() == "createdon")
						{
							sortField = "AT.CreatedOn";
						}
						else if (param.Value.ToLower() == "addedby")
						{
							sortField = "grc.FirstName + ' ' +grc.LastName";
						}
						else
						{
							throw new ArgumentException(Error.InvalidSortBy);
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
							string[] allowedDirections = ["asc", "desc"];
							var order = param.Value;
							if (!allowedDirections.Contains(order.Trim().ToLower()))
							{
								throw new ArgumentException(Error.InvalidSortOrder);
							}
							sortOrder = allowedDirections.Contains(order.Trim().ToLower()) ? order : "asc";
						}

						break;
					case "_includetotal":
						if (!bool.TryParse(param.Value, out includeTotal))
						{
							throw new ArgumentException(string.Format(Error.Invalid_IncludeTotal, param.Value));
						}

						break;
				}
			}

			if (IsGlobalSearchException)
			{
				throw new ArgumentException(Error.Global_Search_InvalidParameter, "globalSearch");
			}

			string sortClause = $"ORDER BY {sortField} {sortOrder}";

			dbArgs.Add("tagUid", tagUid);
			string whereClause = $"WHERE T.uid = @tagUid";
			
			if (whereClauses.Count > 0)
			{
				whereClause += $" and ({string.Join(whereConnector, whereClauses)})";
			}

            var ctestring = $@"
                ;with cte as (
	                SELECT AssetID
                         , t.uid AS TagUid
                         , t.Value
                         , grc.uid CreatedByUid
                         , at.CreatedOn
                         , grc.FirstName AS CreatedByFirstName
                         , grc.LastName AS CreatedByLastName
                      FROM AssetTag at
                     INNER JOIN Tag t ON T.ID = at.TagID
                      LEFT JOIN reporting.Global_Resource grc ON at.CreatedBy = grc.ResourceID                          
                )
            ";

            var tagsCrossApply = @"
                CROSS APPLY (
                    SELECT Value
                         , TagUid AS uid
                         , CreatedByUid
                         , CreatedOn
                         , CreatedByFirstName
                         , CreatedByLastName 
                      FROM cte 
                     WHERE AssetId = A.Id 
                     ORDER BY Value 
                     FOR JSON PATH
                ) AssetTags (Tags)
            ";

			if (includeTotal)
			{
				var countSql = $@"{(addtagasstingfilter ? ctestring : "")} 
						select 
						count(1)
						from Tag T
							inner join AssetTag AT on AT.TagID = T.ID
							inner join Asset A ON A.ID = AT.AssetID
							inner join AssetPath Node on Node.id = a.id
							inner join AssetType AST ON AST.Id = A.AssetTypeId
							inner join AssetDisplayValue ADV on ADV.AssetID = A.ID
							{(addtagasstingfilter ? $" {tagsCrossApply} " : "")}
						{whereClause}";

				result.total = CompanyContext.Query<int>(countSql, dbArgs).FirstOrDefault();

			}

			var pagingSql = $"OFFSET {result.pageSize * (result.pageNum - 1)} ROWS FETCH NEXT {result.pageSize} ROWS ONLY";

			var sql = $@"{ctestring}  
						select 
						ADV.*, 
						node.DisplayPath,
						A.[Uid] as AssetUid,
						AST.[Uid] as AssetTypeUid,
						CASE 
							WHEN AST.Object = 'TaxonomyType' THEN '{Label.AssetTypeClass_Model.CleanForSql()} : '
							WHEN AST.Object = 'ArtifactType' and AST.[Class] = 1 THEN '{Label.AssetTypeClass_Business.CleanForSql()} : '
							WHEN AST.Object = 'ArtifactType' and AST.[Class] = 8 THEN '{Label.AssetTypeClass_Technical.CleanForSql()} : '
							WHEN AST.Object = 'PolicyType' THEN '{Label.AssetTypeClass_Policy.CleanForSql()} : '
							WHEN AST.Object = 'RuleType' THEN '{Label.AssetTypeClass_Rule.CleanForSql()} : '
							WHEN AST.Object = 'TaskType' THEN '{Label.AssetTypeClass_Task.CleanForSql()} : '
							ELSE ''
						END + AST.Name AS AssetType, 
						A.Object,
						A.ObjectID,
						AssetTags.Tags as Tags
						from Tag T
							inner join AssetTag AT on AT.TagID = T.ID
							inner join reporting.Global_Resource grc ON AT.CreatedBy = grc.ResourceID
							inner join Asset A ON A.ID = AT.AssetID
							inner join AssetPath Node on Node.id = a.id
							inner join AssetType AST ON AST.Id = A.AssetTypeId
							inner join AssetDisplayValue ADV on ADV.AssetID = A.ID
							{tagsCrossApply}
						{whereClause}
						{sortClause}
						{pagingSql}
						for json path";

			var data = string.Join("", CompanyContext.Query<string>(sql, dbArgs, ApiTimeout).ToList());

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
									inner join AssetDisplayValue ADV on ADV.AssetID = R.ID
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
								inner join AssetDisplayValue ADV on ADV.AssetID = R.ID
						where   T.[Uid] = @tagUid";
			}


			var result = CompanyContext.Query<dynamic>(sql, new { tagUid, assetUid }, ApiTimeout);
			
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

		public async Task BulkTagAssets(IEnumerable<BulkTagAsset> tags, int resourceId)
		{
			tags = tags.DistinctBy(x => new { x.AssetUid, x.Tag }).ToList();
			await CompanyContext.Connection.OpenIfClosed();

			await CompanyContext.Connection.ExecuteAsync(@"
					DROP TABLE IF EXISTS #bulkTags;
					CREATE TABLE #bulkTags
					(
						Id int identity not null,
						AssetUid uniqueidentifier not null,
						Tag nvarchar(max),
						[Action] nvarchar(20)
					)");


			DataTable table = new DataTable();
			table.Columns.Add("AssetUid", typeof(Guid));
			table.Columns.Add("Tag", typeof(string));
			table.Columns.Add("Action", typeof(string));

			foreach (var tag in tags)
			{
				DataRow row = table.NewRow();
				row["AssetUid"] = tag.AssetUid;
				row["Tag"] = tag.Tag == null ? DBNull.Value : tag.Tag;
				row["Action"] = tag.Action.ToString();

				table.Rows.Add(row);
			}


			using (SqlBulkCopy bulkCopy = new SqlBulkCopy(CompanyContext.Connection)
			{
				BatchSize = 5000,
				DestinationTableName = "#bulkTags",
				BulkCopyTimeout = ApiTimeout
			})
			{
				bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
				bulkCopy.ColumnMappings.Add("Tag", "Tag");
				bulkCopy.ColumnMappings.Add("Action", "Action");

				await bulkCopy.WriteToServerAsync(table);
			}


			await CompanyContext.Connection.ExecuteAsync(@"
					--add new tags
					declare @TagTypeId int;

					select @TagTypeId = COALESCE(max(ID),1)
					from TagType
					where uid = '00000001-0000-0000-0000-B00000000011';

					INSERT INTO Tag (Value,TagTypeId, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
					SELECT DISTINCT B.Tag as Value, 
									@TagTypeId,
									GETUTCDATE(), 
									@resourceId, 
									GETUTCDATE(), 
									@resourceId 
					FROM			#bulkTags B
					WHERE 			COALESCE(B.Tag, '') != '' 
									AND NOT EXISTS (SELECT 1 FROM Tag WHERE [State] = @activeState AND Value = B.Tag);

					--add missing tags to assets, applies to Append and Replace
					INSERT INTO AssetTag (AssetID, TagID, CreatedOn, CreatedBy)
					SELECT	A.ID, 
							T.ID, 
							GETUTCDATE(), 
							@resourceId
					FROM	#bulkTags B
							INNER JOIN Asset A on A.UID = B.AssetUid
							INNER JOIN  Tag T on T.[Value] = B.Tag AND T.[State] = @activeState
					WHERE	COALESCE(B.Tag, '') != '' 
							AND NOT EXISTS (SELECT 1 FROM AssetTag WHERE AssetID = A.ID AND TagID = T.ID)

					--remove existing tags for Replace only
					DELETE	TA
					FROM	AssetTag TA
							INNER JOIN Asset A on A.ID = TA.AssetID
							INNER JOIN Tag T on T.ID = TA.TagID
							INNER JOIN #bulkTags B on B.AssetUid = A.uid AND B.Action = 'Replace'
					WHERE	NOT EXISTS (SELECT B.Tag FROM #bulkTags B WHERE B.AssetUid = A.uid AND B.Tag = T.Value)
				", new { resourceId, activeState = State.Active });
		}

		public bool DoesTagTypeExists(Guid uid)
		{
			return CompanyContext.TagTypes.Any(x => x.uid == uid && x.State == State.Active);
		}

		public TagType GetTagTypeByUid(Guid? uid)
		{
			if (!uid.HasValue || uid == Guid.Empty)
			{
				uid = new Guid("00000001-0000-0000-0000-b00000000011");
			}
			return CompanyContext.TagTypes.FirstOrDefault(x => x.uid == uid);
		}
	}
}
