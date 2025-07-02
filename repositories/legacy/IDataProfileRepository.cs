using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.queue;

namespace repositories
{
    public interface IDataProfileRepository
    {
        Task<ApiExecutionInfo> PostBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution);

        Task<ApiExecutionInfo> PutBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution);

        Task<ApiExecutionInfo> DeleteBatchDataProfiles(List<AssetDataProfileDeleteModel> models, ApiExecution execution);

	}
}
