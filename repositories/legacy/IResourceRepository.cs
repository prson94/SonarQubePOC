using System;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
    public interface IResourceRepository
    {
        Task<GlobalReportingResource> GetByUidAsync(Guid uid);
    }
}
