using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.entities.Process;
using d360.core.enums;
using d360.core.queue;
using SpreadsheetLight;

namespace d360.model.DataAccessLayer
{
    public interface IAssetRepository
    {
        Asset GetAssetByObjectId(string obj, int objId);
        Asset GetAssetByUID(Guid assetUid);
        Task<IEnumerable<AssetTypeApiViewModel>> GetAssetType(IEnumerable<KeyValuePair<string, string>> queryParams, AssetTypeClass? Class, Guid? assetTypeUid);
        List<AssetTypeClassInfo> GetAssetTypeList();
        Task<AssetsApiViewModel> GetAssets(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams, bool useAsAdmin = false, CancellationToken? cancellationToken = null);
        Task<AssetPathResults> GetAssetPaths(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<AssetsByPathApiViewModel> GetAssetsByPath(AssetsByPathApiRequestModel model);
        dynamic GetFieldTypes(Guid assetTypeUid);

        List<DatabaseBulkAssetResult> PostAssets(List<AssetInsert> assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, bool useTempTablesForField = false);
        Tuple<HttpStatusCode, string, string> AddAssetType(AssetTypeUpsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate, int resourceId, out string nameFriendlyName, out bool isNamePartOfKey);
        List<DatabaseBulkAssetResult> PutAssets(List<AssetUpdate> assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, bool useTempTablesForField = false);

        Tuple<HttpStatusCode, string, string> UpdateAssetType(AssetTypeUpsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate);
        List<DatabaseBulkAssetResult> DeleteAsset(AssetDeletes assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> DeleteBulkAssetTypes(AssetTypeDeletes assetTypes, ApiExecution execution);
        Task<ApiExecutionInfo> BulkDeleteAssets(Guid assetTypeUid, AssetDeletes assets, ApiExecution execution, bool clearallassetsfromtype, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> PutBulkAssets(Guid assetTypeUid, List<AssetUpdate> assets, ApiExecution execution, bool sendWorkflowEvents = true);
        Task<ApiExecutionInfo> PostBulkAssets(List<AssetInsert> assets, ApiExecution execution, bool sendWorkflowEvents = true);
        Predicate GetPredicateByUID(Guid predicateGuid);
        AssetType GetArtifactTypeByID(int artifactTypeId);
        AssetType GetAssetTypeByUID(Guid assetTypeUid);
        AssetType GetAssetTypeByUidAndClass(Guid assetTypeUid, AssetTypeClass @class);
        AssetType GetAssetTypeByModel(AssetTypeUpsert model);
        ApiExecution GetExecutionItemByUid(Guid executionUid);
        Task<APIExecutionAPIModelResult> GetExecutionItems(IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<APIExecutionExternalAPIModelResult> GetConnectorStatusItems(IEnumerable<KeyValuePair<string, string>> queryParams, DateTime? _startDate, DateTime? _endDate, Guid? externalId, string status, string component);
        void UpsertAssetStyle(int assetTypeId, string foreColor, string backColor, string icon, string objectName = "Tx");
        bool DoesAssetExists(Guid uid);
        bool IsReachedTransformationLimit(AssetTypeUpsert model);

        Guid GetRuleUIDFromRuleID(int id);
        Task<dynamic> GetAssetDetails(Asset asset);
        Task<List<extensions.PathComponent>> GetAssetPath(Guid assetUid);
        Task<Dictionary<Guid, List<extensions.PathComponent>>> GetAssetPathComponents(IEnumerable<Guid> assetUids);
        Task<dynamic> GetAssetTypeDetails(AssetType type);
        Task<SLDocument> GetAssetsExcel(Guid assetTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams, bool isChildItem = false);
        Task<AssetCountsModel> GetAssetCountOfAssetTypeUid(Guid assetTypeUid);
        Task<IEnumerable<AssetTypeCountModel>> GetAssetTypeCounts(int[] filterClasses, IEnumerable<KeyValuePair<string, string>> queryParams, Guid? assetTypeUid = null);
        Task<AssetsCountModel> GetAssetsCounts();
        Task<dynamic> GetAssetTypeObjectAndObjectId(Guid uid);
        Task<dynamic> GetExecutionStatusModel(Guid executionUid, bool includeResults = true);
        List<DatabaseBulkAssetTypeResult> DeleteSingleAssetType(AssetTypeDeletes assetTypes, AssetType assetType, ApiExecution execution);
        List<ValidationError> ValidateAssetUpsertModel(List<UpsertModel> model, bool validateFields = true, bool nullifyEmptyFields = false);
        Task<SLDocument> GetHierarchyExcel(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams, bool stripHtml);
        Task<dynamic> GetAssetSingle(Guid assetUid);
        Task<List<extensions.IndexFieldDisplay>> GetAssetSearchFields(Guid assetUid);
        Task PopulateSheetForAssetTypeAndAssets(SLDocument document, AssetType assetType, List<Guid> assetUids);
        Task<List<AssetTypeExportTemplate>> GetExportTemplates(Guid assetTypeUid = default(Guid), Guid exportTemplateUID = default(Guid));
        Task<AssetWatchers> GetAssetWatchers(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<WatchedAssetTypeDetailModel> GetWatchedAssetDetails(Guid assetTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        ApiExecutionExternalViewModel AddConnectorStatus(ApiExecutionExternalRequestModel model);

        IEnumerable<dynamic> GetPossibleOwnersForAssetType(AssetType assetType);

        Task<AssetDescendantsResults> GetAssetDescendants(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}