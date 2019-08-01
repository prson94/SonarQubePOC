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
        public TagRepository(ICompanyContext context)
        {
            this.companyContext = context;
        }

        public bool DeleteTag(Guid uid)
        {
            var model = companyContext.Filter<Tag>(i => i.uid == uid).SingleOrDefault();

            if (model == null && model.State != State.Deleted) return false;
                
            model.State = State.Deleted;
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
	                        t.CreatedOn,
	                        grc.uid as CreatedByUid,
	                        t.UpdatedOn,
	                        gru.uid as UpdatedByUid
                         from [tag] t
	                        left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
	                        left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID";

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
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagesize")) {
                
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

            companyContext.Tags.Add(tag);

            companyContext.SaveChanges();

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
            
            companyContext.SaveChanges();

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
            return companyContext.Tags.Any(x => x.Value == value);
        }

        public bool DoesTagExists(TagApiModel model)
        {
            return companyContext.Tags.Any(x => x.Value == model.Value && x.uid != model.uid);
        }

        public bool DoesAssetTagExists(int tagId,long assetId)
        {
            return  companyContext.AssetTags.Any(x => x.TagID ==tagId && x.AssetID == assetId);
        }


        public AssetTag CreateAssetTag(int tagId, long assetId)
        {
            if (this.DoesAssetTagExists(tagId, assetId))
                return null;

            var assetTag = new AssetTag()
            {
               TagID=tagId,
               AssetID=assetId

            };

            companyContext.AssetTags.Add(assetTag);
            companyContext.SaveChanges();
            return assetTag;
        }

        public AssetTag GetAssetTag(int tagId, long assetId)
        {
            return  companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
        }
        public bool DeleteAssetTag(int tagId, long assetId)
        {
            AssetTag tag= companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
            return companyContext.Delete<AssetTag>(tag);
        }


        public bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId)
        {
            bool hasPersmission = companyContext.CurrentResourceIsAdmin;
            if (!hasPersmission)
            {
                AssetTag tag = companyContext.AssetTags.Where(x => x.TagID == tagId && x.AssetID == assetId).SingleOrDefault();
                if(tag != null)
                    hasPersmission = tag.CreatedBy == companyContext.CurrentResourceID;
            }
            return hasPersmission;
        }

        public TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            TagDetailApiModel result = new TagDetailApiModel();
            result.pageNum = 1;
            result.pageSize = 200;

            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
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
                }
            }

            var countSql = @"select count(*) from AssetTag AT
	                        inner join Tag T on AT.TagId = T.ID
	                        where T.uid = @tagUid";

            result.total = companyContext.Query<int>(countSql, new { tagUid }).FirstOrDefault();

            var pagingSql = $"OFFSET {result.pageSize * (result.pageNum - 1)} ROWS FETCH NEXT {result.pageSize} ROWS ONLY";
            var sql = $@";with cte as (
                        select AssetID, T.ID as TagId, T.Value from AssetTag AT
	                        inner join Tag T on T.ID = at.TagID
                        )
                        select 
                        ADV.*, 
                        A.Id as AssetID,
                        AST.Object as AssetType, 
                        A.Object,
                        A.ObjectID,
                        AST.Name  as AssetTypeName,
                        (select TagId as Id, Value from cte where AssetId = A.Id order by Value for json path) as Tags
                        from Tag T
	                        inner join AssetTag AT on AT.TagID = T.ID
	                        inner join Asset A ON A.ID = AT.AssetID
	                        inner join AssetType AST ON AST.Id = A.AssetTypeId
	                        cross apply dbo.GetAssetDisplayValueById(A.ID)ADV
                        where T.uid = @tagUid
                        order by DisplayValue
                        {pagingSql}
                        for json path";

            var data = string.Join("", companyContext.Query<string>(sql, new { tagUid }).ToList());

            result.items = JsonConvert.DeserializeObject<List<TagDetail>>(data);
            return result;
        }

    }
}