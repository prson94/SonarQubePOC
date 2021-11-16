using d360.core.entities;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IResponsibilityRepository
    {
        Task<AssetResponsibilitiesApiModel> GetResponsibilities(IEnumerable<KeyValuePair<string, string>> queryParams, Guid responsibilityUidFilter, Guid assigneeUidFilter, Guid assetUidFilter, Guid assetTypeUidFilter, int pageSize, int pageNum, int timeout);
        Task<IEnumerable<OwnershipApiModel>> GetOwnership(Guid assetUid);
        Task<bool> HasOwnership(Guid assetUid);
        Task<ResponsibilityTypeRuleStatsViewModel> GetResponsibilityRuleStats(Guid responsibilityTypeRuleUid);
        Task<IEnumerable<ResponsibilityTypeRuleViewModel>> GetResponsibilityRules(Guid responsibilityTypeUid);
        Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocations(Guid responsibilityTypeUid);
        Task<IEnumerable<ResponsibilityTypeAllocationViewModel>> GetResponsibilityTypeAllocationsByAsset(Guid assetTypeUid);
        Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypesByAssetUid(Guid assetTypeUid);
        Task<IEnumerable<ResponsibilityTypeViewModel>> GetResponsibilityTypes();
        List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypeUpserts, ApiExecution execution);
        ResponsibilityTypeDeleteResult DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel responsibilityTypeDelete);
        Task<IEnumerable<ClaimsViewModel>> GetClaims();
        Task<dynamic> GetResponsibilityType(Guid uid);
        ResponsibilityTypeAllocationResponseModel AddAllocation(ResponsibilityType ResponsibiltyType, AssetType AssetType, IEnumerable<int> PermissionsBitMask);
        ResponsibilityTypeAllocationResponseModel EditAllocation(ResponsibilityType responsibility, AssetType assetType, List<int> permissions);
        Task<ResponsibilityTypeAllocationResponseModel> DeleteAllocation(ResponsibilityType responsibility, AssetType assetType, bool cascade);
        string GetResponsibilityTypeUsedInOwnershipLookupMessage(ResponsibilityType responsibility, AssetType assetType);
        ResponsibilityType GetResponsibilityTypeByUID(Guid uid);
        bool IsValidResponsibilityForAsset(Guid responsibilityUid, Guid assetUid);
        IEnumerable<SecurityAssetModel> GetSecurityAssetModelsForResources(List<Guid> resourceUids, Guid assetUid, Guid responsibilityUid);
        void InsertResponsibilityOverrides(ResponsibilityType responsibilityType, Asset asset, List<SecurityAssetModel> resources, string context);
        void DeleteResponsibilityOverrides(ResponsibilityType responsibilityType, Asset asset, List<SecurityAssetModel> resources);

        List<ResponsibilityRuleUpsertResponseModel> UpsertResponsibilityRules(Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> responsibilityRules, ApiExecution execution);
        Task<IReadOnlyList<ResponsibilityRuleDeleteResponse>> DeleteResponsibilityRulesAsync(Guid responsibilityTypeUid, IReadOnlyList<Guid> rulesForDeletion);

        Task<ApiExecutionInfo> PostBatchResponsibilityOverride(List<BulkResponsibilityOverridePostModel> models, ApiExecution execution);
        Task<ResponsibilityRuleTestResponseModel> GetResponsibilityRuleTestResults(ResponsibilityRuleUpsertModel test, bool hideD3SUsers, bool includeThen, IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}
