using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using d360.core.entities;

namespace repositories
{
	public interface IAssetTypeRepository
	{
		Task<ICollection<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);
	}
}
