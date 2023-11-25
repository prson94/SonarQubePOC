using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace repositories
{
	public interface ICatalog
	{
		Platform Platform { get; }

		Task<List<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);

		Task<AssetPathResults> GetAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000);
	}
}
