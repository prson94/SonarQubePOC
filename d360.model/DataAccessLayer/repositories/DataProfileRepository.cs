using d360.core.entities;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public class DataProfileRepository : BaseRepository, IDataProfileRepository
	{
		internal IStorageProvider Storage;
		internal IQueueSource Queue;

		public DataProfileRepository(
			ICompanyContext companyContext,
			ISecurityContextProvider securityContext,
			IStorageProvider storage, 
			IQueueSource queue)
			: base(companyContext, securityContext)
		{
			Queue = queue;
			Storage = storage;
		}

		public async Task<ApiExecutionInfo> PostBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = SecurityContext.CompanyID,
				CompanyDomainPrefix = SecurityContext.CompanyPrefix,
				ExecutionID = Guid.NewGuid(),
				ResourceID = execution.ResourceID
			};

			return await CreateApiBatchJob(executionInfo, execution, models, Storage, Queue).ConfigureAwait(false);
		}

		public async Task<ApiExecutionInfo> PutBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = SecurityContext.CompanyID,
				CompanyDomainPrefix = SecurityContext.CompanyPrefix,
				ExecutionID = Guid.NewGuid(),
				ResourceID = execution.ResourceID
			};

			return await CreateApiBatchJob(executionInfo, execution, models, Storage, Queue).ConfigureAwait(false);
		}

		public async Task<ApiExecutionInfo> DeleteBatchDataProfiles(List<AssetDataProfileDeleteModel> models, ApiExecution execution)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = SecurityContext.CompanyID,
				CompanyDomainPrefix = SecurityContext.CompanyPrefix,
				ExecutionID = Guid.NewGuid(),
				ResourceID = execution.ResourceID
			};

			return await CreateApiBatchJob(executionInfo, execution, models, Storage, Queue).ConfigureAwait(false);
		}

	}
}
