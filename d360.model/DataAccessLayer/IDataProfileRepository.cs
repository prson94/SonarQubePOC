using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.queue;

namespace d360.model.DataAccessLayer
{
    public interface IDataProfileRepository
    {
        List<DataProfileUpsertResponse> UpsertDataProfiles(List<DataProfileUpsertModel> DataProfileModels, ApiExecution execution, bool isInsert);

        Task<AssetDataProfilesApiViewModel> GetDataProfiles(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<AssetDataProfilesApiViewModel> GetDataProfiles(string profileIdentifier, IEnumerable<KeyValuePair<string, string>> queryParams);

        List<DataProfileDeleteResponse> DeleteDataProfiles(Asset asset, DateTime startDate, DateTime endDate, ApiExecution execution, bool cascade = false);

        Task<ApiExecutionInfo> PostBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution);

        Task<ApiExecutionInfo> PutBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution);

        Task<ApiExecutionInfo> DeleteBatchDataProfiles(List<AssetDataProfileDeleteModel> models, ApiExecution execution);

        Task<AssetDataProfilesMatchingAssetsApiViewModel> GetMatchingAssets(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams, bool onlyTotal = false);

        Task<IEnumerable<DataProfileExportModel>> GetMatchedAssetsForExport(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<AssetDataProfileByTypeQualifierApiViewModel> GetAssetsByTypeQualifier(string typeQualifier, decimal minConfidence, IEnumerable<KeyValuePair<string, string>> queryParams, bool isExport = false);
        Task<bool> DoesTypeQualifierExist(string typeQualifier);
    }
}
