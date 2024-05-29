using d360.core.security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISecurity
	{
		Platform Platform { get; }

		Task<RepositoryResponse<Rule>> CreatePolicyAsync(CreateRule model);

		Task<RepositoryResponse<ReadRuleOverride>> CreatePolicyOverrideAsync(CreateRuleOverride model);

		Task<RepositoryResponse<ReadRole>> CreateRoleAsync(CreateRole model);

		Task<RepositoryResponse<IEnumerable<ReadRule>>> ReadPoliciesAsync();

		Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRolesAsync();

		Task<RepositoryResponse<bool>> RemovePolicyAsync(Guid uid);

		Task<RepositoryResponse<bool>> RemovePolicyOverrideAsync(Guid uid);

		Task<RepositoryResponse<bool>> RemoveRoleAsync(Guid uid);

		Task<RepositoryResponse<ReadRule>> UpdatePolicyAsync(Guid uid, ReadRule model);

		Task<RepositoryResponse<ReadRuleOverride>> UpdatePolicyOverrideAsync(Guid uid, CreateRuleOverride model);

		Task<RepositoryResponse<ReadRole>> UpdateRoleAsync(Guid uid, CreateRole model);
	}
}
