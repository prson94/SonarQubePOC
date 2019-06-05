using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using Dapper;

namespace d360.model.DataAccessLayer
{
    public class IssueRepository : IIssueRepository
    {
        ICompanyContext companyContext;
        public IssueRepository(ICompanyContext context)
        {
            this.companyContext = context;
        }

        private Expression<Func<IssueType, IssueTypeApiModel>> 
            apiModelMapper = x => new IssueTypeApiModel() { Description = x.Description, IsSystem = x.IsSystem, Name = x.Name, UpdatedOn = x.UpdatedOn, Uid = x.uid };

        public async Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes()
        {
            return await companyContext.IssueTypes.Select(apiModelMapper).ToListAsync();
        }

        public async Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetType(Guid uid)
        {
            var dbArgs = new DynamicParameters();
            string whereClause = " where T.uid= @uid";
            dbArgs.Add("@uid", uid);

            string sql = $@"select I.uid,I.Name,I.Description,I.IsSystem,I.UpdatedOn
                from IssueTypeRelation R
                inner join AssetType T on T.ID = R.AssetTypeID
                inner join IssueType I on I.ID = R.IssueTypeID
                {whereClause}";

            var allocations = await this.companyContext.QueryAsync<IssueTypeApiModel>(sql, dbArgs);
            return allocations;
        }
    }
}
