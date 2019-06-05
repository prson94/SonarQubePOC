using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface ICrossReferencesRepository
    {
        Task<int> CreateNewCrossReference(AssetCrossReference model);
        Task<int> DeleteCrossReferenceByDataSource(string dataSource);
        Task<int> DeleteCrossReferenceByDataSource(string dataSource, string type);
        Task<int> DeleteCrossReferenceByType(string type);
        Task<int> DeleteCrossReferenceByUid(Guid uid);
        Task<IEnumerable<AssetCrossReference>> GetByAssetUid(string assetUid);
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByDataSource(string dataSource);
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByType(string type);
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByTypeId(string type, string externalId);
        Task<IEnumerable<AssetCrossReference>> GetCrossReferences(IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<bool> PostBulkCrossReference(List<AssetCrossReference> models);
        Task<int> PutCrossReference(Guid uid, AssetCrossReference model);
        Task<int> PutCrossReference(Guid uid, string dataSource, string type, AssetCrossReference model);
        Task<bool> XrefExists(AssetCrossReference model);
    }
}