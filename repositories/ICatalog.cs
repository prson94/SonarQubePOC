using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace repositories
{
	public interface ICatalog : IConnectorLabelRepository, IProcessRepository
	{
		Platform Platform { get; }

		Task<RepositoryResponse<IEnumerable<TagApiModel>>> ConsolidateTagsAsync(Guid parentUid, List<Guid> uidsToMerge);

		Task<RepositoryResponse<bool>> CreateAssetTagAsync(long assetId, int tagId, int tagTypeId);

		Task<RepositoryResponse<TagApiModel>> CreateTagAsync(string value, Guid? tagTypeUid);

		Task<RepositoryResponse<TagTypeApiModel>> CreateTagTypeAsync(string value);

		Task<HttpStatusCode> DeleteSemanticAsync(string qualifier);

		Task<Asset> GetAsset(Guid? assetUid);

		Task<dynamic> GetAssetCopyOption(Guid uid, int assetId);

		Task<IEnumerable<long>> GetAssetUids(List<Guid> childrenUids);

		Task<dynamic> GetAssetIgnoredRelationships(Guid targetAssetUid);

		Task<IEnumerable<dynamic>> GetPossibleCreators();

		Task<IEnumerable<dynamic>> GetPossibleRedactors();

		Task<List<GetSemantic>> GetSemanticVersionsByQualifierAsync(string qualifier, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null);
		
		Task<List<GetSemantic>> PatchSemanticsAsync(List<PatchSemantic> semantics);

		Task<List<GetSemantic>> PostSemanticsAsync(List<PostSemantic> semantics);

		Task<List<GetSemantic>> PutSemanticsAsync(List<PutSemantic> semantics);	
		
		Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default);

		Task<RepositoryResponse<IEnumerable<AssetTagList>>> ReadAssetBreadcrumbsByTagAsync(Guid tagUid);

		Task<AssetDetail> ReadAssetDetail(long id);

		Task<AssetDetail> ReadAssetDetail(string @object, int objectId);

		Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000);

		Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadAssetsAsync(Guid assetTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<IEnumerable<AssetTypeApiViewModel>> ReadAssetTypes(int pageNum = 0, int pageSize = 5000);

		Task ReadAssetTypeDefinition();

		Task ReadProfiles();

		Task ReadRelationTypeDefinition();

		Task<RepositoryResponse<PagedApiBaseViewModel<GetSemantic>>> ReadSemanticTypesAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<TagApiModel>> ReadTagAsync(Guid uid);

		Task<RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>> ReadTagsAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<TagTypeApiModel>> ReadTagTypeAsync(Guid uid);

		Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync();

		Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync(Guid assetTypeUid, string name);

		Task<RepositoryResponse<bool>> RemoveAssetTagAsync(long assetId, int tagId, int tagTypeId);

		Task<RepositoryResponse<bool>> RemoveTagsAsync(List<Guid> tags);

		Task<RepositoryResponse<bool>> RemoveTagTypesAsync(List<Guid> tagTypes);
		
		Task<RepositoryResponse<List<dynamic>>> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<bool>> UpdateTagAsync(Guid uid, string value);

		Task<RepositoryResponse<bool>> UpdateTagTypeAsync(Guid uid, string value);

		Task<RepositoryResponse<List<AssetApiResultModel>>> UpsertAssetsAsync(int executionId, List<AssetApiModel> models, bool lookupFieldsPassedByValue = false, bool enableJsonAttributes = false);

		#region AssetTypeChangeProcessor
		Task<(bool isFieldCounterType, int? CounterInitialIndex)> IsFieldCounterType(int? fieldTypeId);

		Task<IEnumerable<int>> GetAssetsByType(int? assetTypeId);

		Task<bool> InsertAssetWithCounter(int? counterStartValue, int? assetTypeId, int? fieldTypeId, IEnumerable<int> assetIds);
		#endregion
	}
}
