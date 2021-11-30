using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IResourceRepository
    {
        [Obsolete]
        GlobalReportingResource GetResouceByUID(Guid uid);

        Task<GlobalReportingResource> GetByUidAsync(Guid uid);
    }
}