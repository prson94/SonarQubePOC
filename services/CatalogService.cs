using d360.core.entities;
using d360.core.queue;
using Microsoft.Extensions.Options;
using services.domain;

namespace services
{
	public interface ICatalogService 
	{
		/// <summary>
		/// A batch trigger method that will take the payload and save it to Azure Storage for asynchnous processing.
		/// This method also triggers a queue message for the async process to handle the payload.
		/// This method will return an execution identifier to be sent back to the caller.
		/// </summary>
		Task<Response<Guid>> BulkUpsertAssetsAsync(bool isInsert, Guid uid, List<AssetApiModel> models, string? applicationId = null, bool triggersWorkflow = true);

		/// <summary>
		/// The service call to process a batch of assets smaller than the real-time limit. 
		/// All required actions such as history, scoring, and workflow triggering, are handled.
		/// This is a synchronous call that will return the results of the upsert operation immediately.
		/// </summary>
		Task<Response<List<AssetApiResultModel>>> UpsertAssetsAsync(ApiExecutionAction action, int companyId, Guid assetTypeUid, List<AssetApiModel> assets, bool triggersWorkflow = true);
	}

	public class CatalogService: BaseService, ICatalogService
	{
		public CatalogService(
			IOptions<MailProviderOptions> mailOptions, IOptions<QueueProviderOptions> queueOptions, IOptions<StorageProviderOptions> storageOptions) : 
			base(mailOptions, queueOptions, storageOptions)
		{
		}
		
		// Example of getting a keyed service and injecting directly here needed, rather than constructor injection.
		//void SomeMethodName([FromKeyedServices("someKey")] IWorkspaces workspaces) { }

		public async Task<Response<Guid>> BulkUpsertAssetsAsync(bool isInsert, Guid uid, List<AssetApiModel> models, string? applicationId = null, bool triggersWorkflow = true)
		{
			var response = new Response<Guid>();
			// Functionality to come soon.
			return response;
		}

		public async Task<Response<List<AssetApiResultModel>>> UpsertAssetsAsync(ApiExecutionAction action, int companyId, Guid assetTypeUid, List<AssetApiModel> assets, bool triggersWorkflow = true)
		{ 
			var response = new Response<List<AssetApiResultModel>>();
			// Functionality to come soon.
			return response;
		}
	}
}
