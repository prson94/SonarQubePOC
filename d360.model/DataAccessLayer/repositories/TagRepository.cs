using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class TagRepository : ITagRepository
    {
        ICompanyContext companyContext;
        ICommunityContext communityContext;
        public TagRepository(ICompanyContext company, ICommunityContext community)
        {
            this.companyContext = company;
            this.communityContext = community;
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
                throw new Exception($"Tag with uid '{uid}' does not exists!");

            var assetTagsForDeletion = companyContext.AssetTags.Where(x => x.TagID == model.ID);

            if (assetTagsForDeletion.Count() > 0 && !cascade)
                throw new Exception($"Tag with uid '{uid}' have related assets. Use cascade='true' to delete this tag!");

            model.State = State.Deleted;
            companyContext.AssetTags.RemoveRange(assetTagsForDeletion);
        }

        public async Task<TagApiModelWrapper> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            TagApiModelWrapper results = new TagApiModelWrapper();
            int pageSize = 0;
            int pageNum = 0;

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


            if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
            {
                Guid uid = new Guid();

                var tagUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                if (Guid.TryParse(tagUidString, out uid))
                {
                    dbArgs.Add("@uid", uid);
                    queryFilters.Add($"t.[UID] = @uid");
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
                    if (pageSize < 1) pageSize = 1;
                }
                if (pageSize > 250) pageSize = 250; // max page size is 250 people.
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagenum"))
            {
                if (int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value, out pageNum))
                {
                    if (pageNum < 1) pageNum = 1;
                }
            }

            if (queryFilters.Count > 0)
            {
                sql += " where " + string.Join(" and ", queryFilters);
                countSql += " where " + string.Join(" and ", queryFilters);
            }

            sql += " order by [ID] ASC"; // admin screen will most likely order results however it sees fit

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;

                results.pageNum = pageNum;
                results.pageSize = pageSize;

                sql += $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";

            }

            results.total = (await companyContext.QueryAsync<int>(countSql, dbArgs)).FirstOrDefault();

            if (results.total > 0)
            {
                results.items = (await companyContext.QueryAsync<TagApiModel>(sql, dbArgs));
            }

            return results;
        }

        public async Task<dynamic> GetTagsWithResourceName(IEnumerable<KeyValuePair<string, string>> queryParams)
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
                        if (qitem.Value.ToLower() == "usecount") sortField = "usecount";
                        if (qitem.Value.ToLower() == "value") sortField = "t.value";
                        break;
                    case "sortorder":
                        int val = int.Parse(qitem.Value);
                        if (val >= 0) sortOrder = "ASC";
                        else sortOrder = "DESC";
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


            return await companyContext.QueryAsync<dynamic>(sql, dbArgs);

        }

        public TagApiModel CreateTag(TagApiModel model)
        {
            var tag = new Tag
            {
                Value = model.Value,
                UpdatedBy = companyContext.CurrentResourceID,
                CreatedBy = companyContext.CurrentResourceID,
                UpdatedOn = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            companyContext.Entry(tag).State = System.Data.Entity.EntityState.Added;

            companyContext.SaveChanges();
            AddTagAudit(tag, "Add");

            var user = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == companyContext.CurrentResourceID);

            model.uid = tag.uid;
            model.UpdatedOn = tag.UpdatedOn.GetValueOrDefault();
            model.UpdatedByUid = user.Uid;
            model.CreatedOn = tag.CreatedOn.GetValueOrDefault();
            model.CreatedByUid = user.Uid;

            return model;
        }

        public TagApiModel UpdateTag(Guid uid, TagApiModel model, Tag existingTag)
        {

            existingTag.Value = model.Value;
            existingTag.UpdatedBy = companyContext.CurrentResourceID;
            existingTag.UpdatedOn = DateTime.UtcNow;
            companyContext.Entry(existingTag).State = System.Data.Entity.EntityState.Modified;

            companyContext.SaveChanges();
            AddTagAudit(existingTag, "Update");
            var updateUser = companyContext.GlobalReportingResources.First(x => x.ResourceID == companyContext.CurrentResourceID);

            var createUser = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == existingTag.CreatedBy);

            model.UpdatedOn = existingTag.UpdatedOn.GetValueOrDefault();
            model.UpdatedByUid = updateUser.Uid;
            model.CreatedOn = existingTag.CreatedOn.GetValueOrDefault();

            if (createUser != null)
            {
                model.CreatedByUid = createUser.Uid;
            }

            return model;
        }

        public Tag GetTagByUid(Guid uid)
        {
            return companyContext.Tags.FirstOrDefault(x => x.uid == uid);
        }

        public bool DoesTagExists(string value)
        {
            return companyContext.Tags.Any(x => x.Value == value && x.State == State.Active);
        }

        public bool DoesTagExists(TagApiModel model)
        {
            return companyContext.Tags.Any(x => x.Value == model.Value && x.uid != model.uid && x.State == State.Active);
        }

        public List<AssetTagList> GetAssetsPathForTag(Guid tagUid)
        {
            string sql = @"select D.DisplayValue ,
						AST.Object,
						A.ObjectID as AssetId,
						AST.ObjectID as AssetTypeId,
						AST.Name
                        from Tag T
	                inner join AssetTag AT on AT.TagId = T.Id
	                inner join Asset A on A.ID = AT.AssetID
					inner join AssetType AST ON AST.ID = A.AssetTypeId
	                left join dbo.GetAssetDisplayValue() D on D.ID = A.ID
                where t.uid = @uid
                ";

            var result = companyContext.Query<dynamic>(sql, new { uid = tagUid }).ToList();

            var ret = new List<AssetTagList>();
            foreach (var item in result)
            {
                var atl = new AssetTagList();
                ret.Add(atl);

                atl.DisplayName = item.DisplayValue;
                switch (item.Object.ToString())
                {
                    case "ArtifactType":
                        atl.Breadcrumbs = "Glossary <i class=\"fa fa-chevron-right\"></i> " + item.Name;
                        atl.Url = $"/artifact/{item.AssetTypeId}/{item.AssetId}";
                        break;
                    case "PolicyType":
                        atl.Breadcrumbs = "Policy <i class=\"fa fa-chevron-right\"></i> " + item.Name;
                        atl.Url = $"/policy/{item.AssetTypeId};hierarchyId={item.AssetId}";
                        break;
                    case "TaxonomyType":
                        atl.Breadcrumbs = "Model <i class=\"fa fa-chevron-right\"></i> " + item.Name;
                        atl.Url = $"/model/{item.AssetTypeId};hierarchyId={item.AssetId}";
                        break;
                    case "RuleType":
                        atl.Breadcrumbs = "Rule <i class=\"fa fa-chevron-right\"></i> " + item.Name;
                        atl.Url = $"/quality/rule/{item.AssetTypeId}/{item.AssetId}";
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
                        INSERT INTO [queue].[Task] 
                        ([Action], [Object], [ObjectID],[Custom]) 
                        select 
                        'TagConsolidated', 
                        'Tag', 
                        FromId,
                        [queue].WriteIndexXml('', 'Tag', TargetId, coalesce(@resourceId, 0)) 
                        from ConsolidateData
                        
                        select T.uid, Items.count as UseCount from Tag T
                        	cross apply (select count(*) from AssetTag where TagId = T.Id)Items (count)
                        where T.uid = @parentUid or T.uid in (select uid from @children)";


            var result = companyContext.Query<TagApiModel>(sql, new { parentUid, resourceId = companyContext.CurrentResourceID });
            return result;
        }

        public List<dynamic> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            string value = "";
            Guid exceptUid = Guid.Empty;
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
                    case "value":
                        value = queryitem.Value.ToLower();
                        break;
                }
            }

            string sql = @"select T.Value as name, T.uid as code, Results.count from Tag T 
                            cross apply (select count(*) from AssetTag where TagID = T.ID)Results(count)
                            where State = 1 and LOWER(T.Value) like '%'+@value+'%' and T.uid != @exceptUid";

            return companyContext.Query<dynamic>(sql, new { value, exceptUid }).ToList();
        }


        private void AddTagAudit(List<Tag> tags, string action)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var tag in tags)
                sb.AppendLine(GetAuditQuery(tag, action));

            companyContext.Query<int>(sb.ToString()).FirstOrDefault();
        }

        private void AddTagAudit(Tag tag, string action)
        {
            string sql = GetAuditQuery(tag, action);
            companyContext.Query<int>(sql).FirstOrDefault();
        }

        private string GetAuditQuery(Tag tag, string action)
        {
            return $@"INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
                         VALUES ('{action}','Tag',{tag.ID},[queue].WriteIndexXml('', 'Tag', {tag.ID}, coalesce({companyContext.CurrentResourceID}, 0)))";
        }


        public bool DoesAssetTagExists(int tagId, long assetId)
        {
            return companyContext.AssetTags.Any(x => x.TagID == tagId && x.AssetID == assetId);
        }


        public AssetTag CreateAssetTag(int tagId, long assetId)
        {
            if (this.DoesAssetTagExists(tagId, assetId))
                return null;

            var assetTag = new AssetTag()
            {
                TagID = tagId,
                AssetID = assetId

            };

            companyContext.AssetTags.Add(assetTag);
            companyContext.SaveChanges();
            return assetTag;
        }

        public AssetTag GetAssetTag(int tagId, long assetId)
        {
            return companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
        }
        public bool DeleteAssetTag(int tagId, long assetId)
        {
            AssetTag tag = companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
            return companyContext.Delete<AssetTag>(tag);
        }


        public bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId)
        {
            bool hasPersmission = companyContext.CurrentResourceIsAdmin;
            if (!hasPersmission)
            {
                AssetTag tag = companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
                if (tag != null)
                    hasPersmission = tag.CreatedBy == companyContext.CurrentResourceID;
            }

            if (!hasPersmission)
            {
                hasPersmission = companyContext.HasAssetPermission(assetId, Permission.ModifyAsset);
            }
            return hasPersmission;
        }

        public TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            TagDetailApiModel result = new TagDetailApiModel();
            result.pageNum = 1;
            result.pageSize = 200;

            var dbArgs = new DynamicParameters();
            string whereConnector = " and ";
            string sortField = "DisplayValue";
            string sortOrder = "ASC";
            List<string> whereClauses = new List<string>();
            //?globalSearch = &DisplayValue = &AssetType = &TagsAsString = &sortBy = DisplayValue & sortOrder = 1

            bool hasGlobalSearch = queryParams.Any(x => x.Key.ToLower() == "globalsearch" && !string.IsNullOrEmpty(x.Value));

            foreach (var param in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                switch (param.Key.ToLower())
                {
                    case "displayvalue":
                        if (!hasGlobalSearch)
                        {
                            dbArgs.Add("displayvalue", $"%{param.Value.ToLower()}%");
                            whereClauses.Add("LOWER(ADV.DisplayValue) like @displayvalue");
                        }
                        break;
                    case "assettype":
                        if (!hasGlobalSearch)
                        {
                            dbArgs.Add("assetname", $"%{param.Value.ToLower()}%");
                            whereClauses.Add("LOWER(AST.Name) like @assetname");
                            AddAssetTypeParam(dbArgs, whereClauses, param.Value);
                        }
                        break;
                    case "tagsasstring":
                        if (!hasGlobalSearch)
                        {
                            dbArgs.Add("tagsasstring", $"%{param.Value.ToLower()}%");
                            whereClauses.Add("LOWER(AssetTags.Tags) like @tagsasstring");
                        }
                        break;
                    case "globalsearch":
                        dbArgs.Add("globalsearch", $"%{param.Value.ToLower()}%");
                        whereClauses.Add("LOWER(AssetTags.Tags) like @globalsearch");
                        whereClauses.Add("LOWER(ADV.DisplayValue) like @globalsearch");
                        whereClauses.Add("LOWER(AST.Name) like @globalsearch");

                        AddAssetTypeParam(dbArgs, whereClauses, param.Value);

                        whereConnector = " or ";
                        break;
                    case "_pagesize":
                        int size = 0;
                        if (int.TryParse(param.Value, out size))
                        {
                            result.pageSize = int.Parse(param.Value);
                        }
                        else throw new Exception("Invalid value for page size parametar!");
                        break;
                    case "_pagenum":
                        int num = 0;
                        if (int.TryParse(param.Value, out num))
                        {
                            result.pageNum = int.Parse(param.Value);
                            if (result.pageNum <= 0) result.pageNum = 1;
                        }
                        else throw new Exception("Invalid value for page number parametar!");
                        break;
                    case "sortby":
                        if (param.Value.ToLower() == "displayvalue") sortField = "displayvalue";
                        if (param.Value.ToLower() == "assettype") sortField = "assettype";
                        if (param.Value.ToLower() == "tagsasstring") sortField = "AssetTags.Tags";
                        break;
                    case "sortorder":
                        int val = int.Parse(param.Value);
                        if (val >= 0) sortOrder = "ASC";
                        else sortOrder = "DESC";
                        break;
                }
            }

            string sortClause = $"ORDER BY {sortField} {sortOrder}";

            dbArgs.Add("tagUid", tagUid);
            string whereClause = $"WHERE T.uid = @tagUid";
            if (whereClauses.Count > 0)
            {
                whereClause += $" and ({string.Join(whereConnector, whereClauses)})";
            }

            //var countSql = $@"select count(*) from AssetTag AT
            //             inner join Tag T on AT.TagId = T.ID
            //             {whereClause}";

            //result.total = companyContext.Query<int>(countSql, dbArgs).FirstOrDefault();

            var pagingSql = $"OFFSET {result.pageSize * (result.pageNum - 1)} ROWS FETCH NEXT {result.pageSize} ROWS ONLY";
            var sql = $@";with cte as (
                        select AssetID, T.uid as TagUid, T.Value from AssetTag AT
	                        inner join Tag T on T.ID = at.TagID
                        )
                        select 
                        ADV.*, 
                        A.Id as AssetID,
						CASE 
							WHEN AST.Object = 'TaxonomyType' THEN 'Model ' + AST.Name
							WHEN AST.Object = 'ArtifactType' THEN 'Glossary ' + AST.Name
							WHEN AST.Object = 'PolicyType' THEN 'Policy ' + AST.Name
							WHEN AST.Object = 'RuleType' THEN 'Rule ' + AST.Name
							ELSE AST.Name
						END AS AssetType, 
                        A.Object,
                        A.ObjectID,
                        AssetTags.Tags as Tags
                        from Tag T
	                        inner join AssetTag AT on AT.TagID = T.ID
	                        inner join Asset A ON A.ID = AT.AssetID
	                        inner join AssetType AST ON AST.Id = A.AssetTypeId
	                        cross apply dbo.GetAssetDisplayValueById(A.ID)ADV
							cross apply (select Value,TagUid as uid from cte where AssetId = A.Id order by Value for json path)AssetTags(Tags)
                        {whereClause}
                        {sortClause}
                        {pagingSql}
                        for json path";

            var data = string.Join("", companyContext.Query<string>(sql, dbArgs).ToList());

            result.items = JsonConvert.DeserializeObject<List<TagDetail>>(data);
            if (result.items == null) result.items = new List<TagDetail>();
            return result;
        }

        public IEnumerable<dynamic> GetTooltip(Guid guid)
        {
            string sql = @"select T.Value, T.CreatedOn, ADV.DisplayValue as CreatedBy from Tag T 
                            inner join Asset R on R.Object = 'Resource' and R.ObjectID = T.CreatedBy
                            cross apply dbo.GetAssetDisplayValueById(R.ID)ADV
                            where T.Uid = @uid";

            var result = companyContext.Query<dynamic>(sql, new { uid = guid });
            return result;
        }

        private static void AddAssetTypeParam(DynamicParameters dbArgs, List<string> whereClauses, string value)
        {
            string paramValue = "";
            if ("model".Contains(value.ToLower()))
                paramValue = "TaxonomyType";

            if ("glossary".Contains(value.ToLower()))
                paramValue = "ArtifactType";

            if ("policy".Contains(value.ToLower()))
                paramValue = "PolicyType";

            if ("rule".Contains(value.ToLower()))
                paramValue = "RuleType";

            if (!string.IsNullOrEmpty(paramValue))
            {
                dbArgs.Add("assettype", paramValue);
                whereClauses.Add("AST.Object = @assettype");
            }
        }

    }
}