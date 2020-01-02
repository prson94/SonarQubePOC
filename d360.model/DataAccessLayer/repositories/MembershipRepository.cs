using d360.core.entities.Membership;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class MembershipRepository : IMembershipRepository
    {
        internal ICompanyContext CompanyContext;

        public MembershipRepository(ICompanyContext companyContext)
        {
            this.CompanyContext = companyContext;
        }
        public async Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> condition = new List<string>();
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
                {
                    Guid uid;
                    var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                    if (Guid.TryParse(uidString, out uid))
                    {
                        if (uid != Guid.Empty)
                        {
                            condition.Add("A.Uid = @Uid");
                            dbArgs.Add("uid", uid);
                        }
                        
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "name"))
                {
                  
                    var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value;
                    if (!string.IsNullOrEmpty(name))
                    {

                        condition.Add("G.Name like  @name");
                        dbArgs.Add("name", name + '%');
                    }
                }

            }

            var whereStatements = condition.Count != 0 ? $" where  {string.Join(" and ", condition)}" : "";
;                        var sql = $@"Select A.Uid,G.Name,G.Description,gr1.uid as PrimaryOwnerUid,gr2.uid as SecondaryOwnerUid from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {whereStatements} ";

            var countSql = $@"Select count(*) from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {whereStatements} ";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await this.CompanyContext.QueryAsync<GroupApiModel>(sql, dbArgs);

            return new GroupApiModels() { items = results, Total = count };

        }
    }
}
