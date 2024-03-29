using d360.core.entities;
using repositories.dis.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.dis
{
	public class Catalog : Repository, ICatalog
	{
		public string BaseUrl { get { return "https://data-catalog-dev.govern.cloud.precisely.services"; } }

		public Task<RepositoryResponse<IEnumerable<TagApiModel>>> ConsolidateTagsAsync(Guid parentUid, List<Guid> uidsToMerge)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> CreateAssetTagAsync(long assetId, int tagId)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<AssetCrossReference>> CreateCrossReferenceAsync(AssetCrossReference model)
		{
			throw new NotImplementedException();
		}

		public Task CreateCrossReferencesAsync(ApiExecution execution, List<AssetCrossReference> import, int timeout = 3600)
		{
			throw new NotImplementedException();
		}

		public Task CreateSemanticType()
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<TagApiModel>> CreateTagAsync(string value, Guid? tagTypeUid)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<TagTypeApiModel>> CreateTagTypeAsync(string value)
		{
			throw new NotImplementedException();
		}

		public Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<IEnumerable<AssetTagList>>> ReadAssetBreadcrumbsByTagAsync(Guid tagUid)
		{
			throw new NotImplementedException();
		}

		public Task<AssetDetail> ReadAssetDetail(long id)
		{
			throw new NotImplementedException();
		}

		public Task<AssetDetail> ReadAssetDetail(string @object, int objectId)
		{
			throw new NotImplementedException();
		}

		public async Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000)
		{
			var model = new AssetPathResults();
			var payload = await Get_PayloadFromService<PagesListModel<GetAssetModel>>($"{BaseUrl}/assets?filter=assetTypeId:eq(652e0aaa5a7fc6c78691b1f4)");

			model.items = payload.data.Select(a => new AssetPathResult { 
				path = string.Join(" > ", a.path.Select(p => string.Join(" / ", p.segments))),
				uid = Guid.NewGuid()//a.id
			});
			model.total = payload.data.Count;
			return model;
		}

		public Task ReadAssetTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<AssetTypeApiViewModel>> ReadAssetTypes(int pageNum = 0, int pageSize = 5000)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<AssetCrossReferenceResult>> ReadCrossReferenceResultsAsync(Guid executionId)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<AssetCrossReference>> ReadCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public Task ReadProfiles()
		{
			throw new NotImplementedException();
		}

		public Task ReadRelationTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public Task ReadSemanticTypes()
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<TagApiModel>> ReadTagAsync(Guid uid)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<PagedApiBaseViewModel<TagApiModel>>> ReadTagsAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<TagTypeApiModel>> ReadTagTypeAsync(Guid uid)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<TagTypeApiModel>> ReadTagTypesAsync()
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> RemoveAssetTagAsync(long assetId, int tagId)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<AssetCrossReference>> RemoveCrossReferencesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<string>> RemoveSemanticType()
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> RemoveTagsAsync(List<Guid> tags)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> RemoveTagTypesAsync(List<Guid> tagTypes)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<AssetCrossReference>> UpdateCrossReferenceAsync(AssetCrossReference model)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<Semantic>> UpdateSemanticType()
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> UpdateTagAsync(Guid uid, string value)
		{
			throw new NotImplementedException();
		}

		public Task<RepositoryResponse<bool>> UpdateTagTypeAsync(Guid uid, string value)
		{
			throw new NotImplementedException();
		}
	}
}
