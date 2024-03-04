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

		Task<RepositoryResponse<AssetCrossReference>> CreateCrossReferenceAsync(AssetCrossReference model);

		Task CreateCrossReferencesAsync(ApiExecution execution, List<AssetCrossReference> import, int timeout = 3600);

		Task CreateSemanticType();

		Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);

		Task<AssetDetail> ReadAssetDetail(long id);

		Task<AssetDetail> ReadAssetDetail(string @object, int objectId);

		Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000);

		Task<IEnumerable<AssetTypeApiViewModel>> ReadAssetTypes(int pageNum = 0, int pageSize = 5000);

		Task ReadAssetTypeDefinition();

		Task<IEnumerable<AssetCrossReferenceResult>> ReadCrossReferenceResultsAsync(Guid executionId);

		Task<IEnumerable<AssetCrossReference>> ReadCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task ReadProfiles();

		Task ReadRelationTypeDefinition();

		Task ReadSemanticTypes();

		Task<RepositoryResponse<AssetCrossReference>> RemoveCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<string>> RemoveSemanticType();

		Task<RepositoryResponse<AssetCrossReference>> UpdateCrossReferenceAsync(AssetCrossReference model);

		Task<RepositoryResponse<Semantic>> UpdateSemanticType();
	}
}
