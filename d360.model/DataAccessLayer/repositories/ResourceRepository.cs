using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ResourceRepository : IResourceRepository
    {
         ICompanyContext companyContext;
        public ResourceRepository(ICompanyContext companyContext)
        {
            this.companyContext = companyContext;
        }

        public GlobalReportingResource GetResouceByUID(Guid uid)
        {
            return companyContext.Filter<GlobalReportingResource>(i => i.Uid==uid).SingleOrDefault();
        }

        public Task<GlobalReportingResource> GetByUidAsync(Guid uid)
        {
            return companyContext.GlobalReportingResources.FirstOrDefaultAsync(x => x.Uid == uid);
        }
    }
}
