using d360.core.entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public interface IDashboardRepository
	{
		Task<List<DashboardApiGetModel>> GetDashboardsAsync();
	}
}