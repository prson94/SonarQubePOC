using d360.core;
using d360.core.entities;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
    public interface IExecutionsRepository
	{
		Task<ApiExecutionInfo> BulkPatchAssetAndRelations(PatchBulkCatalogRequestModel payload);

		Task<APIExecutionAPIModelResult> GetExecutions(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<EndpointPayloadResponse<dynamic>> GetExecutionStatus(Guid executionUid, bool includeResults = true, bool includeProcessingDetail = false);

		ApiExecution GetExecutionItemByUid(Guid executionUid);
		List<APIExecutionErrorApiModel> GetExecutionErrorsByUid(Guid executionUid);

		Task PatchCatalog(int executionId, PatchBulkCatalogRequestModel payload);

		void UpsertExecution(ApiExecution execution);
	}
}
