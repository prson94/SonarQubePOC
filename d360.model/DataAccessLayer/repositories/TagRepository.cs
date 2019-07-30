using d360.core.entities;
using d360.core.enums;
using Dapper;
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
        public TagRepository(ICompanyContext context)
        {
            this.companyContext = context;
        }

        public bool DeleteTags(List<TagApiDeleteModel> model)
        {
            foreach (var item in model)
            {
                DeleteTag(item.uid);
            }
            return true;
        }

        public bool DeleteTag(Guid uid)
        {
            var model = companyContext.Filter<Tag>(i => i.uid == uid).SingleOrDefault();

            if (model == null && model.State != State.Deleted) return false;

            model.State = State.Deleted;
            AddTagAudit(model, "Delete");

            return companyContext.SaveChanges() > 0;
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
            AddTagAudit(existingTag,"Update");
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

        public List<dynamic> GetAssetsPathForTag(Guid tagUid)
        {
            string sql = @"select D.DisplayValue ,
                        'Glossary > Business Term' as AssetPath
                        from Tag T
	                inner join AssetTag AT on AT.TagId = T.Id
	                inner join Asset A on A.ID = AT.AssetID
	                left join dbo.GetAssetDisplayValue() D on D.ID = A.ID
                where t.uid = @uid
                ";

            var result = companyContext.Query<dynamic>(sql, new { uid = tagUid }).ToList();
            return result;
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
                        
                        ;with CTE as 
                        (
                        	select T.Id from AssetTag AT
                        	inner join Tag T on AT.TagID = T.ID
                        	inner join @children CH on CH.uid = T.uid
                        ) 
                        update AssetTag
                        set TagId = @consolidateToId
                        where TagId in (select Id from cte)


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
                        [queue].WriteIndexXml('', 'Tag', TargetId, coalesce(56, 0)) 
                        from ConsolidateData
                        
                        select T.uid, Items.count as UseCount from Tag T
                        	cross apply (select count(*) from AssetTag where TagId = T.Id)Items (count)
                        where T.uid = @parentUid or T.uid in (select uid from @children)";


            var result = companyContext.Query<TagApiModel>(sql, new { parentUid });
            return result;
        }

        public List<dynamic> SearchTags(string tag)
        {
            string sql = @"select T.Value as name, T.uid as code, Results.count from Tag T 
                            cross apply (select count(*) from AssetTag where TagID = T.ID)Results(count)
                            where State = 1 and LOWER(T.Value) like '%'+@term+'%'";

            return companyContext.Query<dynamic>(sql, new { term = tag }).ToList();
        }



        private void AddTagAudit(Tag tag, string action)
        {
            var sql = $@"INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID],[Custom]) 
                         VALUES ('{action}','Tag',{tag.Id},[queue].WriteIndexXml('', 'Tag', {tag.Id}, coalesce({companyContext.CurrentResourceID}, 0)))";
            companyContext.Query<int>(sql).FirstOrDefault();
        }

        public bool SetTaggingStatus(bool state)
        {
            throw new NotImplementedException();
        }
    }
}