using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace repositories
{
	public interface ICatalog : IConnectorLabelRepository, IProcessRepository
	{
		Platform Platform { get; }

		Task<RepositoryResponse<IEnumerable<TagApiModel>>> ConsolidateTagsAsync(Guid parentUid, List<Guid> uidsToMerge);

		Task<RepositoryResponse<bool>> CreateAssetTagAsync(long assetId, int tagId);

		Task<RepositoryResponse<AssetCrossReference>> CreateCrossReferenceAsync(AssetCrossReference model);

		Task CreateCrossReferencesAsync(ApiExecution execution, List<AssetCrossReference> import, int timeout = 3600);

		Task CreateSemanticType();

		Task<RepositoryResponse<TagApiModel>> CreateTagAsync(string value, Guid? tagTypeUid);

		Task<RepositoryResponse<TagTypeApiModel>> CreateTagTypeAsync(string value);

		Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);

		Task<RepositoryResponse<List<dynamic>>> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<IEnumerable<AssetTagList>>> ReadAssetBreadcrumbsByTagAsync(Guid tagUid);

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

		Task<RepositoryResponse<TagApiModel>> ReadTagAsync(Guid uid);

		Task<RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>> ReadTagsAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<TagTypeApiModel>> ReadTagTypeAsync(Guid uid);

		Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync();
		Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync(Guid assetTypeUid,string name);

		Task<RepositoryResponse<bool>> RemoveAssetTagAsync(long assetId, int tagId);

		Task<RepositoryResponse<AssetCrossReference>> RemoveCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<string>> RemoveSemanticType();

		Task<RepositoryResponse<bool>> RemoveTagsAsync(List<Guid> tags);

		Task<RepositoryResponse<bool>> RemoveTagTypesAsync(List<Guid> tagTypes);

		Task<RepositoryResponse<AssetCrossReference>> UpdateCrossReferenceAsync(AssetCrossReference model);

		Task<RepositoryResponse<Semantic>> UpdateSemanticType();

		Task<RepositoryResponse<bool>> UpdateTagAsync(Guid uid, string value);

		Task<RepositoryResponse<bool>> UpdateTagTypeAsync(Guid uid, string value);

		Task<IEnumerable<long>> GetAssetUids(List<Guid> childrenUids);
	}
}
