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
using DocumentFormat.OpenXml.ExtendedProperties;
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

		const int DEFAULT_SQL_BULK_COPY_BLOCK_SIZE = 5000;
		const int DEFAULT_SQL_BULK_COPY_TIMEOUT = 0;
		const int DEFAULT_WORKFLOW_BATCH_SIZE = 50;

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly IStorageProvider Storage;
		readonly IFeatureFlagService FeatureFlags;

		public ExecutionProcessor(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue, IStorageProvider storage, IFeatureFlagService ff) : base(community, config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
			Storage = storage;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger(constants.Queue.Execution, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
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
				var context = new UriSecurityContextProvider
				{
					CompanyID = info.CompanyID,
					ResourceID = info.ResourceID ?? 0,
					CompanyPrefix = info.CompanyDomainPrefix,
					IsAdministrator = false
				};
				var connectionString = Community.GetConnectionStringForTenant(info.CompanyID);
				
				using (var company = new CompanyContext(Cache, Queue, Mail, context, log, new TenantConnectionInfo { ConnectionString = connectionString }))
				{
					var resource = company.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == context.ResourceID);
					if (resource != null)
					{
						context.IsAdministrator = resource.IsAdministrator;
					}

					var fieldsRepository = new FieldsRepository(company, context, Queue, Storage, FeatureFlags);
					var assetRepository = new AssetRepository(company, context, Queue, Storage, FeatureFlags);
					var relationshipRepository = new RelationshipRepository(company, context, Queue, Storage, FeatureFlags);
						
					var dbExecutionItem = company.Connection.Query<ApiExecution>("select * from api.Execution where ExecutionID = @ExecutionID", new { info.ExecutionID }).SingleOrDefault();
					List<DatabaseBulkAssetResult> resultdata = new List<DatabaseBulkAssetResult>();
					try
					{
						if (dbExecutionItem != null)
						{
							int dbExecutionTimeout = 10800;
							company.SqlBulkBatchSize = DEFAULT_SQL_BULK_COPY_BLOCK_SIZE;
							company.SqlBulkBatchTimeout = DEFAULT_SQL_BULK_COPY_TIMEOUT;
							company.WorkflowSendBatchSize = DEFAULT_WORKFLOW_BATCH_SIZE;

							bool executeJob = (dbExecutionItem.State != d360.core.enums.State.Deleted);

							if (executeJob)
							{
								var dapperProvider = new DapperConnectionProvider { 
									ReadOnlyConnectionString = $"{connectionString}ApplicationIntent=ReadOnly",
									ReadWriteConnectionString = $"{connectionString}ApplicationIntent=ReadWrite"
								};

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
											resultdata = assetRepository.PostAssets(postAssets, assetType, dbExecutionItem, sendWorkflowEvents: info.SendWorkflowEvents, false);

											await processLoadBulkTagging(dbExecutionItem, assetType.ID, company, context, log);

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
											resultdata = assetRepository.PutAssets(putAssets, assetType, dbExecutionItem, sendWorkflowEvents: info.SendWorkflowEvents, false);

											await processLoadBulkTagging(dbExecutionItem, assetType.ID, company, context, log);

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
											resultdata = assetRepository.DeleteAssets(deleteAssets, assetType, dbExecutionItem, true);
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
											relationshipRepository.PostRelationships(intersectType, dbExecutionItem, postRelationships, sendWorkflowEvents: info.SendWorkflowEvents);
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
										ICatalog catalog = new Catalog(dapperProvider);
										var postCrossReferences = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetCrossReference>>(info.StorageFolder, info.RequestFileName);
										await catalog.CreateCrossReferencesAsync(dbExecutionItem, postCrossReferences, dbExecutionTimeout);
										resultsSql = @"select [ItemNumber], [uid], [Message], [Success] from api.ExecutionAssetCrossReference where ExecutionID = @executionId order by ItemNumber asc";
										break;
									case ApiExecutionAction.PostDataQualityResults:
										var metricsRepository = new MetricsRepository(company, context, Queue, Storage, FeatureFlags);
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
										var workspace = new Workspaces(dapperProvider);
										UserUpsertModel model = await Storage.DeserializeJsonObjectFromBlobAsync<UserUpsertModel>(info.StorageFolder, info.RequestFileName);
										var userResponse = await workspace.UpsertUsersAsync(dbExecutionItem.Id, model.Users.ToList(), model.LookupFieldsPassedByValue);
										await Storage.SerializeJsonObjectToBlobAsync(info.StorageFolder, info.ResponseFileName, userResponse.Data);
										resultsSql = "";// @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionUser where ExecutionID = @executionId order by ItemNumber asc";
										break;
									case ApiExecutionAction.PatchCatalog:
										var execRepo = new ExecutionsRepository(company, context, Queue, Storage, FeatureFlags);
										log.LogTrace($"Get PatchCatalog payload: {DateTime.UtcNow:hh:mm:ss}");
										var patchCatalogPayload = await Storage.DeserializeJsonObjectFromBlobAsync<PatchBulkCatalogRequestModel>(info.StorageFolder, info.RequestFileName);

										log.LogTrace($"PatchCatalog: {DateTime.UtcNow:hh:mm:ss}");
										await execRepo.PatchCatalog(dbExecutionItem.Id, patchCatalogPayload);
										resultsSql = @"select iif([Type] = 'A', 'Asset', 'Relation') as [Type], TypeSourceId, SourceId, SubjectSourceId, ObjectSourceId, [Message], [Success], cast(iif([Action] = 'A', 1, 0) as bit) as IsNew from api.ExecutionCatalogItem where ExecutionId = @Id order by [Type] asc";

										Queue.CreateMessage(constants.Queue.PostExecution, new PostExecutionQueueMessage { Action = PostExecutionQueueMessageAction.History, CompanyID = info.CompanyID, ExecutionId = dbExecutionItem.Id });
										Queue.CreateMessage(constants.Queue.PostExecutionIndex, new PostExecutionQueueMessage { CompanyID = info.CompanyID, ExecutionId = dbExecutionItem.Id });
										Queue.CreateMessage(constants.Queue.Score, new ScoreQueueInfo
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
									if (results.Count() == 0 && resultdata.Count > 0)
									{
										results = resultdata;
									}
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
							using (var exceptionConnection = new SqlConnection(connectionString))
							{
								if (dbExecutionItem.RetryCount >= maxRetryCount)
								{
									string errorMessage = ex.GetFullExceptionData(false, 2000);
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
							await Queue.CreateMessageAsync(constants.Queue.Execution, info, delay);
						}
						else
						{
							log.LogCritical(ex, "Error after retries. Stopped on Retry {RetryCount}", dbExecutionItem.RetryCount);
						}

						return;
					}
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

		private async Task processLoadBulkTagging(ApiExecution dbExecutionItem, int assetTypeId, ICompanyContext c, ISecurityContextProvider context, ILogger log)
		{
			try
			{
				var tagField = c.FieldTypes.FirstOrDefault(f => f.AssetTypeID == assetTypeId && f.Type == "Tag");
				var load = c.Loads.FirstOrDefault(l => l.PutExecutionID == dbExecutionItem.ExecutionID || l.PostExecutionID == dbExecutionItem.ExecutionID);
				if (load != null && tagField != null)
				{
					var loadHasTagField = c.LoadColumns.Any(l => l.LoadID == load.ID && l.Name == tagField.Name);
					if (loadHasTagField)
					{
						log.LogTrace($"Processing execution {dbExecutionItem.ExecutionID} for load {load.ID}");
						var bulkTags = await c.GetBulkTagAssetsAsync(load.ID, dbExecutionItem.ExecutionID);
						if (bulkTags.Any())
						{
							var repo = new TagRepository(c, context, FeatureFlags, Queue);
							await repo.BulkTagAssets(bulkTags, load.UpdatedBy ?? 0);
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error in {FUNCTION_NAME}, on try/catch retry connection attempt.", FUNCTION_NAME);
			}
		}
	}
}
