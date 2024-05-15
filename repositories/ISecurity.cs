using d360.core.security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISecurity
	{
		Platform Platform { get; }

		Task<RepositoryResponse<List<ReadRole>>> CreateRoles(List<CreateRole> models);

		Task<RepositoryResponse<Rule>> CreateRule(CreateRule model);

		Task<RepositoryResponse<Rule>> CreateAssignmentOverride(CreateRuleOverride model);

		Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRoles();

		Task<RepositoryResponse<IEnumerable<ReadRule>>> ReadRules();
	}
}
