using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace d360.model.DataAccessLayer
{
	public interface IDashboardRepository
	{
		Task<List<DashboardApiGetModel>> GetDashboardsAsync(Guid? uid, DashboardLocation? location, int? id, Guid? assetTypeUid);
		Task<DashboardApiGetModel> PostDashboardAsync(DashboardApiUpsertModel postModel);
		Task<DashboardApiGetModel> PutDashboardAsync(DashboardApiUpsertModel model);
		bool DeleteDashboard(Guid? uid);
		void ValidateDashboardModel(DashboardApiUpsertModel model);
	}
}