using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;

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
            apiModelMapper = x => new IssueTypeApiModel() { Description = x.Description, IsSystem = x.IsSystem, Name = x.Name, UpdatedOn = x.UpdatedOn, uid = x.uid };

        public async Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes()
        {
            return await companyContext.IssueTypes.Select(apiModelMapper).ToListAsync();
        }
    }
}
