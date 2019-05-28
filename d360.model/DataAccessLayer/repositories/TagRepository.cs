using d360.core.entities;
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
    }
}