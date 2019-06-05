using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IIssueRepository
    {
        Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes();

        Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetType(Guid assetTypeUid);

    }
}
