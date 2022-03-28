using System;
using System.Threading.Tasks;

using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IResourceRepository
    {
        [Obsolete]
        GlobalReportingResource GetResouceByUID(Guid uid);

        Task<GlobalReportingResource> GetByUidAsync(Guid uid);
    }
}
