using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IAssetTypeRepository
    {
        Task<ICollection<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);
    }
}
