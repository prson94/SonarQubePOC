using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace d360.model.DataAccessLayer
{
	public class ExecutionsRepository : BaseRepository, IExecutionsRepository
	{
		internal ICompanyContext CompanyContext;
		internal IQueueSource QueueSource;
		internal IStorageProvider StorageProvider;

		public ExecutionsRepository(
			ICompanyContext companyContext,
			IQueueSource queueSource,
			IStorageProvider storageProvider)
			: base(companyContext)
		{
			CompanyContext = companyContext;
			QueueSource = queueSource;
			StorageProvider = storageProvider;
		}

		public async Task<ApiExecutionInfo> BulkPatchAssetAndRelations(PatchBulkCatalogRequestModel payload)
		{
			var execution = new ApiExecution
			{
				ResourceID = CompanyContext.CurrentResourceID,
				Action = ApiExecutionAction.PatchCatalog,
				Total = 0,
				StartedOn = DateTime.UtcNow,
			};

			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = CompanyContext.CurrentCompanyID,
				CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
				ExecutionID = Guid.NewGuid(),
				ResourceID = execution.ResourceID,
				SendWorkflowEvents = true
			};

			// Save to storage container.
			await StorageProvider.CreateFile(
				executionInfo.StorageFolder,
				executionInfo.RequestFileName,
				JsonConvert.SerializeObject(payload)
			);

			// Save to the database.
			execution.ExecutionID = executionInfo.ExecutionID;
			CompanyContext.Add(execution);

			// Save to queue.
			if (!await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
			{
				throw new ApplicationException(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
			}

			return executionInfo;
		}

		public ApiExecution GetExecutionItemByUid(Guid executionUid)
		{
			return CompanyContext.Filter<ApiExecution>(i => i.ExecutionID == executionUid).SingleOrDefault();
		}

		public List<APIExecutionErrorApiModel> GetExecutionErrorsByUid(Guid executionUid)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("executionUid", executionUid);
			return CompanyContext.Database.Connection.Query<APIExecutionErrorApiModel>(
				@$"select * from api.ExecutionAssetError EAE where ExecutionID = @executionUid
				union
				select * from api.ExecutionRelationshipError ERE where ExecutionID = @executionUid", dbArgs).ToList();
		}

		public async Task<APIExecutionAPIModelResult> GetExecutions(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			string orderDirection = "asc";
			string filterSql = "";
			if (queryParams.Any(x => x.Key == "_direction"))
			{
				var allowedDirections = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };
				var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim();
				if (!allowedDirections.Contains(order))
				{
					return new APIExecutionAPIModelResult
					{
						Message = AssetTypeErrors.InvalidDirection,
						StatusCode = HttpStatusCode.BadRequest
					};
				}
				orderDirection = order;
			}

			string orderBySql = "";
			if (!queryParams.Any(p => p.Key == "_order"))
			{
				orderBySql = $" order by [CompletedOn] {orderDirection} ";
			}
			else
			{

				var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
				var validOrderByFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
					"executionid", "resourceuid", "resource", "total",
					"processed", "error", "errormessage", "processingstartedon",
					"startedon", "completedon", "method", "route", "fields", "applicationid" };
				if (!validOrderByFields.Contains(orderByCol))
				{
					return new APIExecutionAPIModelResult
					{
						Message = AssetTypeErrors.InvalidOrderPassed,
						StatusCode = HttpStatusCode.BadRequest
					};
				}

				orderBySql = $" order by [{orderByCol}] {orderDirection} ";
			}

			int pageNum = 1;
			if (queryParams.Any(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)))
			{
				int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)).Value, out pageNum);
			}

			int pageSize = 200;
			if (queryParams.Any(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)))
			{
				int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)).Value, out pageSize);
			}

			if (pageSize > 0 || pageNum > 0)
			{
				if (pageSize < 1)
				{
					pageSize = 1;
				}

				if (pageNum < 1)
				{
					pageNum = 1;
				}

				if (pageSize > 25000)
				{
					pageSize = 25000;
				}

				if (pageNum > 10000)
				{
					pageNum = 10000;
				}
			}

			if (queryParams.Any(p => p.Key == "_status"))
			{
				if (Enum.TryParse(queryParams.FirstOrDefault(p => p.Key == "_status").Value, out ExecutionInternalStatus status))
				{
					switch (status)
					{
						case ExecutionInternalStatus.Pending:
							filterSql = "WHERE Ex.CompletedOn IS NULL AND Ex.ProcessingStartedOn IS NULL";
							break;
						case ExecutionInternalStatus.Running:
							filterSql = "WHERE Ex.CompletedOn IS NULL AND Ex.ProcessingStartedOn IS NOT NULL";
							break;
						default:    // Basically, ExecutionInternalStatus.Completed
							filterSql = "WHERE Ex.CompletedOn IS NOT NULL";
							break;

					}
				}
			}

			var sql = $@"
						SELECT Ex.[ExecutionID]
							  ,GR.[uid] as ResourceUid
							  ,CONCAT(GR.[FirstName],' ', GR.[LastName]) as [Resource]
							  ,[Total]
							  ,[Processed]
							  ,[Error]
							  ,coalesce(ERR.[Message],ex.errormessage) as ErrorMessage
							  ,[ProcessingStartedOn] 
							  ,[StartedOn] 
							  ,[CompletedOn]
							  ,[Method]
							  ,[Route]
							  ,[Fields]
							  ,[ApplicationId]
						  FROM [api].[Execution] Ex
						  INNER JOIN [reporting].[Global_Resource] GR on GR.ResourceID = Ex.ResourceID  
						  LEFT JOIN [api].[ExecutionAssetError] ERR on ERR.[ExecutionID] = Ex.[ExecutionID] 
						  {filterSql}
						  {orderBySql}
						  offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only
						";

			var countSQL = $@"
						SELECT count(*)
						  FROM [api].[Execution] Ex
						  INNER JOIN [reporting].[Global_Resource] GR on GR.ResourceID = Ex.ResourceID 
						  LEFT JOIN [api].[ExecutionAssetError] ERR on ERR.[ExecutionID] = Ex.[ExecutionID]
						  {filterSql}
						";

			var multiSQL = $"{sql}; {countSQL}";
			using (var multi = await CompanyContext.QueryMultipleAsync(multiSQL, null, ApiTimeout))
			{
				var executions = multi.Read<dynamic>().ToList();
				var count = multi.Read<int>().First();

				var items = executions.Select(x =>
				{
					var f = string.IsNullOrEmpty(x.Fields) ? "{}" : x.Fields;
					return new APIExecutionAPIModel
					{
						CompletedOn = x.CompletedOn,
						Error = x.Error,
						ErrorMessage = x.ErrorMessage,
						ExecutionID = x.ExecutionID,
						Fields = JsonConvert.DeserializeObject<dynamic>(f),
						Method = x.Method,
						Processed = x.Processed,
						ProcessingStartedOn = x.ProcessingStartedOn,
						Resource = x.Resource,
						ResourceUid = x.ResourceUid,
						Route = x.Route,
						StartedOn = x.StartedOn,
						Total = x.Total,
						ApplicationId = x.ApplicationId
					};
				});

				var resultsModel = new APIExecutionAPIModelResult
				{
					items = items,
					total = count,
					pageNum = pageNum,
					pageSize = pageSize,
					StatusCode = HttpStatusCode.OK
				};

				return resultsModel;
			}
		}

		public async Task<EndpointPayloadResponse<dynamic>> GetExecutionStatus(Guid executionUid, bool includeResults = true, bool includeProcessingDetail = false)
		{
			var response = new EndpointPayloadResponse<dynamic>();
			var dbExecutionItem = GetExecutionItemByUid(executionUid);

			if (dbExecutionItem == null)
			{
				response.Code = HttpStatusCode.NotFound;
				response.Message = AssetTypeErrors.ExecutionUIDNotFound;
				response.Payload = null;
				return response;
			}
			var errors = GetExecutionErrorsByUid(executionUid);

			if (errors.Count > 0)
			{
				List<string> err = new List<string>();
				if (!string.IsNullOrEmpty(dbExecutionItem.ErrorMessage))
				{
					err.Add(dbExecutionItem.ErrorMessage);
				}
				err.AddRange(errors.Select(x => x.Message));
				dbExecutionItem.Total += errors.Count;
				dbExecutionItem.Error += errors.Count;
				dbExecutionItem.ErrorMessage = string.Join("; ", err);
			}


			var info = new ApiExecutionInfo { CompanyID = CompanyContext.CurrentCompanyID, ExecutionID = executionUid };

			List<dynamic> results = null;

			if (includeResults && dbExecutionItem.CompletedOn.HasValue)
			{
				int attempt = 0;
				int maxAttempt = 5;
				while (attempt <= maxAttempt && results == null) {
					try
					{
						results = await StorageProvider.DeserializeJsonObjectFromBlobAsync<List<dynamic>>(info.StorageFolder, info.ResponseFileName);
					}
					catch
					{
						if (attempt >= maxAttempt) {
							results = new List<dynamic> { new { Message = "Results file no longer exists." } };
						}
						Thread.Sleep(1000);
						attempt++;
					}
				}
			}

			var f = string.IsNullOrEmpty(dbExecutionItem.Fields) ? "{}" : dbExecutionItem.Fields;

			response.Code = HttpStatusCode.OK;
			if (includeProcessingDetail)
			{
				response.Payload = new
				{
					dbExecutionItem.Total,
					dbExecutionItem.Processed,
					dbExecutionItem.Error,
					dbExecutionItem.ErrorMessage,
					Fields = JsonConvert.DeserializeObject<dynamic>(f),
					dbExecutionItem.StartedOn,
					dbExecutionItem.ProcessingStartedOn,
					dbExecutionItem.CompletedOn,
					Results = results
				};
			}
			else
			{
				response.Payload = new
				{
					dbExecutionItem.Total,
					dbExecutionItem.Processed,
					dbExecutionItem.Error,
					Fields = JsonConvert.DeserializeObject<dynamic>(f),
					dbExecutionItem.StartedOn,
					dbExecutionItem.CompletedOn,
					Results = results
				};
			}

			return response;
		}

		public async Task PatchCatalog(int executionId, PatchBulkCatalogRequestModel payload)
		{
			DataTable assetTable;
			DataTable assetPropertyTable;
			DataTable relationTable;
			DataTable relationPropertyTable;

			#region Data Table Generation

			assetTable = new DataTable();
			assetTable.Columns.Add("ExecutionId", typeof(int));
			assetTable.Columns.Add("Type", typeof(string));
			assetTable.Columns.Add("TypeSourceId", typeof(string));
			assetTable.Columns.Add("SourceId", typeof(string));
			assetTable.Columns.Add("IsDelete", typeof(bool));

			relationTable = new DataTable();
			relationTable.Columns.Add("ExecutionId", typeof(int));
			relationTable.Columns.Add("Type", typeof(string));
			relationTable.Columns.Add("TypeSourceId", typeof(string));
			relationTable.Columns.Add("SubjectSourceId", typeof(string));
			relationTable.Columns.Add("ObjectSourceId", typeof(string));

			assetPropertyTable = new DataTable();
			assetPropertyTable.Columns.Add("ExecutionId", typeof(int));
			assetPropertyTable.Columns.Add("Type", typeof(string));
			assetPropertyTable.Columns.Add("TypeSourceId", typeof(string));
			assetPropertyTable.Columns.Add("SourceId", typeof(string));
			assetPropertyTable.Columns.Add("Name", typeof(string));
			assetPropertyTable.Columns.Add("Value", typeof(string));

			relationPropertyTable = new DataTable();
			relationPropertyTable.Columns.Add("ExecutionId", typeof(int));
			relationPropertyTable.Columns.Add("Type", typeof(string));
			relationPropertyTable.Columns.Add("TypeSourceId", typeof(string));
			relationPropertyTable.Columns.Add("SubjectSourceId", typeof(string));
			relationPropertyTable.Columns.Add("ObjectSourceId", typeof(string));
			relationPropertyTable.Columns.Add("Name", typeof(string));
			relationPropertyTable.Columns.Add("Value", typeof(string));

			if (payload.Assets == null && payload.Relations == null)
			{
				throw new ApplicationException("Neither asset collection nor relation collection was provided. At least one collection must be provided.");
			}
			else
			{
				if (payload.Assets != null)
				{
					payload.Assets.ForEach(ag =>
					{
						ag.Items.ForEach(a =>
						{
							DataRow row = assetTable.NewRow();
							row["ExecutionId"] = executionId;
							row["Type"] = 'A';
							row["TypeSourceId"] = ag.AssetTypeSourceId;
							row["SourceId"] = a.SourceId;
							row["IsDelete"] = a.IsFullRefresh;
							assetTable.Rows.Add(row);

							if (a.Properties != null)
							{
								a.Properties.ForEach(p =>
								{
									DataRow propertyRow = assetPropertyTable.NewRow();
									propertyRow["ExecutionId"] = executionId;
									propertyRow["Type"] = 'A';
									propertyRow["TypeSourceId"] = ag.AssetTypeSourceId;
									propertyRow["SourceId"] = a.SourceId;
									propertyRow["Name"] = p.Name;
									propertyRow["Value"] = p.Value;
									assetPropertyTable.Rows.Add(propertyRow);
								});
							}

						});
					});
				}

				if (payload.Relations != null)
				{
					payload.Relations.ForEach(rg =>
					{
						rg.Items.ForEach(r =>
						{
							DataRow row = relationTable.NewRow();
							row["ExecutionId"] = executionId;
							row["Type"] = 'R';
							row["TypeSourceId"] = rg.RelationTypeSourceId;
							row["SubjectSourceId"] = r.SubjectSourceId;
							row["ObjectSourceId"] = r.ObjectSourceId;
							relationTable.Rows.Add(row);

							if (r.Properties != null)
							{
								r.Properties.ForEach(p =>
								{
									DataRow propertyRow = relationPropertyTable.NewRow();
									propertyRow["ExecutionId"] = executionId;
									propertyRow["Type"] = 'R';
									propertyRow["TypeSourceId"] = rg.RelationTypeSourceId;
									propertyRow["SubjectSourceId"] = r.SubjectSourceId;
									propertyRow["ObjectSourceId"] = r.ObjectSourceId;
									propertyRow["Name"] = p.Name;
									propertyRow["Value"] = p.Value;
									relationPropertyTable.Rows.Add(propertyRow);
								});
							}
						});
					});
				}
			}

			#endregion Data Table Generation

			var resourceId = CompanyContext.CurrentResourceID;
			var date = DateTime.UtcNow;

			var executionProcessingInfoFields = CompanyContext.ApiExecutions.Single(x => x.Id == executionId).Fields ?? "{}";
			var executionProcessingInfo = JsonConvert.DeserializeObject<ApiExecutionFields_PatchExecution>(executionProcessingInfoFields);

			while (executionProcessingInfo.RetryCount < 10 && executionProcessingInfo.LastCompletedStepNumber < 8)
			{
				try
				{
					await CompanyContext.Connection.OpenIfClosed();

					if (executionProcessingInfo.LastCompletedStepNumber <= 0)
					{
						using (SqlBulkCopy bulkCopy = CompanyContext.Connection.CreateBulkCopy("api.ExecutionCatalogItem", 5000, 3600))//, transaction))
						{
							bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
							bulkCopy.ColumnMappings.Add("Type", "Type");
							bulkCopy.ColumnMappings.Add("TypeSourceId", "TypeSourceId");
							bulkCopy.ColumnMappings.Add("SourceId", "SourceId");
							bulkCopy.ColumnMappings.Add("IsDelete", "IsDelete");
							bulkCopy.WriteToServer(assetTable);

							bulkCopy.ColumnMappings.Clear();

							bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
							bulkCopy.ColumnMappings.Add("Type", "Type");
							bulkCopy.ColumnMappings.Add("TypeSourceId", "TypeSourceId");
							bulkCopy.ColumnMappings.Add("SubjectSourceId", "SubjectSourceId");
							bulkCopy.ColumnMappings.Add("ObjectSourceId", "ObjectSourceId");
							bulkCopy.WriteToServer(relationTable);
						}
						executionProcessingInfo.LastCompletedStepNumber = 1;
					}

					if (executionProcessingInfo.LastCompletedStepNumber < 2)
					{
						using (SqlBulkCopy bulkCopy = CompanyContext.Connection.CreateBulkCopy("api.ExecutionCatalogItemProperty", 5000, 7200))
						{
							bulkCopy.ColumnMappings.Add("TypeSourceId", "TypeSourceId");        //0
							bulkCopy.ColumnMappings.Add("Name", "Name");                        //1
							bulkCopy.ColumnMappings.Add("Value", "Value");                      //2
							bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
							bulkCopy.ColumnMappings.Add("Type", "Type");
							bulkCopy.ColumnMappings.Add("SourceId", "SourceId");                //3
							bulkCopy.WriteToServer(assetPropertyTable);

							bulkCopy.ColumnMappings.Clear();

							bulkCopy.ColumnMappings.Add("TypeSourceId", "TypeSourceId");        //0
							bulkCopy.ColumnMappings.Add("Name", "Name");                        //1
							bulkCopy.ColumnMappings.Add("Value", "Value");                      //2
							bulkCopy.ColumnMappings.Add("ExecutionId", "ExecutionId");
							bulkCopy.ColumnMappings.Add("Type", "Type");
							bulkCopy.ColumnMappings.Add("SubjectSourceId", "SubjectSourceId");  //3
							bulkCopy.ColumnMappings.Add("ObjectSourceId", "ObjectSourceId");    //4
							bulkCopy.WriteToServer(relationPropertyTable);
						}
						executionProcessingInfo.LastCompletedStepNumber = 2;
					}

					// Non-delete of assets.
					if (executionProcessingInfo.LastCompletedStepNumber < 3)
					{
						await CompanyContext.Connection.ExecuteAsync("exec PatchCatalog @executionId, @step, @resourceId, @date", new { executionId, step = 'A', resourceId, date }, commandTimeout: 7200);
						executionProcessingInfo.LastCompletedStepNumber = 3;
					}

					// Relations.
					if (executionProcessingInfo.LastCompletedStepNumber < 4)
					{
						await CompanyContext.Connection.ExecuteAsync("exec PatchCatalog @executionId, @step, @resourceId, @date", new { executionId, step = 'R', resourceId, date }, commandTimeout: 7200);
						executionProcessingInfo.LastCompletedStepNumber = 4;
					}

					// Delete, if any, of assets
					if (executionProcessingInfo.LastCompletedStepNumber < 5)
					{
						await CompanyContext.Connection.ExecuteAsync("exec PatchCatalog @executionId, @step, @resourceId, @date", new { executionId, step = 'D', resourceId, date }, commandTimeout: 7200);
						executionProcessingInfo.LastCompletedStepNumber = 5;
					}

					// Path/display value processing.
					if (executionProcessingInfo.LastCompletedStepNumber < 6)
					{
						await CompanyContext.Connection.ExecuteAsync("exec PatchCatalog @executionId, @step, @resourceId, @date", new { executionId, step = 'P', resourceId, date }, commandTimeout: 7200);
						executionProcessingInfo.LastCompletedStepNumber = 6;
					}

					// Cleanup and wrap-up of run.
					if (executionProcessingInfo.LastCompletedStepNumber < 7)
					{
						await CompanyContext.Connection.ExecuteAsync("exec PatchCatalog @executionId, @step, @resourceId, @date", new { executionId, step = 'E', resourceId, date }, commandTimeout: 7200);
						executionProcessingInfo.LastCompletedStepNumber = 7;
					}
				}
				catch (Exception ex)
				{
					executionProcessingInfo.RetryCount++;
					Thread.Sleep(10000); // 10 second pause.

					if (executionProcessingInfo.RetryCount >= 10)
					{
						executionProcessingInfoFields = JsonConvert.SerializeObject(executionProcessingInfo);

						await CompanyContext.Connection.ExecuteAsync(
							"update api.Execution set [State] = 4, ErrorMessage = @m, CompletedOn = @dt, MarkedForProcessing = 0, Fields = @fields where Id = @executionId",
							new
							{
								executionId,
								dt = DateTime.UtcNow,
								fields = executionProcessingInfoFields,
								m = ex.GetFullExceptionData(false, 2450)
							}
						);
					}
				}
			} // while
		}

		public void UpsertExecution(ApiExecution execution)
		{
			if (execution.ExecutionID == Guid.Empty)
			{
				execution.ExecutionID = Guid.NewGuid();
			}

			if (execution.Id == 0)
			{
				// Insert
				CompanyContext.Add(execution);
			}
			else
			{
				// Update
				CompanyContext.Update(execution);
			}
		}
	}
}
