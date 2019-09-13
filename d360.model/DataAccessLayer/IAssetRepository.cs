using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;

namespace d360.model.DataAccessLayer
{
    public interface IAssetRepository
    {
        Asset GetAssetByUID(Guid assetUid);
        Task<IEnumerable<AssetTypeApiViewModel>> GetAssetType(AssetTypeClass? Class, Guid? fusionTypeUid);
        List<AssetTypeClassInfo> GetAssetTypeList();
        Task<AssetsApiViewModel> GetAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams);
        dynamic GetFieldTypes(Guid assetTypeUid);

        List<DatabaseBulkAssetResult> PostAssets(List<AssetInsert> assets, AssetType assetType, ApiExecution execution, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false);
        Tuple<HttpStatusCode, string, string> AddAssetType(AssetTypeInsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate, int resourceId, out string nameFriendlyName, out bool isNamePartOfKey);
        List<DatabaseBulkAssetResult> PutAssets(List<AssetUpdate> assets, AssetType assetType, ApiExecution execution, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false);

        Tuple<HttpStatusCode, string, string> UpdateAssetType(AssetTypeInsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate);
        List<DatabaseBulkAssetResult> DeleteAsset(AssetDeletes assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> DeleteBulkAssetTypes(AssetTypeDeletes assetTypes, ApiExecution execution);
        Task<ApiExecutionInfo> BulkDeleteAssets(Guid assetTypeUid, AssetDeletes assets, ApiExecution execution, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> PutBulkAssets(Guid assetTypeUid, List<AssetUpdate> assets, ApiExecution execution, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> PostBulkAssets(List<AssetInsert> assets, ApiExecution execution, bool sendWorkflowEvents = true);
        Predicate GetPredicateByUID(Guid predicateGuid);
        AssetType GetAssetTypeByUID(Guid assetTypeUid);
        AssetType GetAssetTypeByModel(AssetTypeInsert model);
        ApiExecution GetExecutionItemByUid(Guid executionUid);
        void UpsertObjectStyle(string type, int id, string foreColor, string backColor, string objectName = "Tx");
        bool DoesAssetExists(Guid uid);


    }
}