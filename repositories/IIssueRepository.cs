using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IIssueRepository
    {
        Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetType(Guid assetTypeUid);

        IssueType GetIssueTypeByUID(Guid issueTypeUid);

        Issue GetIssueByUID(Guid issueUid);
    }
}
