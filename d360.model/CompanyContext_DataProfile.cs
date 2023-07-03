using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext
	{
		#region DbSets

		DbSet<AssetDataProfile> AssetDataProfile { get; set; }
		
		DbSet<AssetDataProfileSample> AssetDataProfileSample { get; set; }
		
		DbSet<AssetDataProfileSampleJson> AssetDataProfileSampleJson { get; set; }

		#endregion


		#region Methods

		Task DeleteDataProfilesAsync(List<AssetDataProfileDeleteModel> models, ApiExecution execution, int timeout = 3600);

		Task<List<DataProfileUpsertResponse>> GetExecutionDataProfileResultsAsync(Guid executionId);

		Task<List<DataProfileDeleteResponse>> GetExecutionDeleteDataProfileResultsAsync(Guid executionId);

		Task UpsertDataProfilesAsync(List<DataProfileUpsertModel> request, ApiExecution execution, bool isInsert, int timeout = 3600);
		
		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region DbSets

		public DbSet<AssetDataProfile> AssetDataProfile { get; set; }

		public DbSet<AssetDataProfileSample> AssetDataProfileSample { get; set; }

		public DbSet<AssetDataProfileSampleJson> AssetDataProfileSampleJson { get; set; }

		#endregion


		#region Methods

		public async Task DeleteDataProfilesAsync(List<AssetDataProfileDeleteModel> models, ApiExecution execution, int timeout = 3600)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "DeleteDataProfiles";
			bool isLog = true; // trace info for all assets is extermely useful            

			bool generalChecksCompleted = false;
			int itemNumber = 1;
			CurrentExecutionLocationModel currentLocation = null;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			Stopwatch sw = Stopwatch.StartNew();
			int step = 0;

			var dups = models.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			addMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			if (dups.Any())
			{
				string message = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
			}
			else
			{
				try
				{
					addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeleteAssetDataProfile");
					sw.Restart();

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("AssetUid", typeof(Guid));
					table.Columns.Add("StartDate", typeof(DateTime));
					table.Columns.Add("EndDate", typeof(DateTime));
					table.Columns.Add("Cascade", typeof(bool));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));

					foreach (AssetDataProfileDeleteModel item in models)
					{
						DataRow row = table.NewRow();
						List<string> errorMessages = new List<string>();

						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = itemNumber;
						if (item.ExecutionItemUid.HasValue)
						{
							row["ExecutionItemUid"] = item.ExecutionItemUid;
						}
						else
						{
							row["ExecutionItemUid"] = DBNull.Value;
						}
						row["AssetUid"] = item.AssetUid;
						row["StartDate"] = item.StartDate;

						if (item.StartDate == DateTime.MinValue)
						{
							errorMessages.Add("Startdate is a required field");
						}

						row["EndDate"] = item.EndDate;

						if (item.EndDate == DateTime.MinValue)
						{
							errorMessages.Add("EndDate is a required field");
						}

						if (errorMessages.Any())
						{
							row["Message"] = string.Join(";", errorMessages);
							row["Success"] = 0;
						}

						row["Cascade"] = item.Cascade;

						table.Rows.Add(row);

						itemNumber++;
					}

					#region Bulk Copy

					await Connection.OpenIfClosed();
					using (SqlBulkCopy bulkCopy = Connection.CreateBulkCopy("[api].[ExecutionDeleteAssetDataProfile]", table.Rows.Count, SqlBulkBatchTimeout))
					{
						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
						bulkCopy.ColumnMappings.Add("StartDate", "StartDate");
						bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
						bulkCopy.ColumnMappings.Add("Cascade", "Cascade");
						bulkCopy.ColumnMappings.Add("Message", "Message");
						bulkCopy.ColumnMappings.Add("Success", "Success");

						bulkCopy.WriteToServer(table);
					}

					addMeasurement(metrics, "BulkCopy to execution table", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					#endregion

					Connection.Execute($@"
						update	api.ExecutionDeleteAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
						where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	DEDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
						from
							api.ExecutionDeleteAssetDataProfile DEDP
							left Join
							Asset A on DEDP.AssetUid = A.Uid
						where	ExecutionID = @ExecutionID and A.Uid is null;

						update	api.ExecutionDeleteAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'StartDate must be before EndDate.'
						where	ExecutionID = @ExecutionID and startdate > enddate;

						declare @IsAdministrator bit = 0						
						select	@IsAdministrator = IsAdministrator
						from	reporting.Global_Resource
						where	ResourceID = @ResourceID

						IF(@IsAdministrator = 0)
						BEGIN
							update	EDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + '{CompanyContextApiError.DataProfilingNoPermission}'
							from	
									api.ExecutionDeleteAssetDataProfile EDP
							where 
									EDP.ExecutionID = @ExecutionID and not exists (
												select 1
												from	Asset A
														outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
														where	
														A.Uid = EDP.AssetUid 
														and
														(															
															(
																P.AssetID = A.ID
																or 
																P.AssetTypeID is null
															)
															OR
															(																	
																P.AssetID=0 
																and 
																P.AssetTypeID=A.AssetTypeID
															)
														)
														and 
														P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
												);
						END
",
									new { execution.ExecutionID, execution.ResourceID, p = Permission.EditAsset }, commandTimeout: timeout);

					addMeasurement(metrics, "LogDeleteAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = models.Count();
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					string querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";

					string sql = $@"
								drop table if exists #child
								create table #child (
									itemnumber int,
									assetID bigint,
									startDate datetime,
									endDate datetime
								)

								drop table if exists #parent
								create table #parent (
									itemnumber int,
									assetID bigint,
									startDate datetime,
									endDate datetime
								)

								drop table if exists #deleteAssetDataProfile
								create table #deleteAssetDataProfile (
									itemnumber int,
									assetID bigint,
									startDate datetime,
									endDate datetime
								)

								insert into #parent
								select 
									ItemNumber,
									ID,
									startdate,
									enddate
								from 
									Asset A
									inner join
									API.ExecutionDeleteAssetDataProfile E on A.uid = E.AssetUid and E.[Cascade] = 1
								Where
									{querySuffix}	                                

								insert into #deleteAssetDataProfile
								select * from #parent

								WHILE ((Select Count(*) from #parent) > 0)
								BEGIN
									insert into #child
									select 
										ItemNumber,
										AAP.ObjectAssetID as AssetID,
										p.startDate,
										p.endDate
									from 
										#parent P 
										inner join PredicateIntersect AAP on AAP.SubjectAssetID = P.AssetID and AAP.PredicateType in (3,4)

									delete from #parent 
	
									insert into #parent
									select * from #child

									insert into #deleteAssetDataProfile
									select 
										c.* 
									from 
										#child c 
										left join 
										#deleteAssetDataProfile a on c.assetID=a.assetID and a.startdate =c.startdate and a.enddate=c.enddate
									where a.assetID is null

									delete from #child
								END

								insert into #deleteAssetDataProfile
								select 
									ItemNumber,
									id,
									startdate,
									enddate
								from 
									Asset A
									inner join
									API.ExecutionDeleteAssetDataProfile E on A.uid = E.AssetUid and E.[Cascade] = 0
								where
									{querySuffix}	                                

								drop table if exists #deletedResults
								create table #deletedResults (
									itemnumber int,
									id bigint
								)

								merge AssetDataProfile as ADP
								using (select * from #deleteAssetDataProfile) DADP
								on DADP.assetID = ADP.AssetID and ADP.ProfileSetDate between DADP.startDate and DADP.endDate
								when matched then
								DELETE
								OUTPUT DADP.itemNumber, DELETED.ID into #deletedResults;

							
								Delete from AssetDataProfileSample where AssetDataProfileID in( select ID from #deletedResults dr where dr.ItemNumber between @beginItemNumber and @endItemNumber )
								Delete from AssetDataProfileSampleJson where AssetDataProfileID in( select ID from #deletedResults dr where dr.ItemNumber between @beginItemNumber and @endItemNumber )

								Update E
								set E.DeletedCount = DR.DeletedCount
								from 
								api.ExecutionDeleteAssetDataProfile E 
								cross apply (select itemNumber, Count(ID) as DeletedCount from #deletedResults DR where DR.itemnumber = E.itemNumber group by itemNumber) DR
								where 
								{querySuffix}";

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								#region Load valid items into table

								try
								{
									Connection.Query<KeyValuePair<long, long>>(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

									#endregion

									// Update success flag.
									Connection.Execute(
										$@"update E 
											set Success = 1 
									   From api.ExecutionDeleteAssetDataProfile E
									   where {querySuffix};",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;
								}
								catch (Exception ex)
								{
									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
										// Continue through loops, do not kill the entire process.
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										sw.Restart();
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeleteAssetDataProfile", ex.GetFullExceptionData(false), timeout);
										addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}
								}
							}
						}

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
						sw.Restart();
					}
				}

				addMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);
				addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);
			}

			CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionDeleteAssetDataProfile");
			Connection.CloseIfOpened();
		}

		public async Task<List<DataProfileUpsertResponse>> GetExecutionDataProfileResultsAsync(Guid executionId)
		{
			var sql = "select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
			var qry = await Connection.QueryAsync<DataProfileUpsertResponse>(sql, new { executionId });
			return qry.ToList();
		}

		public async Task<List<DataProfileDeleteResponse>> GetExecutionDeleteDataProfileResultsAsync(Guid executionId)
		{
			var qry = await Connection.QueryAsync<DataProfileDeleteResponse>(
				"select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success], DeletedCount from api.ExecutionDeleteAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc",
				new { executionId }
			);

			return qry.ToList();
		}

		public async Task UpsertDataProfilesAsync(List<DataProfileUpsertModel> request, ApiExecution execution, bool isInsert, int timeout = 3600)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "UpsertDataProfiles";
			bool isLog = true; // trace info for all assets is extermely useful

			bool generalChecksCompleted = false;
			int itemNumber = 1;
			CurrentExecutionLocationModel currentLocation = null;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			Stopwatch sw = Stopwatch.StartNew();
			int step = 0;

			var dups = request.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			var dupRecords = request.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			addMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

			sw.Restart();
			
			if (dups.Any() || dupRecords.Any())
			{
				if (dups.Any())
				{
					string message = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				}
				else
				{
					string message = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate}"))}. AssetUid and ProfileSetDate pairs are used as record identifiers and must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				}
			}
			else
			{
				try
				{
					addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetDataProfile");
					sw.Restart();

					#region Build data tables.

					DataTable DataProfileTable = new DataTable();
					DataTable DataProfileSampleTable = new DataTable();

					DataProfileTable.Columns.Add("ExecutionID", typeof(Guid));
					DataProfileTable.Columns.Add("ItemNumber", typeof(int));
					DataProfileTable.Columns.Add("ExecutionItemUid", typeof(Guid));
					DataProfileTable.Columns.Add("AssetUid", typeof(Guid));
					DataProfileTable.Columns.Add("ProfileSetDate", typeof(DateTime));
					DataProfileTable.Columns.Add("ProfileIdentifier", typeof(string));

					DataProfileTable.Columns.Add("UniqueCount", typeof(long));
					DataProfileTable.Columns.Add("SampleCount", typeof(long));
					DataProfileTable.Columns.Add("NullCount", typeof(long));
					DataProfileTable.Columns.Add("BlankCount", typeof(long));
					DataProfileTable.Columns.Add("MeanValue", typeof(double));
					DataProfileTable.Columns.Add("MinimumValue", typeof(string));

					DataProfileTable.Columns.Add("MaximumValue", typeof(string));
					DataProfileTable.Columns.Add("MinimumLength", typeof(int));
					DataProfileTable.Columns.Add("MaximumLength", typeof(int));
					DataProfileTable.Columns.Add("StandardDeviation", typeof(double));
					DataProfileTable.Columns.Add("Type", typeof(string));

					DataProfileTable.Columns.Add("Multiline", typeof(bool));
					DataProfileTable.Columns.Add("RegExp", typeof(string));
					DataProfileTable.Columns.Add("Confidence", typeof(decimal));
					DataProfileTable.Columns.Add("TypeQualifier", typeof(string));
					DataProfileTable.Columns.Add("LogicalType", typeof(bool));

					DataProfileTable.Columns.Add("LeadingWhiteSpace", typeof(bool));
					DataProfileTable.Columns.Add("LeadingZeroCount", typeof(int));
					DataProfileTable.Columns.Add("TrailingWhiteSpace", typeof(bool));

					DataProfileTable.Columns.Add("MatchCount", typeof(long));
					DataProfileTable.Columns.Add("OutlierCardinality", typeof(int));
					DataProfileTable.Columns.Add("DataSignature", typeof(string));

					DataProfileTable.Columns.Add("StructureSignature", typeof(string));
					DataProfileTable.Columns.Add("Cardinality", typeof(int));
					DataProfileTable.Columns.Add("ShapeCardinality", typeof(int));

					DataProfileTable.Columns.Add("TotalCount", typeof(long));
					DataProfileTable.Columns.Add("OutlierCount", typeof(long));
					DataProfileTable.Columns.Add("KeyConfidence", typeof(decimal));
					DataProfileTable.Columns.Add("DetectionLocale", typeof(string));
					DataProfileTable.Columns.Add("FtaVersion", typeof(string));
					DataProfileTable.Columns.Add("DecimalSeparator", typeof(string));

					DataProfileTable.Columns.Add("PopularityCount", typeof(long));
					DataProfileTable.Columns.Add("IsAuthorizedForPopularity", typeof(bool));
					DataProfileTable.Columns.Add("SourceLastModified", typeof(DateTime));
					DataProfileTable.Columns.Add("FilterCount", typeof(long));

					DataProfileSampleTable.Columns.Add("ExecutionID", typeof(Guid));
					DataProfileSampleTable.Columns.Add("ItemNumber", typeof(int));
					DataProfileSampleTable.Columns.Add("ExecutionItemUid", typeof(Guid));
					DataProfileSampleTable.Columns.Add("SampleType", typeof(string));
					DataProfileSampleTable.Columns.Add("Key", typeof(string));
					DataProfileSampleTable.Columns.Add("Value", typeof(string));
					DataProfileSampleTable.Columns.Add("JsonValue", typeof(string));

					#endregion

					#region Populate Data Tables

					foreach (DataProfileUpsertModel item in request)
					{
						DataRow row = DataProfileTable.NewRow();

						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = itemNumber;
						if (item.ExecutionItemUid.HasValue)
						{
							row["ExecutionItemUid"] = item.ExecutionItemUid;
						}
						else
						{
							row["ExecutionItemUid"] = DBNull.Value;
						}
						row["AssetUid"] = item.assetUid;
						row["ProfileSetDate"] = item.profileSetDate;
						row["ProfileIdentifier"] = item.profileIdentifier ?? (object)DBNull.Value;

						row["UniqueCount"] = item.uniqueCount ?? (object)DBNull.Value;
						row["SampleCount"] = item.sampleCount ?? (object)DBNull.Value;
						row["NullCount"] = item.nullCount ?? (object)DBNull.Value;
						row["BlankCount"] = item.blankCount ?? (object)DBNull.Value;
						row["MeanValue"] = item.meanValue ?? (object)DBNull.Value;
						row["MinimumValue"] = item.minValue ?? (object)DBNull.Value;

						row["MaximumValue"] = item.maxValue ?? (object)DBNull.Value;
						row["MinimumLength"] = item.minLength ?? (object)DBNull.Value;
						row["MaximumLength"] = item.maxLength ?? (object)DBNull.Value;
						row["StandardDeviation"] = item.standardDeviation ?? (object)DBNull.Value;
						row["Type"] = item.type ?? (object)DBNull.Value;

						row["Multiline"] = item.multiline ?? (object)DBNull.Value;
						row["RegExp"] = item.regExp ?? (object)DBNull.Value;
						row["Confidence"] = item.confidence ?? (object)DBNull.Value;
						row["TypeQualifier"] = item.typeQualifier ?? (object)DBNull.Value;
						row["LogicalType"] = item.logicalType ?? (object)DBNull.Value;

						row["LeadingWhiteSpace"] = item.leadingWhiteSpace ?? (object)DBNull.Value;
						row["LeadingZeroCount"] = item.leadingZeroCount ?? (object)DBNull.Value;
						row["TrailingWhiteSpace"] = item.trailingWhiteSpace ?? (object)DBNull.Value;
						row["MatchCount"] = item.matchCount ?? (object)DBNull.Value;
						row["OutlierCardinality"] = item.outlierCardinality ?? (object)DBNull.Value;

						row["DataSignature"] = item.dataSignature ?? (object)DBNull.Value;
						row["StructureSignature"] = item.structureSignature ?? (object)DBNull.Value;
						row["Cardinality"] = item.cardinality ?? (object)DBNull.Value;
						row["ShapeCardinality"] = item.shapesCardinality ?? (object)DBNull.Value;

						row["TotalCount"] = item.TotalCount ?? (object)DBNull.Value;
						row["OutlierCount"] = item.OutlierCount ?? (object)DBNull.Value;
						row["KeyConfidence"] = item.KeyConfidence ?? (object)DBNull.Value;
						row["DetectionLocale"] = item.DetectionLocale ?? (object)DBNull.Value;
						row["FtaVersion"] = item.FtaVersion ?? (object)DBNull.Value;
						row["DecimalSeparator"] = item.DecimalSeparator ?? (object)DBNull.Value;

						row["PopularityCount"] = item.PopularityCount ?? (object)DBNull.Value;
						row["IsAuthorizedForPopularity"] = item.IsAuthorizedForPopularity ?? (object)DBNull.Value;
						row["SourceLastModified"] = item.SourceLastModified ?? (object)DBNull.Value;
						row["FilterCount"] = item.FilterCount ?? (object)DBNull.Value;


						DataProfileTable.Rows.Add(row);
						if (item.outlierDetail != null)
						{
							foreach (DataProfileSampleDetail outlier in item.outlierDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								else
								{
									row["ExecutionItemUid"] = DBNull.Value;
								}
								sampleRow["SampleType"] = "outlierDetail";
								sampleRow["Key"] = outlier.key ?? (object)DBNull.Value;
								sampleRow["Value"] = outlier.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.shapesDetail != null)
						{
							foreach (DataProfileSampleDetail shape in item?.shapesDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "shapesDetail";
								sampleRow["Key"] = shape.key;
								sampleRow["Value"] = shape.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.cardinalityDetail != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.cardinalityDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "cardinalityDetail";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterCasingStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterCasingStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterCasingStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterDataTypeStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterDataTypeStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterDataTypeStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterSpacingStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterSpacingStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterSpacingStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.scriptDistributionStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.scriptDistributionStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "scriptDistributionStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.specialCharacterStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.specialCharacterStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "specialCharacterStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.percentileStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.percentileStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "percentileStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.textPatternDetails != null)
						{
							foreach (DataProfileTextPatternDetail stat in item?.textPatternDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "textPatternDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.semanticAnalysisDetails != null)
						{
							foreach (DataProfileSemanticAnalysisDetail stat in item?.semanticAnalysisDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "semanticAnalysisDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.confidenceAnalysisDetails != null)
						{
							foreach (DataProfileConfidenceAnalysisDetails stat in item?.confidenceAnalysisDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "confidenceAnalysisDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.tableStructureInfo != null)
						{
							DataRow jsonRow = DataProfileSampleTable.NewRow();
							jsonRow["ExecutionID"] = execution.ExecutionID;
							jsonRow["ItemNumber"] = itemNumber;
							if (item.ExecutionItemUid.HasValue)
							{
								jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
							}
							jsonRow["SampleType"] = "tableStructureInfo";
							jsonRow["JsonValue"] = JsonConvert.SerializeObject(item.tableStructureInfo);
							DataProfileSampleTable.Rows.Add(jsonRow);
						}

						if (item.topK != null)
						{
							foreach (string topK in item?.topK)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "topK";
								sampleRow["Key"] = DBNull.Value;
								sampleRow["Value"] = topK;
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.bottomK != null)
						{
							foreach (string bottomK in item?.bottomK)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "bottomK";
								sampleRow["Key"] = DBNull.Value;
								sampleRow["Value"] = bottomK;
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						itemNumber++;
					}

					#endregion

					#region Bulk Copy

					await Connection.OpenIfClosed();

					using (SqlTransaction transaction = Connection.BeginTransaction())
					{
						try
						{
							#region Bulk Copy Data Profile

							using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
							{
								BatchSize = DataProfileTable.Rows.Count,
								DestinationTableName = "[api].[ExecutionAssetDataProfile]",
								BulkCopyTimeout = SqlBulkBatchTimeout
							})
							{
								bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
								bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
								bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
								bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
								bulkCopy.ColumnMappings.Add("ProfileSetDate", "ProfileSetDate");
								bulkCopy.ColumnMappings.Add("ProfileIdentifier", "ProfileIdentifier");
								bulkCopy.ColumnMappings.Add("UniqueCount", "UniqueCount");
								bulkCopy.ColumnMappings.Add("SampleCount", "SampleCount");
								bulkCopy.ColumnMappings.Add("NullCount", "NullCount");
								bulkCopy.ColumnMappings.Add("BlankCount", "BlankCount");
								bulkCopy.ColumnMappings.Add("MeanValue", "MeanValue");

								bulkCopy.ColumnMappings.Add("MinimumValue", "MinimumValue");
								bulkCopy.ColumnMappings.Add("MaximumValue", "MaximumValue");
								bulkCopy.ColumnMappings.Add("MinimumLength", "MinimumLength");
								bulkCopy.ColumnMappings.Add("MaximumLength", "MaximumLength");
								bulkCopy.ColumnMappings.Add("StandardDeviation", "StandardDeviation");

								bulkCopy.ColumnMappings.Add("Type", "Type");
								bulkCopy.ColumnMappings.Add("Multiline", "Multiline");
								bulkCopy.ColumnMappings.Add("RegExp", "RegExp");
								bulkCopy.ColumnMappings.Add("Confidence", "Confidence");
								bulkCopy.ColumnMappings.Add("TypeQualifier", "TypeQualifier");

								bulkCopy.ColumnMappings.Add("LogicalType", "LogicalType");
								bulkCopy.ColumnMappings.Add("LeadingWhiteSpace", "LeadingWhiteSpace");
								bulkCopy.ColumnMappings.Add("LeadingZeroCount", "LeadingZeroCount");

								bulkCopy.ColumnMappings.Add("TrailingWhiteSpace", "TrailingWhiteSpace");
								bulkCopy.ColumnMappings.Add("MatchCount", "MatchCount");
								bulkCopy.ColumnMappings.Add("OutlierCardinality", "OutlierCardinality");

								bulkCopy.ColumnMappings.Add("DataSignature", "DataSignature");
								bulkCopy.ColumnMappings.Add("StructureSignature", "StructureSignature");
								bulkCopy.ColumnMappings.Add("Cardinality", "Cardinality");
								bulkCopy.ColumnMappings.Add("ShapeCardinality", "ShapeCardinality");

								bulkCopy.ColumnMappings.Add("TotalCount", "TotalCount");
								bulkCopy.ColumnMappings.Add("OutlierCount", "OutlierCount");
								bulkCopy.ColumnMappings.Add("KeyConfidence", "KeyConfidence");
								bulkCopy.ColumnMappings.Add("DetectionLocale", "DetectionLocale");
								bulkCopy.ColumnMappings.Add("FtaVersion", "FtaVersion");
								bulkCopy.ColumnMappings.Add("DecimalSeparator", "DecimalSeparator");

								bulkCopy.ColumnMappings.Add("PopularityCount", "PopularityCount");
								bulkCopy.ColumnMappings.Add("IsAuthorizedForPopularity", "IsAuthorizedForPopularity");
								bulkCopy.ColumnMappings.Add("SourceLastModified", "SourceLastModified");
								bulkCopy.ColumnMappings.Add("FilterCount", "FilterCount");

								bulkCopy.WriteToServer(DataProfileTable);
							}

							#endregion

							#region Bulk Copy Data Profile Sample

							using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
							{
								BatchSize = DataProfileSampleTable.Rows.Count,
								DestinationTableName = "[api].[ExecutionAssetDataProfileSample]",
								BulkCopyTimeout = SqlBulkBatchTimeout
							})
							{
								bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
								bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
								bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
								bulkCopy.ColumnMappings.Add("SampleType", "SampleType");
								bulkCopy.ColumnMappings.Add("Key", "Key");
								bulkCopy.ColumnMappings.Add("Value", "Value");
								bulkCopy.ColumnMappings.Add("JsonValue", "JsonValue");

								bulkCopy.WriteToServer(DataProfileSampleTable);
							}

							#endregion

							transaction.Commit();

							addMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
						}
						catch (Exception)
						{
							if (transaction != null)
							{
								transaction.Rollback();
							}
							throw;
						}
					}

					#endregion

					Connection.Execute($@"
						update	api.ExecutionAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
						where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	api.ExecutionAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a ProfileSetDate.'
						where	ExecutionID = @ExecutionID and [ProfileSetDate] is null;

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
						from
							api.ExecutionAssetDataProfile EDP
							left Join
							Asset A on EDP.AssetUid = A.Uid
						where	ExecutionID = @ExecutionID and A.Uid is null;

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Profiling data can only be associated with Business or Technical Asset types'
						from
							api.ExecutionAssetDataProfile EDP
							inner Join
							Asset A on EDP.AssetUid = A.Uid
							inner join 
							AssetType AST on A.AssetTypeId = AST.ID
						where	ExecutionID = @ExecutionID and AST.Class not in (1, 8);

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Record does not exist with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 120)
						from
							api.ExecutionAssetDataProfile EDP
							inner join 
							Asset A on EDP.AssetUid = A.Uid
							left join 
							AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
						where	ExecutionID = @ExecutionID and ADP.AssetId is null and @isInsert = 0;
						
						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Record already exists with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 20)
						from
							api.ExecutionAssetDataProfile EDP
							inner join 
							Asset A on EDP.AssetUid = A.Uid
							inner join 
							AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
						where	ExecutionID = @ExecutionID and @isInsert = 1;						

						Update EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Elements in '+ EDPS.SampleType +' cannot be Empty strings'
						from  
							api.ExecutionAssetDataProfile EDP 
							inner join 
							(
								select 
									distinct ExecutionID, itemnumber, SampleType 
								from 
									api.ExecutionAssetDataProfileSample 
								where ExecutionID = @ExecutionID and LEN(TRIM(value))=0 and LOWER(SampleType) in ('topk', 'bottomk') 
							) EDPS on EDP.ExecutionID=EDPS.ExecutionID and EDP.ItemNumber=EDPS.ItemNumber 
						where 
							EDP.ExecutionID = @ExecutionID                             
						
						declare @IsAdministrator bit = 0						
						select	@IsAdministrator = IsAdministrator
						from	reporting.Global_Resource
						where	ResourceID = @ResourceID
						IF(@IsAdministrator = 0)
						BEGIN
							update	EDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + '{CompanyContextApiError.DataProfilingNoPermission}'
							from	
									api.ExecutionAssetDataProfile EDP
							where 
									EDP.ExecutionID = @ExecutionID and not exists (
												select 1
												from	Asset A
														outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
														where	
														A.Uid = EDP.AssetUid 
														and
														(															
															(
																P.AssetID = A.ID
																or 
																P.AssetTypeID is null
															)
															OR
															(																	
																P.AssetID=0 
																and 
																P.AssetTypeID=A.AssetTypeID
															)
														)
														and 
														P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
												);
						END
",
									new { execution.ExecutionID, isInsert, execution.ResourceID, p = Permission.EditAsset }, commandTimeout: timeout);

					addMeasurement(metrics, "LogAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
					sw.Restart();

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = request.Count();
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					string querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";
					string insertSQL = $@"
										DROP TABLE IF EXISTS #mergeResultTable
										CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

										MERGE INTO AssetDataProfile ADP
										USING (
												SELECT
													A.ID as AssetId, E.*
												FROM  
													api.ExecutionAssetDataProfile E
												INNER JOIN
													Asset A ON A.Uid = E.AssetUid
												WHERE {querySuffix}
												) EDP
										ON 1 = 0                                       
										WHEN NOT MATCHED THEN
										INSERT ([AssetID]
													,[ProfileSetDate]
													,[ProfileIdentifier]
                                                    ,[UniqueCount]
													,[SampleCount]
													,[NullCount]
													,[BlankCount]
													,[MeanValue]
													,[MinimumValue]
													,[MaximumValue]
													,[MinimumLength]
													,[MaximumLength]
													,[StandardDeviation]
													,[Type]
													,[Multiline]
													,[RegExp]
													,[Confidence]
													,[TypeQualifier]
													,[LogicalType]
													,[LeadingWhiteSpace]
													,[LeadingZeroCount]
													,[TrailingWhiteSpace]
													,[MatchCount]
													,[OutlierCardinality]
													,[DataSignature]
													,[StructureSignature]
													,[Cardinality]
													,[ShapeCardinality]
													,[TotalCount]
													,[OutlierCount]
													,[KeyConfidence]
													,[DetectionLocale]
													,[FtaVersion]
													,[DecimalSeparator]
													,[PopularityCount]
													,[IsAuthorizedForPopularity]
													,[SourceLastModified]
													,[FilterCount]
													,[CreatedBy]
													,[CreatedOn]
													,[UpdatedBy]
													,[UpdatedOn])
												VALUES
													(EDP.AssetID
													,EDP.ProfileSetDate
													,EDP.ProfileIdentifier
                                                    ,EDP.UniqueCount
													,EDP.SampleCount
													,EDP.NullCount
													,EDP.BlankCount
													,EDP.MeanValue
													,EDP.MinimumValue
													,EDP.MaximumValue
													,EDP.MinimumLength
													,EDP.MaximumLength
													,EDP.StandardDeviation
													,EDP.Type
													,EDP.Multiline
													,EDP.RegExp
													,EDP.Confidence
													,EDP.TypeQualifier
													,EDP.LogicalType
													,EDP.LeadingWhiteSpace
													,EDP.LeadingZeroCount
													,EDP.TrailingWhiteSpace
													,EDP.MatchCount
													,EDP.OutlierCardinality
													,EDP.DataSignature
													,EDP.StructureSignature
													,EDP.Cardinality
													,EDP.ShapeCardinality
													,EDP.TotalCount
													,EDP.OutlierCount
													,EDP.KeyConfidence
													,EDP.DetectionLocale
													,EDP.FtaVersion
													,EDP.DecimalSeparator
													,EDP.PopularityCount
													,EDP.IsAuthorizedForPopularity
													,EDP.SourceLastModified
													,EDP.FilterCount
													,@CurrentResourceID
													,getutcdate()
													,@CurrentResourceID
													,getutcdate())
											OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;";
					string updateSQL = $@"
										DROP TABLE IF EXISTS #mergeResultTable
										CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

										MERGE INTO AssetDataProfile ADP
										USING (
												SELECT
													A.ID as AssetId, E.*
												FROM  
													api.ExecutionAssetDataProfile E
												INNER JOIN
													Asset A ON A.Uid = E.AssetUid
												WHERE {querySuffix}
												) EDP
										ON (EDP.AssetId = ADP.AssetID AND EDP.profileSetDate = ADP.profileSetDate)
										WHEN MATCHED THEN
										UPDATE SET
                                            ADP.[ProfileIdentifier] = EDP.[ProfileIdentifier]
                                            ,ADP.[UniqueCount] = EDP.[UniqueCount]
											,ADP.[SampleCount] = EDP.[SampleCount]
											,ADP.[NullCount] = EDP.[NullCount]
											,ADP.[BlankCount] = EDP.[BlankCount]
											,ADP.[MeanValue] = EDP.[MeanValue]
											,ADP.[MinimumValue] = EDP.[MinimumValue]
											,ADP.[MaximumValue] = EDP.[MaximumValue]
											,ADP.[MinimumLength] = EDP.[MinimumLength]
											,ADP.[MaximumLength] = EDP.[MaximumLength]
											,ADP.[StandardDeviation] = EDP.[StandardDeviation]
											,ADP.[Type] = EDP.[Type]
											,ADP.[Multiline] = EDP.[Multiline]
											,ADP.[RegExp] = EDP.[RegExp]
											,ADP.[Confidence] = EDP.[Confidence]
											,ADP.[TypeQualifier] = EDP.[TypeQualifier]
											,ADP.[LogicalType] = EDP.[LogicalType]
											,ADP.[LeadingWhiteSpace] = EDP.[LeadingWhiteSpace]
											,ADP.[LeadingZeroCount] = EDP.[LeadingZeroCount]
											,ADP.[TrailingWhiteSpace] = EDP.[TrailingWhiteSpace]
											,ADP.[MatchCount] = EDP.[MatchCount]
											,ADP.[OutlierCardinality] = EDP.[OutlierCardinality]
											,ADP.[DataSignature] = EDP.[DataSignature]
											,ADP.[StructureSignature] = EDP.[StructureSignature]
											,ADP.[Cardinality] = EDP.[Cardinality]
											,ADP.[ShapeCardinality] = EDP.[ShapeCardinality]
											,ADP.[TotalCount] = EDP.[TotalCount]
											,ADP.[OutlierCount] = EDP.[OutlierCount]
											,ADP.[KeyConfidence] = EDP.[KeyConfidence]
											,ADP.[DetectionLocale] = EDP.[DetectionLocale]
											,ADP.[FtaVersion] = EDP.[FtaVersion]
											,ADP.[DecimalSeparator] = EDP.[DecimalSeparator]
											,ADP.[PopularityCount] = EDP.[PopularityCount]
											,ADP.[IsAuthorizedForPopularity] = EDP.[IsAuthorizedForPopularity]
											,ADP.[SourceLastModified] = EDP.[SourceLastModified]
											,ADP.[FilterCount] = EDP.[FilterCount]
											,ADP.[UpdatedBy] = @CurrentResourceID
											,ADP.[UpdatedOn] = getutcdate()                                       
										OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;

											Delete ADPS from AssetDataProfileSample ADPS inner join #mergeResultTable rt on ADPS.AssetDataProfileID = rt.DataProfileID
											Delete ADPSJ from AssetDataProfileSampleJson ADPSJ inner join #mergeResultTable rt on ADPSJ.AssetDataProfileID = rt.DataProfileID";


					string insertSampleSQL = $@"
										insert into AssetDataProfileSample 
													([AssetDataProfileID]
													,[SampleType]
													,[Key]
													,[Value])                                            
										SELECT  
											rt.DataProfileID
											,EDPS.SampleType
											,EDPS.[Key]
											,EDPS.Value
										FROM  
											api.ExecutionAssetDataProfileSample EDPS
										INNER JOIN
											api.ExecutionAssetDataProfile E ON EDPS.ExecutionID=E.ExecutionID AND EDPS.itemnumber = E.itemnumber
										INNER JOIN 
											#mergeResultTable rt ON rt.itemNumber = EDPS.itemNumber
										WHERE 
											EDPS.JsonValue is null AND
											{querySuffix}
											";

					string insertSampleJsonSQL = $@"
										insert into AssetDataProfileSampleJson 
													([AssetDataProfileID]
													,[SampleType]
													,[Value])                                            
										SELECT  
											rt.DataProfileID
											,EDPS.SampleType
											,EDPS.JsonValue
										FROM  
											api.ExecutionAssetDataProfileSample EDPS
										INNER JOIN
											api.ExecutionAssetDataProfile E ON EDPS.ExecutionID=E.ExecutionID AND EDPS.itemnumber = E.itemnumber
										INNER JOIN 
											#mergeResultTable rt ON rt.itemNumber = EDPS.itemNumber
										WHERE 
											EDPS.Value is null AND EDPS.JsonValue is not null AND
											{querySuffix}
											";

					string sql = $@"{insertSQL}
								{insertSampleSQL}
								{insertSampleJsonSQL}";

					if (!isInsert)
					{
						sql = $@"{updateSQL}
								{insertSampleSQL}
								{insertSampleJsonSQL}";
					}

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{

							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								#region Load valid items into table
								try
								{
									Connection.Execute(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

									#endregion

									// Update success flag.
									Connection.Execute(
										$@"update E 
											set Success = 1 
									   From api.ExecutionAssetDataProfile E
									   where {querySuffix};",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;
								}
								catch (Exception ex)
								{
									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
										// Do not interrupt loop if only this instance fails.
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										sw.Restart();
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAssetDataProfile", ex.GetFullExceptionData(false), timeout);
										addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
										sw.Restart();
									}
								}
							}
						}

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
						sw.Restart();
					}
				}
			}

			CompleteApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionAssetDataProfile");

			var profileUidsQuery = await Connection.QueryAsync<Guid>("select AssetUid from api.ExecutionAssetDataProfile where ExecutionID = @ExecutionID and Success = 1", new { execution.ExecutionID });
			var profileUids = profileUidsQuery.ToList(); 
			
			QueueSource.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
			{
				CompanyID = CurrentCompanyID,
				BatchUids = profileUids,
				BatchOperation = ReindexBatchOperation.Update
			});

			addMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			Connection.CloseIfOpened();
		}

		#endregion
	}
}
