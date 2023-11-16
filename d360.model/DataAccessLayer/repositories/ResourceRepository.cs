using d360.core.entities;
using repositories;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public class ResourceRepository : IResourceRepository
    {
        private readonly ICompanyContext CompanyContext;

        public ResourceRepository(ICompanyContext companyContext)
        {
            CompanyContext = companyContext;
        }

        public GlobalReportingResource GetResouceByUID(Guid uid)
        {
            return CompanyContext.Filter<GlobalReportingResource>(i => i.Uid == uid).SingleOrDefault();
        }

        public Task<GlobalReportingResource> GetByUidAsync(Guid uid)
        {
            return CompanyContext.GlobalReportingResources.FirstOrDefaultAsync(x => x.Uid == uid);
        }
    }
}
