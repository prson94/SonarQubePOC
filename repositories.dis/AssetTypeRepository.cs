using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using d360.core.entities;

namespace repositories.dis
{
	public class AssetTypeRepository : IAssetTypeRepository
	{
		public Task<ICollection<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
