using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public interface IDashboardRepository
	{
		Task<List<DashboardApiGetModel>> GetDashboardsAsync(Guid? uid, DashboardLocation? location, int? id);
		Task<DashboardApiGetModel> PostDashboardAsync(DashboardApiPostModel postModel);
		Task<bool> DeleteDashboard(Guid? uid);
	}
}