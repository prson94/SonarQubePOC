using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
    public class TagRepository : ITagRepository
    {
        ICompanyContext companyContext;
        public TagRepository(ICompanyContext context)
        {
            this.companyContext = context;
        }

        public async Task<IEnumerable<Tag>> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var sql = "select uid, DataSource, Type, ExternalID, FieldHash from [dbo].[AssetCrossReference]";
            List<string> queryFilters = new List<string>();


            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_assetuid"))
            {
                Guid uid = new Guid();

                var tagUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                if (Guid.TryParse(tagUidString, out uid))
                {
                    dbArgs.Add("@uid", uid);
                    queryFilters.Add($"[UID] = @uid");
                }
            }

            if (queryFilters.Count > 0)
            {
                sql += " where " + string.Join(" and ", queryFilters);
            }
            return (await companyContext.QueryAsync<Tag>(sql, dbArgs));            
        }

    }
}
