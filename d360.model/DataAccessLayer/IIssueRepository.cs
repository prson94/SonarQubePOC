using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IIssueRepository
    {
        Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetType(Guid assetTypeUid);

        IssueType GetIssueTypeByUID(Guid issueTypeUid);

        Issue GetIssueByUID(Guid issueUid);
    }
}
