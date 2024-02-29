using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.entities.Metric;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.featureflags;
using d360.model;
using d360.model.DataAccessLayer;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	public class ExecutionProcessor : BaseWebJob
	{
		const string FUNCTION_NAME = "ExecutionProcessor";

		const int DEFAULT_MERGE_BLOCK_SIZE = 500;
		const int DEFAULT_SQL_BULK_COPY_BLOCK_SIZE = 5000;
		const int DEFAULT_SQL_BULK_COPY_TIMEOUT = 0;
		const int DEFAULT_WORKFLOW_BATCH_SIZE = 50;

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly IStorageProvider Storage;
		readonly IFeatureFlagService FeatureFlags;

		public ExecutionProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue, IStorageProvider storage, IFeatureFlagService ff) : base(config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
			Storage = storage;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger("%ApiExecutionQueue%", Connection = "QueuesConnectionString")] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<ApiExecutionInfo>(myQueueItem);

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyID },
				{ "UrlPrefix", info.CompanyDomainPrefix },
				{ "ExecutionId", info.ExecutionID },
				{ "ExecutionAction", info.Action.ToString() }
			};

			using (log.BeginScope(logProperties))
			{
				#region More Dependency Injection

				var context = new UriSecurityContextProvider
				{
					CompanyID = info.CompanyID,
					ResourceID = info.ResourceID ?? 0,
					CompanyPrefix = info.CompanyDomainPrefix,
					IsAdministrator = false
				};
				var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
				var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true)
				{
					ApiExecutionQueue = Configuration["ApiExecutionQueue"],
					AssetGraphQueue = Configuration["AssetGraphQueue"],
					BulkLoadQueue = Configuration["BulkLoadQueue"],
					DisplayValueQueue = Configuration["DisplayValueQueue"],
					EventBusTopicName = Configuration["EventBusTopicName"],
					ScoringQueue = Configuration["ScoringQueue"],
					SearchIndexQueue = Configuration["SearchIndexQueue"]
				};

				var resource = company.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == company.CurrentResourceID);
				if (resource != null)
				{
					company.CurrentResourceIsAdmin = resource.IsAdministrator;
				}

				var fieldsRepository = new FieldsRepository(company, Queue, Storage, FeatureFlags);
				var assetRepository = new AssetRepository(company, Queue, Storage, community, FeatureFlags);
				var membershipRepository = new MembershipRepository(company, community, assetRepository, Queue, Storage, FeatureFlags);
				var relationshipRepository = new RelationshipRepository(community, company, Queue, Storage, FeatureFlags);

				#endregion

				await company.Connection.OpenAsync();
				var dbExecutionItem = company.Connection.Query<ApiExecution>("select * from api.Execution where ExecutionID = @ExecutionID", new { info.ExecutionID }).SingleOrDefault();

				try
				{
					if (dbExecutionItem != null)
					{
						int mergeBlockSize = DEFAULT_MERGE_BLOCK_SIZE;

						int dbExecutionTimeout = int.Parse(Configuration["DBExecuteQueryTimeout"]);

						if (int.TryParse(Configuration["V2ApiBatchMergeBlockSize"], out int tempBlockSize))
						{
							mergeBlockSize = tempBlockSize > 0 ? tempBlockSize : DEFAULT_MERGE_BLOCK_SIZE;
						}

						if (int.TryParse(Configuration["V2ApiBatchSqlBatchSize"], out int tempsqlBulkCopyBlockSize))
						{
							company.SqlBulkBatchSize = tempsqlBulkCopyBlockSize >= 0 ? tempsqlBulkCopyBlockSize : DEFAULT_SQL_BULK_COPY_BLOCK_SIZE;
						}

						if (int.TryParse(Configuration["V2ApiBatchSqlBulkTimeout"], out int tempsqlBulkCopyTimeout))
						{
							company.SqlBulkBatchTimeout = tempsqlBulkCopyTimeout >= 0 ? tempsqlBulkCopyTimeout : DEFAULT_SQL_BULK_COPY_TIMEOUT;
						}

						if (int.TryParse(Configuration["V2ApiBatchWorkflowBatchSize"], out int tempWorkflowBatchSize))
						{
							company.WorkflowSendBatchSize = tempWorkflowBatchSize >= 0 ? tempWorkflowBatchSize : DEFAULT_WORKFLOW_BATCH_SIZE;
						}

						bool executeJob = (dbExecutionItem.State != d360.core.enums.State.Deleted);

						if (executeJob)
						{
							var action = info.Action ?? dbExecutionItem.Action;

							string resultsSql = "";

							await markExecutionAsProcessing(dbExecutionItem, company);

							AssetType assetType;
							IntersectType intersectType;

							switch (action)
							{
								case ApiExecutionAction.PostAssets:
									var postAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);

									assetType = company.Filter<AssetType>(i => i.uid == postAssetsFields.AssetTypeUid).SingleOrDefault();
									if (assetType != null)
									{
										log.LogTrace($"Get PostAssets payload: {DateTime.UtcNow:hh:mm:ss}");
										var postAssets = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetInsert>>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"PostAssets: {DateTime.UtcNow:hh:mm:ss}");
										assetRepository.PostAssets(postAssets, assetType, dbExecutionItem, true, false);

										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from	api.ExecutionAsset where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Asset Type for uid [{postAssetsFields.AssetTypeUid}] not found.", company);
									}

									break;
								case ApiExecutionAction.PutAssets:
									var putAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);

									assetType = company.Filter<AssetType>(i => i.uid == putAssetsFields.AssetTypeUid).SingleOrDefault();
									if (assetType != null)
									{
										log.LogTrace($"Get PutAssets payload: {DateTime.UtcNow:hh:mm:ss}");
										var putAssets = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetUpdate>>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"PutAssets: {DateTime.UtcNow:hh:mm:ss}");
										assetRepository.PutAssets(putAssets, assetType, dbExecutionItem, true, false);

										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from	api.ExecutionAsset where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Asset Type for uid [{putAssetsFields.AssetTypeUid}] not found.", company);
									}

									break;
								case ApiExecutionAction.DeleteAssets:
									var deleteAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);

									assetType = company.Filter<AssetType>(i => i.uid == deleteAssetsFields.AssetTypeUid).SingleOrDefault();
									if (assetType != null)
									{
										log.LogTrace($"Get DeleteAssets payload: {DateTime.UtcNow:hh:mm:ss}");
										var deleteAssets = await Storage.DeserializeJsonObjectFromBlobAsync<AssetDeletes>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"DeleteAssets: {DateTime.UtcNow:hh:mm:ss}");
										assetRepository.DeleteAssets(deleteAssets, assetType, dbExecutionItem, true);
										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedAsset where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Asset Type for uid [{deleteAssetsFields.AssetTypeUid}] not found.", company);
									}
									break;
								case ApiExecutionAction.PostRelationships:
									var postRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(dbExecutionItem.Fields);

									intersectType = company.Filter<IntersectType>(i => i.uid == postRelationshipsFields.IntersectTypeUid).SingleOrDefault();
									if (intersectType != null)
									{
										log.LogTrace($"Get PostRelations payload: {DateTime.UtcNow:hh:mm:ss}");
										var postRelationships = await Storage.DeserializeJsonObjectFromBlobAsync<RelationshipInserts>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"PostRelationships: {DateTime.UtcNow:hh:mm:ss}");
										relationshipRepository.PostRelationships(intersectType, dbExecutionItem, postRelationships);
										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionRelationship where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Intersect Type for uid [{postRelationshipsFields.IntersectTypeUid}] not found.", company);
									}

									break;
								case ApiExecutionAction.PutRelationships:
									var putRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutRelationships>(dbExecutionItem.Fields);

									intersectType = company.Filter<IntersectType>(i => i.uid == putRelationshipsFields.IntersectTypeUid).SingleOrDefault();
									if (intersectType != null)
									{
										log.LogTrace($"Get PutRelations payload: {DateTime.UtcNow:hh:mm:ss}");
										var putRelationships = await Storage.DeserializeJsonObjectFromBlobAsync<RelationshipUpdates>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"PutRelationships: {DateTime.UtcNow:hh:mm:ss}");
										relationshipRepository.PutRelationships(intersectType, dbExecutionItem, putRelationships);

										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionRelationship where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Intersect Type for uid [{putRelationshipsFields.IntersectTypeUid}] not found.", company);
									}
									break;
								case ApiExecutionAction.DeleteRelationships:
									var deleteRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteRelationships>(dbExecutionItem.Fields);
									intersectType = company.Filter<IntersectType>(i => i.uid == deleteRelationshipsFields.IntersectTypeUid).SingleOrDefault();

									if (intersectType != null)
									{
										log.LogTrace($"Get DeleteRelations payload: {DateTime.UtcNow:hh:mm:ss}");
										var deleteRelationships = await Storage.DeserializeJsonObjectFromBlobAsync<RelationshipDeletes>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"DeleteRelationships: {DateTime.UtcNow:hh:mm:ss}");
										relationshipRepository.DeleteRelationships(dbExecutionItem, intersectType, deleteRelationships);

										resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedRelationship where ExecutionID = @executionId order by ItemNumber asc";
									}
									else
									{
										await markExecutionAsErred(dbExecutionItem, $"Intersect Type for uid [{deleteRelationshipsFields.IntersectTypeUid}] not found.", company);
									}
									break;
								case ApiExecutionAction.DeleteAssetTypes:
									log.LogTrace($"Get DeleteAssetTypes payload: {DateTime.UtcNow:hh:mm:ss}");
									var deleteAssetTypes = await Storage.DeserializeJsonObjectFromBlobAsync<AssetTypeDeletes>(info.StorageFolder, info.RequestFileName);

									log.LogTrace($"RemoveAssetTypes: {DateTime.UtcNow:hh:mm:ss}");
									assetRepository.DeleteAssetTypes(deleteAssetTypes, dbExecutionItem, false, true);
									company.CreateRollupPathChangedExecution();
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedAssetType where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PostCrossReferences:
									var connectionString = community.GetCompanyConnectionString();
									ICatalog catalog = new Catalog(new DapperConnectionProvider { ConnectionString = connectionString });
									var postCrossReferences = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetCrossReference>>(info.StorageFolder, info.RequestFileName);
									await catalog.CreateCrossReferencesAsync(dbExecutionItem, postCrossReferences, dbExecutionTimeout);
									resultsSql = @"select [ItemNumber], [uid], [Message], [Success] from api.ExecutionAssetCrossReference where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PostDataQualityResults:
									var metricsRepository = new MetricsRepository(company, Queue, Storage, FeatureFlags);
									var postDataQualityResultsRequest = await Storage.DeserializeJsonObjectFromBlobAsync<List<DataQualityInsertModel>>(info.StorageFolder, info.RequestFileName);
									metricsRepository.InsertDataQualityResult(postDataQualityResultsRequest, dbExecutionItem, true);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetResult where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PostDataProfile:
									var postDataProfile = await Storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(info.StorageFolder, info.RequestFileName);
									await company.UpsertDataProfilesAsync(postDataProfile, dbExecutionItem, true, dbExecutionTimeout);
									resultsSql = @"select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PutDataProfile:
									var putDataProfile = await Storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(info.StorageFolder, info.RequestFileName);
									await company.UpsertDataProfilesAsync(putDataProfile, dbExecutionItem, false, dbExecutionTimeout);
									resultsSql = @"select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.DeleteDataProfile:
									var deleteDataProfile = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetDataProfileDeleteModel>>(info.StorageFolder, info.RequestFileName);
									await company.DeleteDataProfilesAsync(deleteDataProfile, dbExecutionItem, dbExecutionTimeout);
									resultsSql = @"select [ItemNumber], [ExecutionItemUid], AssetUid as [uid], StartDate, EndDate, [Cascade], [Message], [Success] from api.ExecutionDeleteAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PostResponsibilityOverride:
									var postResponsibilityOverride = await Storage.DeserializeJsonObjectFromBlobAsync<List<BulkResponsibilityOverridePostModel>>(info.StorageFolder, info.RequestFileName);
									await company.BulkInsertResponsibilityOverrideAsync(postResponsibilityOverride, dbExecutionItem, dbExecutionTimeout);
									resultsSql = @"select [ItemNumber], AssetUid, [ExecutionItemUid], [Message], [Success] from api.ExecutionResponsibilityTypeRelationOverrideItem where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.DeleteFieldTypes:
									var deleteFieldtypes = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteFieldtypes>(dbExecutionItem.Fields);

									company.SetApiExecutionProcessingStartTime(dbExecutionItem.ExecutionID);
									List<FieldType> currentFieldTypes = fieldsRepository.GetFieldTypes(deleteFieldtypes.TypeIdentifierInfo);
									var result = fieldsRepository.DeleteFields(currentFieldTypes, deleteFieldtypes.FieldNamesToDelete);
									dbExecutionItem.Processed = result;
									dbExecutionItem.CompletedOn = DateTime.UtcNow;
									await markExecutionAsCompleteForFieldType(dbExecutionItem, company);
									break;
								case ApiExecutionAction.UpsertUsers:
									UserUpsertModel model = await Storage.DeserializeJsonObjectFromBlobAsync<UserUpsertModel>(info.StorageFolder, info.RequestFileName);
									await membershipRepository.ProcessUpsertUsers(dbExecutionItem, model.Users, model.LookupFieldsPassedByValue, model.IsInsert, false).ConfigureAwait(false);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionUser where ExecutionID = @executionId order by ItemNumber asc";
									break;
								case ApiExecutionAction.PatchCatalog:
									var execRepo = new ExecutionsRepository(company, Queue, Storage, FeatureFlags);
									log.LogTrace($"Get PatchCatalog payload: {DateTime.UtcNow:hh:mm:ss}");
									var patchCatalogPayload = await Storage.DeserializeJsonObjectFromBlobAsync<PatchBulkCatalogRequestModel>(info.StorageFolder, info.RequestFileName);

									log.LogTrace($"PatchCatalog: {DateTime.UtcNow:hh:mm:ss}");
									await execRepo.PatchCatalog(dbExecutionItem.Id, patchCatalogPayload);
									resultsSql = @"select iif([Type] = 'A', 'Asset', 'Relation') as [Type], TypeSourceId, SourceId, SubjectSourceId, ObjectSourceId, [Message], [Success], cast(iif([Action] = 'A', 1, 0) as bit) as IsNew from api.ExecutionCatalogItem where ExecutionId = @Id order by [Type] asc";

									Queue.CreateMessage(company.AssetGraphQueue, new PostExecutionQueueMessage { Action = PostExecutionQueueMessageAction.History, CompanyID = info.CompanyID, ExecutionId = dbExecutionItem.Id });
									Queue.CreateMessage(company.AssetGraphQueue, new PostExecutionQueueMessage { Action = PostExecutionQueueMessageAction.Indexing, CompanyID = info.CompanyID, ExecutionId = dbExecutionItem.Id });
									Queue.CreateMessage(company.ScoringQueue, new ScoreQueueInfo
									{
										ChangeType = ScoreQueueChangeType.PatchCatalogExecution,
										CompanyID = info.CompanyID,
										ExecutionId = dbExecutionItem.Id,
										ResourceID = info.ResourceID,
										StartedOn = DateTime.UtcNow
									});
									break;
								default:
									resultsSql = "";
									break;
							}

							if (!string.IsNullOrEmpty(resultsSql))
							{
								var results = await company.Connection.QueryAsync<dynamic>(resultsSql, new { executionId = dbExecutionItem.ExecutionID, dbExecutionItem.Id }, commandTimeout: 540);
								await Storage.SerializeJsonObjectToBlobAsync(info.StorageFolder, info.ResponseFileName, results);
								if (action == ApiExecutionAction.PatchCatalog)
								{
									company.CompleteApiExecutionAndGetCounts(dbExecutionItem.Id, action);
								}
								else
								{
									company.CompleteApiExecutionAndGetCounts(dbExecutionItem.ExecutionID, action);
								}
							}
						}
					}
					else
					{
						// this is the case where the batch job has been started however no record can be found in the api execution table for the execution id.  Log it
						log.LogWarning($"Cannot find [api].[execution] record for batch ExecutionID:{(info != null ? info.ExecutionID.ToString() : "unknown execution id")}");
					}
				}
				catch (Exception ex)
				{
					int delaySeconds = int.Parse(Configuration["RunningJobDelay"] ?? "30");
					int maxRetryCount = 20;

					try
					{
						// We open a new connection here because we can run into issues with the EF context object where the underlying connection object is in an unstable state. 
						var companyConnectionString = community.GetCompanyConnectionString(true);
						using (var exceptionConnection = new SqlConnection(companyConnectionString))
						{
							if (dbExecutionItem.RetryCount >= maxRetryCount)
							{
								string errorMessage = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
								dbExecutionItem.ErrorMessage = errorMessage;
								dbExecutionItem.CompletedOn = DateTime.UtcNow;
							}
							await exceptionConnection.OpenAsync();
							await exceptionConnection.ExecuteAsync("update api.Execution set ErrorMessage = @ErrorMessage, CompletedOn = @CompletedOn where ExecutionID = @ExecutionID", dbExecutionItem);
						}
					}
					catch (Exception cex)
					{
						log.LogError(cex, "Error in {FUNCTION_NAME}, on try/catch retry connection attempt.", FUNCTION_NAME);
					}

					if (dbExecutionItem.RetryCount < maxRetryCount)
					{
						log.LogWarning(ex, "Currently on Retry {RetryCount}", dbExecutionItem.RetryCount);
						TimeSpan delay = new TimeSpan(0, 0, delaySeconds * dbExecutionItem.RetryCount.Value); // Incremental backoff.
						await Queue.CreateMessageAsync(company.ApiExecutionQueue, info, delay);
					}
					else
					{
						log.LogCritical(ex, "Error after retries. Stopped on Retry {RetryCount}", dbExecutionItem.RetryCount);
					}

					return;
				}
			}
		}

		private async Task markExecutionAsProcessing(ApiExecution dbExecutionItem, CompanyContext company)
		{
			dbExecutionItem.ProcessingStartedOn = DateTime.UtcNow;
			dbExecutionItem.RetryCount = dbExecutionItem.RetryCount.HasValue ? dbExecutionItem.RetryCount.Value + 1 : 0;
			await company.Connection.ExecuteAsync(
				"update api.Execution set ProcessingStartedOn = @ProcessingStartedOn, RetryCount = @RetryCount, ErrorMessage = null where Id = @Id",
				dbExecutionItem);
		}

		private async Task markExecutionAsCompleteForFieldType(ApiExecution dbExecutionItem, CompanyContext company)
		{
			await company.Connection.ExecuteAsync(
				"update api.Execution set Processed = @Processed, CompletedOn = @CompletedOn where Id = @Id",
				dbExecutionItem);
		}

		private async Task markExecutionAsErred(ApiExecution dbExecutionItem, string err, CompanyContext company)
		{
			dbExecutionItem.ErrorMessage = err;
			dbExecutionItem.CompletedOn = DateTime.UtcNow;
			await company.Connection.ExecuteAsync(
				"update api.Execution set ErrorMessage = @ErrorMessage, CompletedOn = @CompletedOn where Id = @Id",
				new { dbExecutionItem.ErrorMessage, dbExecutionItem.CompletedOn, dbExecutionItem.Id });
		}

	}
}
