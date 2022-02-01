using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core.entities;
using d360.model.DataAccessLayer.repositories;

namespace d360.model.DataAccessLayer
{
    public interface IAuditDapperRepository
    {
        Task<PagedApiBaseViewModel<AssetAuditApiItemModel>> PagedAuditViewAsync(Guid? assetUid, Guid? assetTypeUid, string action, DateTime? startDate, DateTime? endDate,
            string filter, IReadOnlyList<OrderByModel> orderByList, int pageNum, int pageSize);

        Task<IReadOnlyList<AssetAuditApiItemModel>> AuditViewAsync(
            Guid? assetUid,
            Guid? assetTypeUid,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            string filter,
            IReadOnlyList<OrderByModel> orderByList
        );
    }
}
