using d360.core.entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface INavigationRepository
	{
		Task<IReadOnlyList<AdminConfigurationItem>> GetAdminConfigurationItems();
	}
}
