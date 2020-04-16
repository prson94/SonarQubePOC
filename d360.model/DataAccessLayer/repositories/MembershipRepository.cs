using d360.core.entities.Membership;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.enums;
using System.Net;

namespace d360.model.DataAccessLayer
{
    public class MembershipRepository : IMembershipRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext CommunityContext;

        public MembershipRepository(ICompanyContext companyContext, ICommunityContext communityContext)
        {
            this.CompanyContext = companyContext;
            this.CommunityContext = communityContext;
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
                  
                    var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.Trim();
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
                {whereStatements}  order by G.Name  ";

            var countSql = $@"Select count(*) from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {whereStatements}  ";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await this.CompanyContext.QueryAsync<GroupApiModel>(sql, dbArgs);

            return new GroupApiModels() { items = results, Total = count };

        }

        public WorkHttpStatus DeleteResources(IEnumerable<UserApiDeleteModel> resources)
        {
            try
            {
                List<UserApiDeleteModel> models = new List<UserApiDeleteModel>();
                foreach (var model in resources)
                {
                    model.Resource = CompanyContext.GlobalReportingResources.SingleOrDefault(r => r.Uid == model.Uid && r.State != CompanyResourceState.Deleted);

                    if (model.Resource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Not Found", $"User for uid [{model.Uid}] not found.");
                    }

                    if (model.Resource.ResourceID < 1)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid User", $"User for uid [{model.Uid}] is a system user and cannot be deleted.");
                    }

                    model.CompanyResource = CommunityContext.CompanyResources.SingleOrDefault(r => r.CompanyID == CompanyContext.CurrentCompanyID && r.ResourceID == model.Resource.ResourceID && r.State != CompanyResourceState.Deleted);

                    if (model.CompanyResource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Not Found", $"User for uid [{model.Uid}] not found.");
                    }
                }

                foreach(var model in resources)
                {
                    model.Resource.State = CompanyResourceState.Deleted;
                    model.CompanyResource.State = CompanyResourceState.Deleted;

                    CompanyContext.Update(model.Resource);
                    CommunityContext.Update(model.CompanyResource);
                }

            }
            catch
            {
                return new WorkHttpStatus(HttpStatusCode.InternalServerError, "Internal Server Error", $"An internal server error occurred");
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "Success", "Users deleted successfully");
        }

    }
}
