using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IIssueRepository
    {
        Task<IEnumerable<IssueTypeApiModel>> GetIssueTypesAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetTypeAsync(Guid assetTypeUid);

        Task<IssueType> GetIssueTypeByUIDAsync(Guid issueTypeUid);

        Task<Issue> GetIssueByUIDAsync(Guid issueUid);

		Task<IEnumerable<dynamic>> GetIssuesByUserAsync(int? CurrentUserId);

	}
}
