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

        Task<IssueType> GetIssueTypeByUID(Guid issueTypeUid);

        Task<Issue> GetIssueByUID(Guid issueUid);
    }
}
