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

		Task CreateSemanticType();

		Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);

		Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000);

		Task ReadAssetTypeDefinition();

		Task ReadProfiles();

		Task ReadRelationTypeDefinition();

		Task ReadSemanticTypes();

		Task RemoveSemanticType();

		Task UpdateSemanticType();
	}
}
