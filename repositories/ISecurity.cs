using d360.core.security;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISecurity
	{
		Platform Platform { get; }

		Task<RepositoryResponse<Role>> CreateRole(CreateRole model);

		Task<RepositoryResponse<Rule>> CreateRule(CreateRule model);
	}
}
