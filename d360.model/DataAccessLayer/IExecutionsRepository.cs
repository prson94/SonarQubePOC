using d360.core;
using d360.core.entities;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IExecutionsRepository
	{
		Task<ApiExecutionInfo> BulkPatchAssetAndRelations(PatchBulkCatalogRequestModel payload);

		Task<APIExecutionAPIModelResult> GetExecutions(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<EndpointPayloadResponse<dynamic>> GetExecutionStatus(Guid executionUid, bool includeResults = true);

		ApiExecution GetExecutionItemByUid(Guid executionUid);

		Task PatchCatalog(int executionId, PatchBulkCatalogRequestModel payload);
	}
}
