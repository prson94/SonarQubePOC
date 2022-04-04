using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.queue;

namespace d360.model.DataAccessLayer
{
    public interface ICrossReferencesRepository
    {
        Task<int> CreateNewCrossReference(AssetCrossReference model);
        
        Task<int> DeleteCrossReferenceByDataSource(string dataSource, int timeout = 90);
        
        Task<int> DeleteCrossReferenceByDataSource(string dataSource, string type, int timeout = 90);
        
        Task<int> DeleteCrossReferenceByType(string type, int timeout = 90);
        
        Task<int> DeleteCrossReferenceByUid(Guid uid);
        
        Task<IEnumerable<AssetCrossReference>> GetByAssetUid(string assetUid);
        
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByDataSource(string dataSource);
        
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByType(string type);
        
        Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByTypeId(string type, string externalId);
        
        Task<IEnumerable<AssetCrossReference>> GetCrossReferences(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        IEnumerable<AssetCrossReferenceResult> PostBulkCrossReference(List<AssetCrossReference> models, ApiExecution execution);
        
        Task<int> PutCrossReference(Guid uid, AssetCrossReference model);
        
        Task<int> PutCrossReference(Guid uid, string dataSource, string type, AssetCrossReference model);
        
        Task<bool> XrefExists(AssetCrossReference model);
        
        Task<ApiExecutionInfo> PostBatchCrossReference(List<AssetCrossReference> crossReferences, ApiExecution execution, bool sendWorkflowEvents = true);
        
        BulkAssetCrossReferenceResult GetExecutionStatus(ApiExecution execution);
    }
}
