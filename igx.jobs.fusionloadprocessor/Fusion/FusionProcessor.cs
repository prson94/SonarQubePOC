using d360.core.entities;
using d360.utils.company;
using d360.core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using d360.extensions;
using Newtonsoft.Json;
using System.Data;
using d360.extensions.queue;
using d360.extensions.storage;
using Microsoft.Azure.WebJobs.Host;
using System.IO;

namespace igx.jobs.fusionloadprocessor
{
    public class FusionProcessor
    {
        #region Properties

        public int CompanyID { get; private set; }
        public int FusionID { get; private set; }
        public int ExecutionID { get; private set; }
        public string LogFileName { get; private set; }
        public bool IsFirstRun { get; private set; }

        /// <summary>
        /// Timeout in seconds to read data from database
        /// </summary>
        public int ReadQueryTimeout { get; set; }
        /// <summary>
        /// Timeout in seconds to execute queries against the database update/insert/create
        /// </summary>
        public int ExecuteQueryTimeout { get; set; }

        /// <summary>
        /// Number of seconds before bulk copy operations timeout
        /// </summary>
        public int BulkCopyTimeout { get; set; }

        public int MergeChunkSize { get; set; }

        public TextWriter Log { get; set; }

        #endregion

        #region Variables/Constants

        private FusionWorkArea _workArea = new FusionWorkArea();

        private static string SourceIDAttribute = "SourceID";
        private static string ParentSourceIDAttribute = "ParentSourceID";
        private static string NameAttribute = "Name";
        private static string ActionAttribute = "Action";
        private static string FusionAttributeTypeIDAttribute = "FusionAttributeTypeID";

        //private static int MAX_FIELD_VALUE_LENGTH = 4000;
        private static int MAX_SOURCEID_LENGTH = 250;
        private static int MAX_FIELD_NAME_LENGTH = 250;
        private static string FUSION_ATTRIBUTE_MISSING_NAME_NAME = "Name not resolved";
        private static string FUSION_PROCESSOR_AI_NAME_TOTAL = "FusionProcessor - Total";
        private static string FUSION_PROCESSOR_AI_NAME_DOWNLOAD = "FusionProcessor - Download JSON";
        private static string FUSION_PROCESSOR_AI_NAME_LOG_EXECUTION = "FusionProcessor - Log Execution";
        private static string FUSION_PROCESSOR_AI_NAME_SAVE_CHANGED_VALUES = "FusionProcessor - Save Changed Values";
        private static string FUSION_PROCESSOR_AI_NAME_LOADCURRENTFUSIONFIELD = "FusionProcessor - Load Current Fusion Field Info";
        private static string FUSION_PROCESSOR_AI_NAME_GENERATE_FUSION_ATTR_VALUES = "FusionProcessor - Generate Fusion Attr Values";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_MERGE = "FusionProcessor - Fusion Attribute Merge";
        //private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_CACHE = "FusionProcessor - Fusion Attribute Cache";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_RELOAD = "FusionProcessor - Fusion Attribute Reload";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_FIELDTYPE_MAP = "FusionProcessor - Fusion Attribute Field Map";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_VALS = "FusionProcessor - Fusion Field Vals";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_MERGE = "FusionProcessor - Fusion Field Merge";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_PARENT_UPDATE = "FusionProcessor - Fusion Attr Parent ID Map";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_PARENT_UPDATE_MERGE = "FusionProcessor - Fusion Attr Parent ID Merge";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_CHANGES = "FusionProcessor - Fusion Determine Field Changes";
        private static string FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_ATTR_CHANGES = "FusionProcessor - Fusion Determine Attr Changes";
        //private static string FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_PATHS_UPD = "FusionProcessor - Update Paths / TextPath";
        private static string FUSION_PROCESSOR_AI_NAME_PROCESS_RELATIONSHIPS = "FusionProcessor - Process Relationships";
        private static string FUSION_PROCESSOR_AI_NAME_PROCESS_QUERY_ITEMS = "FusionProcessor - Process Query Items";

        #endregion

        public async Task Process(string functionName, FusionProcessingData fusionData, int bulkTimeout, int readTimeout, int executeTimeout, int chunkSize, TextWriter log)
        {
            BulkFusionImport data = null;

            var jobDuration = Stopwatch.StartNew();
            var metrics = new Dictionary<string, double>();

            BulkCopyTimeout = bulkTimeout;
            ReadQueryTimeout = readTimeout;
            ExecuteQueryTimeout = executeTimeout;
            CompanyID = fusionData.CompanyID;
            FusionID = fusionData.FusionID;
            LogFileName = fusionData.LogFileName;
            MergeChunkSize = chunkSize;
            Log = log;

            if (CompanyID <= 0) throw new Exception("Invalid company id specified.");

            if (string.IsNullOrEmpty(LogFileName)) throw new Exception("Error invalid or no file specified to process fusion data from");

            if (FusionID < 0) throw new Exception("Invalid fusion id specified.");

            var baseEventProperties = new Dictionary<string, string>()
            {
                { "FusionID", FusionID.ToString() },
                { "FileName", LogFileName },
                { "SQLBulkCopyTimeout", BulkCopyTimeout.ToString() },
                { "SQLReadQueryTimeout", ReadQueryTimeout.ToString() },
                { "SQLExecutionTimeout", ExecuteQueryTimeout.ToString() }
            };

            Log.WriteLine("====================================================================================================");
            Log.WriteLine($"STARTING FUSION JOB FOR FUSION ID: {FusionID} COMPANY ID: {CompanyID} FILE: {LogFileName}");

            var storageProvider = new AzureStorageProvider();

            var folderName = string.Format("bulk-fusion-{0}", fusionData.CompanyID);
            //load json from azure

            var sw = Stopwatch.StartNew();
            Log.WriteLine("STARTING JSON DATA READ");

            string json = storageProvider.GetFileContentsAsString(folderName, fusionData.LogFileName, Encoding.UTF8);
            data = JsonConvert.DeserializeObject<BulkFusionImport>(json);

            if (data == null) throw new Exception("UNABLE TO LOAD FUSION DATA FROM AZURE STORAGE / NULL FUSION DATA OBJECT.");

            Log.WriteLine(string.Format("COMPLETED JSON DATA READ\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_DOWNLOAD, sw.Elapsed);


            if (data.Models == null)
                data.Models = new List<Dictionary<string, string>>();
            if (data.QueryItems == null)
                data.QueryItems = new List<IDictionary<string, string>>();
            if (data.Relationships == null)
                data.Relationships = new FusionRelationshipModels();

            Log.WriteLine($"FUSION JOB HAS {data.Models.Count} MODELS, {data.QueryItems.Count} QUERY ITEMS, {data.Relationships.Count} RELATIONS");

            metrics["MODELS"] = data.Models.Count;
            metrics["QUERYITEMS"] = data.QueryItems.Count;
            metrics["RELATIONS"] = data.Relationships.Count;
            metrics["JSON DATA SIZE"] = json.Length;

            baseEventProperties["REFRESH"] = data.ForceRefresh ? "Yes" : "No";

            CoreFunction.AITrackEvent(functionName, "Fusion Job Starting", baseEventProperties, CompanyID, metrics);

            // Sanitize data.
            // Remove spaces from values in models.
            // This can be done in parallel.
            var cTask = Task.Run(() => CleanModelData(data.Models));
            var fTask = Task.Run(() => RemoveRelationSpaces(data.Relationships));

            // Wait for this to finish.
            await cTask;
            await fTask;

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(CompanyID))
            {
                try
                {
                    companyConnection.Open();
                    //Generate an execution id                                        

                    sw.Restart();
                    ExecutionID = await LogExecution(companyConnection, data.Version);
                    Log.WriteLine(string.Format("LogExecution\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
                    CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_LOG_EXECUTION, sw.Elapsed);

                    Log.WriteLine($"Processing fusion execution ID: [{ExecutionID}]");

                    //Process Models                
                    await ProcessModels(companyConnection, data.Models, functionName, baseEventProperties, CompanyID);

                    //Process Dynamically defined query results.
                    if (data.QueryItems != null)
                    {
                        sw.Restart();
                        await ProcessQueryItems(companyConnection, data.QueryItems);
                        CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_PROCESS_QUERY_ITEMS, sw.Elapsed);
                    }

                    //Process Relationships
                    sw.Restart();

                    await ProcessRelationships(companyConnection, data.Relationships);
                    CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_PROCESS_RELATIONSHIPS, sw.Elapsed);

                    sw.Restart();
                    await SaveChangedValuesLog(companyConnection);
                    Log.WriteLine(string.Format("SaveChangedValuesLog\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
                    CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_SAVE_CHANGED_VALUES, sw.Elapsed);

                    //Update the executionID to say this is done
                    await UpdateExecutionWithStats(companyConnection);

                    //If any changes were made add record to queue.task
                    await UpdateQueue(companyConnection);

                    //if any changes occured fire off message to say 
                    MarkFusionJobAsHavingLoaded();
                }
                catch (AggregateException exception)
                {
                    CoreFunction.AITrackException(functionName, exception, CompanyID);
                    Log.WriteLine("FusionProcessor::Process encountered and error while running fusion job.");

                    foreach (Exception ex in exception.InnerExceptions)
                    {
                        Log.WriteLine($"Exception details [{ex.Message}]");
                        LogFusionError(companyConnection, ex);
                    }

                    throw exception;
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, CompanyID);

                    Log.WriteLine($"FusionProcessor::Process encountered and error while running fusion job.  Exception details [{ex.Message}]" );

                    LogFusionError(companyConnection, ex);

                    throw ex;
                }
            }
            jobDuration.Stop();

            metrics["Duration(s)"] = jobDuration.ElapsedMilliseconds / 1000;
            CoreFunction.AITrackEvent(functionName, "Fusion Job Complete", baseEventProperties, CompanyID, metrics);
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_TOTAL, jobDuration.Elapsed);
        }

        private void MarkFusionJobAsHavingLoaded()
        {
            if (_workArea.Changes.AddCount <= 0 && _workArea.Changes.UpdateCount <= 0 && _workArea.Changes.DeleteCount <= 0) return;

            var topicName = CompanyConnectionUtils.GetEventTopicName(CompanyID);

            if (string.IsNullOrEmpty(topicName)) return;

            var eventBus = new AzureQueueSource();

            eventBus.CreateTopicMessage(topicName, new d360.core.queue.EventInfo
            {
                CompanyID = CompanyID,
                Action = d360.core.enums.Workflow.ChangeType.Loaded,
                ResourceID = 0,
                Object = new d360.core.queue.EventObjectInfo
                {
                    Object = SystemObjects.Fusion,
                    ObjectID = FusionID,
                    ObjectType = SystemObjects.FusionType,
                    ObjectTypeID = -1
                },
                DomainPrefix = "demo.dev"
            });
        }

        private async Task UpdateQueue(SqlConnection companyConnection)
        {
            if (_workArea.Changes.AddCount > 0 || _workArea.Changes.UpdateCount > 0 || _workArea.Changes.DeleteCount > 0 || IsFirstRun)
            {
                await companyConnection.ExecuteAsync(@"
                    insert into [queue].[task] ([Action], [Object], [ObjectID]) values ('Notify','FusionExecution',@id)                    
                ", new { id = ExecutionID }, commandTimeout: ExecuteQueryTimeout);

                await companyConnection.ExecuteAsync(@"
                    insert into [queue].[task] ([Action], [Object], [ObjectID]) values ('FusionCache','Fusion',@id)                    
                ", new { id = FusionID }, commandTimeout: ExecuteQueryTimeout);
            }
        }

        private void LogFusionError(SqlConnection companyConnection, Exception ex)
        {
            if (ex == null || ExecutionID <= 0)
            {
                Log.WriteLine("UNABLE TO LOG ERROR TO [FUSION].[ERROR] TABLE EXECUTION ID IS NULL OR EXCEPTION OBJECT IS NULL");

                return;
            }
            companyConnection.Execute(
                @"insert into [fusion].[error] ([ExecutionID],[Date],[Error]) values(@ID,CURRENT_TIMESTAMP,@message);", 
                new { message = ex.ToString(), ID = ExecutionID }
            );
        }

        private async Task SaveChangedValuesLog(SqlConnection companyConnection)
        {
            if (_workArea.Changes.ChangedValues.Count <= 0) return;

            //bulk sql insert to the resultex table
            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
            {

                bulkCopy.BatchSize = _workArea.Changes.ChangedValues.Count();
                bulkCopy.DestinationTableName = "[fusion].[result]";
                bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                var table = new DataTable();
                var columnName = "ExecutionID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FusionAttributeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "Body";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FieldTypeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FieldName";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "Action";
                table.Columns.Add(columnName, typeof(char));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "OldValue";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "NewValue";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in _workArea.Changes.ChangedValues)
                {
                    var row = table.NewRow();

                    row["ExecutionID"] = ExecutionID;
                    row["FusionAttributeID"] = item.FusionAttributeID;

                    if (item.Action == "D") row["Body"] = "Item removed from source.";

                    row["FieldTypeID"] = item.FieldTypeID;
                    var fieldInfo = _workArea.FieldToAttributeMapping.FirstOrDefault(x => x.FieldTypeID == item.FieldTypeID);
                    if (fieldInfo != null)
                        row["FieldName"] = fieldInfo.FieldTypeName;
                    else
                        row["FieldName"] = "Name";

                    row["Action"] = item.Action;
                    if (!string.IsNullOrEmpty(item.OldValue) && item.OldValue.Length > 250)
                        row["OldValue"] = item.OldValue.Substring(0, 250);
                    else
                        row["OldValue"] = item.OldValue;

                    if (!string.IsNullOrEmpty(item.Value) && item.Value.Length > 250)
                        row["NewValue"] = item.Value.Substring(0, 250);
                    else
                        row["NewValue"] = item.Value;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }
        }

        private async Task UpdateExecutionWithStats(SqlConnection companyConnection)
        {
            var res = await companyConnection.ExecuteAsync(@"
                    update [fusion].[execution]
                        set [DateCompleted] = @date,
                            [Adds] = @a,
                            [Updates] = @u,
                            [Deletes] = @d
                    where [id] = @id;
                ", new { date = DateTime.UtcNow, id = ExecutionID, a = _workArea.Changes.AddCount, u = _workArea.Changes.UpdateCount, d = _workArea.Changes.DeleteCount }, commandTimeout: ExecuteQueryTimeout);
        }

        private async Task<int> LogExecution(SqlConnection companyConnection, string version)
        {
            if (string.IsNullOrEmpty(version)) version = "unknown";
            //insert a record into the fusion execution table that logs the start of this execution            
            var result = await companyConnection.QueryAsync<int>(@"
                    insert 
                        into [fusion].[execution] ([fusionID],[RawLogFileName],[DateStarted],[Version])
                        values(@inFusionID,@log,@started,@ver);
                        SELECT CAST(SCOPE_IDENTITY() as int)
            ", new { inFusionID = FusionID, log = LogFileName, started = DateTime.UtcNow, ver = version }, commandTimeout: ReadQueryTimeout);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Handles the relationships for the fusion data
        /// </summary>
        /// <param name="relationships"></param>
        /// <returns></returns>
        private async Task ProcessRelationships(SqlConnection companyConnection, FusionRelationshipModels relationships)
        {
            if (relationships.Count == 0)
            {
                Log.WriteLine("NO RELATIONS SPECIFIED AS PART OF FUSION JOB SKIPPING PROCESSRELATIONSHIPS.");

                return;
            }

            //Load the intersect types
            await LoadFusionIntersectTypes(companyConnection);

            //build mapping of fusion attributes ids to intersect types
            GenerateRelationshipInsertData(relationships);

            // determine which relations already exist and remove them
            await DoResolvedRelationsInsert(companyConnection);
        }

        private async Task DoResolvedRelationsInsert(SqlConnection companyConnection)
        {
            // insert all the resolved relation into into a temp table
            await companyConnection.ExecuteAsync(@"create table #tempResolvedRel([ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, IntersectTypeID int, SourceIntersectTypeID int, TargetIntersectTypeID int,  StartFusionAttributeID int, EndFusionAttributeID int)", commandTimeout: ExecuteQueryTimeout);

            Log.WriteLine($"WRITING {_workArea.Relationships.ResolvedRelationshipData.Count} RESOLVED RELATIONSHIPS TO #TEMPRESOLVEDREL TEMP TABLE.");

            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
            {
                bulkCopy.BatchSize = _workArea.Relationships.ResolvedRelationshipData.Count;
                bulkCopy.DestinationTableName = "#tempResolvedRel";
                bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                var table = new DataTable();
                var columnName = "IntersectTypeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "SourceIntersectTypeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "TargetIntersectTypeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "StartFusionAttributeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "EndFusionAttributeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in _workArea.Relationships.ResolvedRelationshipData)
                {
                    var row = table.NewRow();

                    row["IntersectTypeID"] = item.IntersectTypeID;
                    row["SourceIntersectTypeID"] = item.StartIntersectTypeID;
                    row["TargetIntersectTypeID"] = item.EndIntersectTypeID;
                    row["StartFusionAttributeID"] = item.StartFusionAttributeID;
                    row["EndFusionAttributeID"] = item.EndFusionAttributeID;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            // insert relations
            await companyConnection.ExecuteAsync(@"
declare @objectType varchar(50) = 'FusionAttribute';			
			
MERGE
	INTO    [Intersect] d
	USING   (
			SELECT	IntersectTypeID, 
					ID,
					StartFusionAttributeID,
					EndFusionAttributeID
			FROM	#tempResolvedRel
			) S
	ON      (d.Subject = 'FusionAttribute' and 
                                                    d.Object = 'FusionAttribute' and 
                                                    (
                                                        ( d.SubjectID = S.startfusionattributeid and d.ObjectID = S.endfusionattributeid ) OR
                                                        ( d.SubjectID = S.endfusionattributeid and d.ObjectID = S.startfusionattributeid  )
                                                    ))
	WHEN NOT MATCHED THEN
	INSERT  (IntersectTypeID, Subject, SubjectID, Object, ObjectID)
	VALUES  (S.IntersectTypeID, @objectType, S.StartFusionAttributeID, @objectType, S.EndFusionAttributeID);
	
", new { executionID = ExecutionID }, commandTimeout: ExecuteQueryTimeout);
        }

        /// <summary>
        /// Insert relationships between start /end id's we cant figure out 
        /// into the unresolved relations table
        /// </summary>
        /// <returns></returns>
        private async Task DoUnresolvedRelationsInsert(SqlConnection companyConnection)
        {
            if (_workArea.Relationships.UnresolvedRelationshipData.Count == 0)
            {
                Log.WriteLine("NO UNRESOLVED RELATIONS EXITING DoUnresolvedRelationsInsert.");

                return;
            }

            await companyConnection.ExecuteAsync(@"create table #tempUnresolvedRel(StartID varchar(250), EndID nvarchar(250))", commandTimeout: ExecuteQueryTimeout);

            Log.WriteLine($"INSERTING {_workArea.Relationships.UnresolvedRelationshipData.Count} UNRESOLVED RELATIONSHIPS INTO TEMPUNRESOLVEDREL TEMP TABLE.");

            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
            {
                bulkCopy.BatchSize = _workArea.Relationships.UnresolvedRelationshipData.Count;
                bulkCopy.DestinationTableName = "#tempUnresolvedRel";
                bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                var table = new DataTable();
                var columnName = "StartID";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "EndID";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in _workArea.Relationships.UnresolvedRelationshipData)
                {
                    var row = table.NewRow();

                    row["StartID"] = item.StartSourceID;
                    row["EndID"] = item.EndSourceID;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            //merge with fusion.stagingrelationunresolved
            await companyConnection.ExecuteAsync(@"
                merge [fusion].[stagingrelationunresolved] as T
                using (
                    select distinct StartID,
                        EndID
                    from #tempUnresolvedRel
                ) as S
                on T.StartID = S.StartID and T.EndID = S.EndID
                when matched then
                    update set T.CreatedOn = getdate()                                             
                when not matched then
                    insert (StartID, EndID, CreatedOn)
                    values (S.StartID, S.EndID, getdate());
            ", commandTimeout: ExecuteQueryTimeout);
        }

        private void GenerateRelationshipInsertData(FusionRelationshipModels relationships)
        {
            var sourceToIDMapping = new Dictionary<string, FusionAttributeTempTableValue>();

            // Existing fusion values, there may be some that we did not update.
            foreach (var item in _workArea.ExistingFusionAttributes)
            {
                sourceToIDMapping[item.Key] = item.Value;
            }

            // These are the IDs of the updated items / new items.
            foreach (var item in _workArea.FusionAttributeTempValues)
            {
                FusionAttributeTempTableValue temp;
                if (sourceToIDMapping.TryGetValue(item.SourceID, out temp))
                {
                    if (item.ID > 0)
                        temp.ID = item.ID;
                }
                else
                {
                    sourceToIDMapping[item.SourceID] = item;
                }
            }

            // Loop through relationships.  Look for the id of the fusion attribute that goes with the sourceid's
            // if you cant find the id that goes with the sourceid stick the relationship in the unresolvedrelations collection
            // if you mapp the relationship stick the relationship in the resolvedrelations collection
            foreach (var item in relationships)
            {
                if (string.IsNullOrEmpty(item.StartID) || string.IsNullOrEmpty(item.EndID))
                {
                    Log.WriteLine("FOUND INVALID RELATIONSHIP CONTAINING NULL START/END VALUE FOR STARTID [" + item.StartID + "] ENDID [" + item.EndID + "].  DISREGARDING AS INVALID");

                    continue;
                }

                if (item.StartID.Length > MAX_SOURCEID_LENGTH)
                {
                    Log.WriteLine("FOUND INVALID STARTID STARTID [" + item.StartID + "] IS GREATER THAN MAX SOURCEID LENGTH OF [" + MAX_SOURCEID_LENGTH + "].  DISREGARDING AS INVALID.");

                    continue;
                }

                if (item.EndID.Length > MAX_SOURCEID_LENGTH)
                {
                    Log.WriteLine("FOUND INVALID ENDID STARTID [" + item.EndID + "] IS GREATER THAN MAX SOURCEID LENGTH OF [" + MAX_SOURCEID_LENGTH + "].  DISREGARDING AS INVALID.");

                    continue;
                }
                var relData = new FusionRelationshipTableData
                {
                    StartSourceID = item.StartID,
                    EndSourceID = item.EndID
                };

                FusionAttributeTempTableValue fusionInfo = null;
                var sourceAttributeTypeID = 0;
                var targetAttributeTypeID = 0;
                //TRY TO FIND THE FUSIONATTRIBUTE ID FOR BOTH THE START ID AND THE END ID
                if (sourceToIDMapping.TryGetValue(item.StartID, out fusionInfo))
                {
                    if (fusionInfo == null) throw new Exception("INVALID FUSION ATTRIBUTE ENCOUNTERED");

                    sourceAttributeTypeID = fusionInfo.FusionAttributeTypeID;
                    relData.StartFusionAttributeID = fusionInfo.ID;
                }
                if (sourceToIDMapping.TryGetValue(item.EndID, out fusionInfo))
                {
                    if (fusionInfo == null) throw new Exception("INVALID FUSION ATTRIBUTE ENCOUNTERED");

                    targetAttributeTypeID = fusionInfo.FusionAttributeTypeID;
                    relData.EndFusionAttributeID = fusionInfo.ID;
                }

                if (relData.StartFusionAttributeID > 0 && relData.EndFusionAttributeID > 0)
                {
                    var intersectInfo = _workArea.Relationships.IntersectTypeMapping.FirstOrDefault(x => x.SubjectID == sourceAttributeTypeID && x.ObjectID == targetAttributeTypeID);

                    if (intersectInfo == null)
                    {                        
                        continue;
                    }
                    
                    relData.IntersectTypeID = intersectInfo.ID;

                    _workArea.Relationships.ResolvedRelationshipData.Add(relData);
                }                
            }
        }

        private async Task LoadFusionIntersectTypes(SqlConnection companyConnection)
        {
            _workArea.Relationships.IntersectTypeMapping = await companyConnection.QueryAsync<FusionIntersectMapping>(@"
select  I.ID, I.SubjectID, I.ObjectID, P.[Type] as PredicateType
from    [IntersectType] I
        left join [Predicate] P on P.ID = I.PredicateID and I.Subject = 'FusionAttributeType' AND I.Object = 'FusionAttributeType'", commandTimeout: ReadQueryTimeout);

            Log.WriteLine($"LOADED {_workArea.Relationships.IntersectTypeMapping.Count()} INTERSECT TYPE MAPPINGS FROM IntersectType table.");
        }

        /// <summary>
        /// Handles the models for the relationship data
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        private async Task ProcessModels(SqlConnection companyConnection, List<Dictionary<string, string>> models, string functionName, IDictionary<string, string> properties, int companyID)
        {
            Stopwatch sw = Stopwatch.StartNew();
            // RUN QUERY TO GET FIELDS INFO FOR THE FIELDS IN THIS RUN
            await LoadCurrentFusionFieldInfo(companyConnection);
            Log.WriteLine(string.Format("LOADCURRENTFUSIONFIELD INFO TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_LOADCURRENTFUSIONFIELD, sw.Elapsed);

            // RUN QUERY TO GET THE EXISTING FUSIONATTRIBUTES IN THIS RUN
            await LoadCurrentFusionAttributeMap(companyConnection);

            //build a table that contains all the fusionattributes we need to insert
            sw.Restart();
            GenerateFusionAttributeTableValues(models);
            Log.WriteLine(string.Format("GENERATEFUSIONATTRIBUTETABLEVALUES TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_GENERATE_FUSION_ATTR_VALUES, sw.Elapsed);

            // handle fusionattribute updates / inserts
            //we have two cases
            // items that 
            sw.Restart();
            await DoFusionAttributeMerge(companyConnection, functionName, properties, companyID);
            Log.WriteLine(string.Format("DoFusionAttributeMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_MERGE, sw.Elapsed);

            // RUN QUERY TO GET FUSION ATTRIBUTE IDS
            sw.Restart();
            await LoadCurrentFusionAttributeInfo(companyConnection);
            Log.WriteLine(string.Format("LoadCurrentFusionAttributeInfo TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_RELOAD, sw.Elapsed);

            // load all the fusionfield type ids
            sw.Restart();
            await LoadFusionAttributeToFieldTypeIDMap(companyConnection);
            Log.WriteLine(string.Format("LoadFusionAttributeToFieldTypeIDMap TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_FIELDTYPE_MAP, sw.Elapsed);

            // handle fields
            sw.Restart();
            GenerateFusionFieldTableValues(models);
            Log.WriteLine(string.Format("GenerateFusionFieldTableValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_VALS, sw.Elapsed);

            sw.Restart();
            await DoFusionFieldMerge(companyConnection, functionName, properties, companyID);
            Log.WriteLine(string.Format("DoFusionFieldMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_MERGE, sw.Elapsed);

            // fields and attributes now updated need to update any parent ids
            sw.Restart();
            UpdateAttributesWithParentIDValues();
            Log.WriteLine(string.Format("UpdateAttributesWithParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_PARENT_UPDATE, sw.Elapsed);

            //update the parentids by doing a merge
            sw.Restart();
            await UpdateFusionAttributeParentIDs(companyConnection);
            Log.WriteLine(string.Format("MergeUpdatedParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_ATTR_PARENT_UPDATE_MERGE, sw.Elapsed);

            //update old values with values we             
            sw.Restart();
            DetermineChangedFields();
            Log.WriteLine(string.Format("DetermineChangedFields TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_CHANGES, sw.Elapsed);

            sw.Restart();
            DetermineChangedFusionAttributes();
            Log.WriteLine(string.Format("DetermineChangedFusionAttributes TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            CoreFunction.AITrackRequest(FUSION_PROCESSOR_AI_NAME_FUSION_FIELD_ATTR_CHANGES, sw.Elapsed);
        }

        /// <summary>
        /// Handles the query items for custom queries generated by a company administrator.
        /// </summary>
        /// <param name="queryItems"></param>
        /// <returns></returns>
        private async Task ProcessQueryItems(SqlConnection companyConnection, List<IDictionary<string, string>> queryItems)
        {
            Log.WriteLine($"Working with {queryItems.Count} FUSIONQUERYATTRIBUTES");

            Stopwatch sw = Stopwatch.StartNew();

            if (queryItems.Count > 0)
            {
                using (var trans = companyConnection.BeginTransaction())
                {
                    try
                    {
                        #region LOAD QUERY ATTRIBUTES into TEMP table (#FusionQueryAttribute)

                        await companyConnection.ExecuteAsync(@"
        set nocount on 
        create table #FusionQueryAttribute (
            ID int null,
            FusionQueryAttributeTypeID int not null, 
            SourceID varchar(250) not null,
            [Action] varchar(1) not null
        )
        set nocount off", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                        {
                            bulkCopy.BatchSize = queryItems.Count;
                            bulkCopy.DestinationTableName = "#FusionQueryAttribute";
                            bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                            var table = new DataTable();

                            var columnName = "ID";
                            table.Columns.Add(columnName, typeof(int));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "FusionQueryAttributeTypeID";
                            table.Columns.Add(columnName, typeof(int));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "SourceID";
                            table.Columns.Add(columnName, typeof(string));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "Action";
                            table.Columns.Add(columnName, typeof(string));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            foreach (var queryItem in queryItems)
                            {
                                var dr = table.NewRow();
                                dr["FusionQueryAttributeTypeID"] = queryItem["FusionQueryAttributeTypeID"];
                                dr["SourceID"] = queryItem["SourceID"];
                                dr["Action"] = queryItem["Action"];
                                table.Rows.Add(dr);
                            }

                            await bulkCopy.WriteToServerAsync(table);
                        }

                        Log.WriteLine($"LOAD QUERY ATTRIBUTES into TEMP table (#FusionQueryAttribute) TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        #region MERGE query attributes.

                        sw.Restart();

                        await companyConnection.ExecuteAsync(@"
        set nocount on 
        create table #MergeOutputFusionQueryAttribute (
            ID int null,
            FusionQueryAttributeTypeID int not null, 
            SourceID varchar(250) not null,
            [Action] varchar(1) not null
        );
        CREATE NONCLUSTERED INDEX [CIX_Temp_MergeOutputFusionQueryAttribute] ON #MergeOutputFusionQueryAttribute ( FusionQueryAttributeTypeID ASC, SourceID ASC );
        set nocount off", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        //merge temp table with fusion query attributes table
                        await companyConnection.ExecuteAsync(@"
        merge   FusionQueryAttribute as T 
        using   ( 
                select  *
                from    #FusionQueryAttribute 
                ) as S 
                on T.FusionQueryAttributeTypeID = S.FusionQueryAttributeTypeID and T.SourceID = S.SourceID 
        when    matched then 
                update set  T.Deleted = case 
                                        when S.[Action] = 'D' then cast(1 as bit) 
                                        else cast(0 as bit) 
                                       end,
                            T.UpdatedOn = getutcdate(),
                            T.UpdatedBy = 0 
        when    not matched by target then 
                insert (FusionQueryAttributeTypeID, SourceID, Deleted, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy) 
                values (S.FusionQueryAttributeTypeID, S.SourceID, 0, getutcdate(), 0, getutcdate(), 0);", commandTimeout: ExecuteQueryTimeout, transaction: trans);
                        
                        await companyConnection.ExecuteAsync(@"
update	T
set		T.Deleted = 1,
		T.UpdatedOn = getutcdate(),
		T.UpdatedBy = 0
from	FusionQueryAttribute T
		inner join FusionQueryAttributeType TT on TT.FusionID = @f and TT.ID = T.FusionQueryAttributeTypeID
		left join #FusionQueryAttribute S on S.FusionQueryAttributeTypeID = T.FusionQueryAttributeTypeID and S.SourceID = T.SourceID
where	S.SourceID is null;", new { f = FusionID }, commandTimeout: ExecuteQueryTimeout, transaction: trans);


                        Log.WriteLine($"MERGE query attributes TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        #region LOAD QUERY ATTRIBUTES FIELDS into TEMP table (#FusionQueryAttributeField)

                        sw.Restart();

                        var queryAttributeFieldTypes = companyConnection.Query<FusionQueryTypeFieldTypeModel>(@"
        select  t.ID as FusionQueryAttributeTypeID,
                ft.Name as FieldTypeName,
                ft.ID as FieldTypeID
        from    FusionQueryAttributeType t 
                inner join FieldType ft on ft.[Object] = 'FusionQueryAttributeType' and ft.ObjectID = t.ID and t.FusionID = @f;", new { f = FusionID }, transaction: trans).ToList();

                        await companyConnection.ExecuteAsync(@"
        set nocount on 
        create table #FusionQueryAttributeField (
            FusionQueryAttributeID int null,
            FusionQueryAttributeTypeID int not null, 
            SourceID varchar(250) not null,
            FieldTypeID int not null,
            Value nvarchar(max) null
        )
        CREATE NONCLUSTERED INDEX [IX_Temp_FusionQueryAttributeField1] ON #FusionQueryAttributeField ( FusionQueryAttributeTypeID ASC, SourceID ASC );
        CREATE NONCLUSTERED INDEX [IX_Temp_FusionQueryAttributeField2] ON #FusionQueryAttributeField ( FusionQueryAttributeID ASC, FieldTypeID ASC );
        set nocount off", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                        {
                            bulkCopy.BatchSize = queryItems.Count;
                            bulkCopy.DestinationTableName = "#FusionQueryAttributeField";
                            bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                            var table = new DataTable();

                            var columnName = "FusionQueryAttributeID";
                            table.Columns.Add(columnName, typeof(int));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "FusionQueryAttributeTypeID";
                            table.Columns.Add(columnName, typeof(int));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "SourceID";
                            table.Columns.Add(columnName, typeof(string));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "FieldTypeID";
                            table.Columns.Add(columnName, typeof(int));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            columnName = "Value";
                            table.Columns.Add(columnName, typeof(string));
                            bulkCopy.ColumnMappings.Add(columnName, columnName);

                            foreach (var queryItem in queryItems)
                            {
                                var fusionQueryAttributeTypeID = int.Parse(queryItem["FusionQueryAttributeTypeID"]);
                                var fusionQueryAttributeSourceID = queryItem["SourceID"];

                                foreach (var key in queryItem.Keys)
                                {
                                    if (key != "SourceID" && key != "FusionQueryAttributeTypeID")
                                    {
                                        var queryAttributeFieldType = queryAttributeFieldTypes.SingleOrDefault(i => i.FusionQueryAttributeTypeID == fusionQueryAttributeTypeID && string.Compare(i.FieldTypeName,key,true) == 0);
                                        if (queryAttributeFieldType != null)
                                        {
                                            var dr = table.NewRow();
                                            dr["FusionQueryAttributeTypeID"] = fusionQueryAttributeTypeID;
                                            dr["SourceID"] = fusionQueryAttributeSourceID;
                                            dr["FieldTypeID"] = queryAttributeFieldType.FieldTypeID;
                                            dr["Value"] = queryItem[key];
                                            table.Rows.Add(dr);
                                        }
                                    }
                                }
                            }

                            await bulkCopy.WriteToServerAsync(table);
                        }

                        Log.WriteLine($"LOAD QUERY ATTRIBUTES FIELDS into TEMP table (#FusionQueryAttributeField) TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        #region MERGE query attribute fields.

                        sw.Restart();

                        // Update the query fields table with the query attribute ID loaded into #FusionQueryAttribute temp table above.
                        await companyConnection.ExecuteAsync(@"
        update  T 
        set     T.FusionQueryAttributeID = S.ID
        from    #FusionQueryAttributeField T
                inner join FusionQueryAttribute S on 
                    S.FusionQueryAttributeTypeID = T.FusionQueryAttributeTypeID 
                    and S.SourceID = T.SourceID;", commandTimeout: ExecuteQueryTimeout, transaction: trans);
                        //                await companyConnection.ExecuteAsync(@"
                        //update  T 
                        //set     T.FusionQueryAttributeID = S.ID
                        //from    #FusionQueryAttributeField T
                        //        inner join #FusionQueryAttribute S on 
                        //            S.FusionQueryAttributeTypeID = T.FusionQueryAttributeTypeID 
                        //            and S.SourceID = T.SourceID;", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        //merge temp table with fusion query attributes table
                        await companyConnection.ExecuteAsync(@"
        merge   Field as T 
        using   (
                select  *
                from    #FusionQueryAttributeField
                where   FusionQueryAttributeID is not null
                ) as S 
                on (T.ObjectType = 'FusionQueryAttribute' and T.ObjectID = S.FusionQueryAttributeID and T.FieldTypeID = S.FieldTypeID) 
        when    matched then 
                update set  T.Value = S.Value, 
                            T.FormattedValue = S.Value 
        when    not matched by target then 
                insert (ObjectType, ObjectID, FieldTypeID, Value, FormattedValue) 
                values ('FusionQueryAttribute', S.FusionQueryAttributeID, S.FieldTypeID, S.Value, S.Value);", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        Log.WriteLine($"MERGE query attribute fields TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine($"ERROR IN ProcessQueryItems: {ex.GetFullExceptionData()}");
                        trans.Rollback();
                    }
                } //using
            } //if
        }

        /// <summary>
        /// Update the fusionattribute table text path column this needs to be done after the parents are updated.
        /// </summary>
        /// <param name="companyConnection"></param>
        /// <returns></returns>
        private async Task UpdateFusionAttributeTextPaths(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync("[fusion].[UpdateFusionTextPaths]", new { FusionID }, commandTimeout: ExecuteQueryTimeout, commandType: CommandType.StoredProcedure);

        }

        /// <summary>
        /// Change values that have changed from previous run for fusionattribute table
        /// </summary>
        private void DetermineChangedFusionAttributes()
        {
            if (IsFirstRun)
            {
                Log.WriteLine("NOT LOGGING ANY CHANGED FUSION ATTRIBUTE INFO AS THIS IS THE FIRST RUN FOR THIS FUSION ID.");

                _workArea.Changes.AddCount += _workArea.FusionAttributeTempValues.Count();
                
                return;
            }

            //COMPARE FUSION ATTRIBUTE INITIAL VALUE TO NEW ONE           
            foreach (var x in _workArea.FusionAttributeTempValues)
            {

                string oldValue = string.Empty;
                string action = string.Empty;

                DetermineItemChange(_workArea.ExistingFusionAttributes, x.Name, x.SourceID, out action, out oldValue);

                if (!string.IsNullOrEmpty(action))
                    _workArea.Changes.ChangedValues.Add(new FusionChangeTableValue(x, oldValue, action));
            }

        }


        /// <summary>
        /// Find values from the fields table that have changed since previous run
        /// </summary>
        private void DetermineChangedFields()
        {
            if (IsFirstRun)
            {
                Log.WriteLine("NOT LOGGING ANY CHANGED FIELD INFO AS THIS IS THE FIRST RUN FOR THIS FUSION ID.");


                _workArea.Changes.AddCount += _workArea.FieldValueCollection.Count();
                _workArea.FieldValueCollection = null;

                return;
            }

            Dictionary<string, string> oldFieldDict = new Dictionary<string, string>();

            foreach (var item in _workArea.FieldValueCollection)
            {
                if (string.IsNullOrEmpty(item.Value)) continue;

                var key = string.Format("{0}_{1}", item.FieldTypeID, item.ObjectID);

                oldFieldDict.Add(key, item.Value);
            }

            foreach (var x in _workArea.FieldTempValues)
            {
                var key = string.Format("{0}_{1}", x.FieldTypeID, x.ObjectID);
                string oldValue = string.Empty;
                string action = string.Empty;

                DetermineItemChange(oldFieldDict, x.Value, key, out action, out oldValue);

                if (!string.IsNullOrEmpty(action))
                    _workArea.Changes.ChangedValues.Add(new FusionChangeTableValue(x, oldValue, action));
            }

            //free memory used by old values we dont need them anymore
            _workArea.FieldValueCollection = null;
        }

        /// <summary>
        /// Determines is a value has changed from its previous value
        /// </summary>
        /// <param name="sourceID"></param>
        /// <param name="action"></param>
        /// <param name="oldValue"></param>
        private void DetermineItemChange(Dictionary<string, string> oldValueList, string value, string sourceID, out string action, out string oldValue)
        {
            if (!oldValueList.TryGetValue(sourceID, out oldValue) && !string.IsNullOrEmpty(value))
            {
                action = "A";
                _workArea.Changes.AddCount++;
            }
            else if ((string.IsNullOrEmpty(value) && string.IsNullOrEmpty(oldValue)) || (oldValue == value))
            {
                action = string.Empty;
                return;
            }
            else
            {
                action = "U";
                _workArea.Changes.UpdateCount++;
            }
        }

        /// <summary>
        /// Determines is a value has changed from its previous value
        /// </summary>
        /// <param name="sourceID"></param>
        /// <param name="action"></param>
        /// <param name="oldValue"></param>
        private void DetermineItemChange(Dictionary<string, FusionAttributeTempTableValue> oldValueList, string value, string sourceID, out string action, out string oldValue)
        {
            FusionAttributeTempTableValue temp;
            if (!oldValueList.TryGetValue(sourceID, out temp) && !string.IsNullOrEmpty(value))
            {
                action = "A";
                _workArea.Changes.AddCount++;
                oldValue = string.Empty;
                return;
            }

            oldValue = temp.Name;
            if ((string.IsNullOrEmpty(value) && string.IsNullOrEmpty(oldValue)) || (oldValue == value))
            {
                action = string.Empty;
                return;
            }
            else
            {
                action = "U";
                _workArea.Changes.UpdateCount++;
            }
        }

        private async Task UpdateFusionAttributeParentIDs(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"create table #tempParentID([SubjectID] int, [ObjectID] int);", commandTimeout: ExecuteQueryTimeout);

            //insert to the temp table            
            var parentsNeedingUpdates = _workArea.FusionAttributeTempValues.Where(x => x.ParentID > 0);

            var count = parentsNeedingUpdates.Count();

            if (count <= 0) return;

            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
            {
                Log.WriteLine($"INSERTING {count} PARENT/CHILD RELATIONSHIP MAPPINGS INTO TEMPPARENTID TEMP TABLE.");

                bulkCopy.BatchSize = count;
                bulkCopy.DestinationTableName = "#tempParentID";
                bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                var table = new DataTable();

                var columnName = "SubjectID";   // Parent
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "ObjectID";        // Child
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in parentsNeedingUpdates)
                {
                    var row = table.NewRow();

                    if (item.ID <= 0 || item.ParentID <= 0)
                        throw new Exception("ERROR INVALID PARENT CHILD MAPPING. CHILD - " + item.ID.ToString() + " PARENT - " + item.ParentID.ToString());

                    row["SubjectID"] = item.ParentID;
                    row["ObjectID"] = item.ID;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            Log.WriteLine("BULK COPY TO #tempParentID COMPLETED.  UPDATING FUSIONATTRIBUTE PARENTID COLUMN WITH NEW VALUES");

            await companyConnection.ExecuteAsync(@"
merge   [Intersect] as T
using   (
        select  T.ID as IntersectTypeID,
                S.Object as Subject,
                TR.SubjectID,
                O.Object,
                TR.ObjectID
        from    #tempParentID TR
                inner join Asset S on S.Object = 'FusionAttribute' and S.ObjectID = TR.SubjectID
                inner join AssetType ST on ST.ID = S.AssetTypeID
                inner join Asset O on O.Object = 'FusionAttribute' and O.ObjectID = TR.ObjectID
                inner join AssetType OT on OT.ID = O.AssetTypeID
                inner join IntersectType T on T.Subject = ST.Object and T.SubjectID = ST.ObjectID and T.Object = OT.Object and T.ObjectID = OT.ObjectID
                inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
        ) as S
        on T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.Subject and T.SubjectID = S.SubjectID and T.Object = S.Object and T.ObjectID = S.ObjectID
        when matched then
            update set  T.Visible = 1,
                        T.State = 1,
                        T.Deleted = 0
        when not matched then
            insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, State, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Owner, Deleted, Visible)
            values (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 1, 0, getutcdate(), 0, getutcdate(), 'Fusion Load', 0, 1);", commandTimeout: ExecuteQueryTimeout);

            await companyConnection.ExecuteAsync(@"
            update	T
            set		T.ParentID = S.SubjectID
            from	FusionAttribute T
                    inner join #tempParentID S on T.ID = S.ObjectID;
                 ", commandTimeout: ExecuteQueryTimeout);
        }

        private void UpdateAttributesWithParentIDValues()
        {
            Log.WriteLine("UpdateAttributesWithParentIDValues - Updating fusionattributes with the parent id values from the insert process.");
            //fusionattributetempvalues doesnt have any id values need to get them from AttributeMappingCollection
            foreach (var item in _workArea.FusionAttributeTempValues)
            {
                int id = 0;
                if (!_workArea.FusionSourceToIDMap.TryGetValue(item.SourceID, out id))
                {
                    Log.WriteLine("Unable to resolve id of source id[" + item.SourceID + "] This should not happen as we should have inserted this already and reloaded.");

                    continue;
                }

                item.ID = id; //sets the id of this guy

                if (string.IsNullOrEmpty(item.ParentSourceID)) continue; // only add this mapping for items that have parent / child relations

                //AttributeMappingCollection HAS THE ID AND PARENT ID                
                int parentId = 0;

                if (!_workArea.FusionSourceToIDMap.TryGetValue(item.ParentSourceID, out parentId))
                {
                    Log.WriteLine("Unable to resolve parent of source id[" + item.SourceID + "], Parent[" + item.ParentSourceID + "]");
                }
                else
                {
                    item.ParentID = parentId;
                }
            }//);
        }

        /// <summary>
        /// Load all fieldtypeid mappings for fusionattribute types
        /// </summary>
        /// <param name="companyConnection"></param>
        private async Task LoadFusionAttributeToFieldTypeIDMap(SqlConnection companyConnection)
        {
            // TODO: Add cache of fieldtype to fusionattribute type id this is about 500 items
            //  it needs to be by company etc...
            _workArea.FieldToAttributeMapping = await companyConnection.QueryAsync<FusionFieldIDAttributeIDMapping>(@"
                select 
	                objectid as FusionAttributeTypeID,
	                name as FieldTypeName,
	                [id] as FieldTypeID
                 from 
                    fieldtype 
                where [object] = 'FusionAttributeType'
                ", commandTimeout: ReadQueryTimeout);
        }

        private async Task DoFusionFieldMerge(SqlConnection companyConnection, string functionName, IDictionary<string, string> properties, int companyID)
        {
            await companyConnection.ExecuteAsync(@"
                    create table #tempFusionFields(FusionAttributeID int, FieldTypeID int, Value nvarchar(max));
                    CREATE UNIQUE CLUSTERED INDEX PK_tempFusionFields ON #tempFusionFields ([FusionAttributeID] ASC,[FieldTypeID] ASC);
            ", commandTimeout: ExecuteQueryTimeout);

            using (var trans = companyConnection.BeginTransaction())
            {
                //do this in chunks of n max rows
                int chunkSize = MergeChunkSize;
                int chunks = (_workArea.FieldTempValues.Count / chunkSize) + 1;

                CoreFunction.AITrackTrace(
                    functionName, 
                    $"DoFusionFieldMerge - INSERTING {_workArea.FieldTempValues.Count} FIELD VALUES TO #tempFusionFields TEMP TABLE IN {chunkSize} ROW CHUNKS - {chunks} TOTAL CHUNKS.", 
                    properties,
                    companyID
                );

                for (var i = 0; i < chunks; i++)
                {
                    int startIndex = (i * chunkSize);
                    int endIndex = (_workArea.FieldTempValues.Count > (startIndex + chunkSize)) ? (startIndex + chunkSize) : (startIndex + (_workArea.FieldTempValues.Count % chunkSize));

                    if (i > 0)
                    {
                        await companyConnection.ExecuteAsync(@"TRUNCATE TABLE #tempFusionFields", commandTimeout: ExecuteQueryTimeout, transaction: trans);
                    }

                    var size = endIndex - startIndex;

                    Log.WriteLine($"DoFusionFieldMerge - INSERTING {size} FIELD VALUES TO #tempFusionFields TEMP TABLE.");
                    //insert to the temp table

                    using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                    {
                        bulkCopy.BatchSize = size;
                        bulkCopy.DestinationTableName = "#tempFusionFields";
                        bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                        var table = new DataTable();
                        var columnName = "FusionAttributeID";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "FieldTypeID";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "Value";
                        table.Columns.Add(columnName, typeof(string));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        for (var j = startIndex; j < endIndex; j++)
                        {
                            var item = _workArea.FieldTempValues[j];
                            var row = table.NewRow();

                            row["FusionAttributeID"] = item.ObjectID;
                            row["FieldTypeID"] = item.FieldTypeID;
                            row["Value"] = item.Value;

                            table.Rows.Add(row);
                        }

                        await bulkCopy.WriteToServerAsync(table);
                    }

                    Log.WriteLine($"DoFusionFieldMerge - INSERTED {size} FIELD VALUES TO #tempFusionFields TEMP TABLE.");

                    Log.WriteLine("DoFusionFieldMerge - Starting to merge #tempFusionFields with [dbo].[field]");

                    //merge temp table with fields table
                    await companyConnection.ExecuteAsync(@"
                        merge Field as T
                        using (
                            select FusionAttributeID as ObjectID,
                                FieldTypeID,
                                Value                        
                            from #tempFusionFields
                        ) as S
                        on T.ObjectType = 'FusionAttribute' and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
                        when matched then
                            update set  T.Value = S.Value,
                                        T.FormattedValue = S.Value            
                        when not matched then
                            insert (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
                            values (S.FieldTypeID, 'FusionAttribute', S.ObjectID, S.Value, S.Value);
                    ", new { fus = FusionID }, commandTimeout: ExecuteQueryTimeout, transaction: trans);


                }

                trans.Commit();

                Log.WriteLine("DoFusionFieldMerge - Completed merge of #tempFusionFields with [dbo].[field]");
            }
        }

        private void GenerateFusionFieldTableValues(List<Dictionary<string, string>> models)
        {
            foreach (var x in models)
            {
                if (x == null)
                {
                    Log.WriteLine("INVALID MODEL IN MODELS COLLECTION");

                    continue;
                }
                //iterate through models
                // for each additonal field we need to add a new fusionfieldtemptablevalue
                string actionString = string.Empty;
                string name = string.Empty;

                var sourceID = x[SourceIDAttribute];

                x.TryGetValue(NameAttribute, out name);
                var fusionTypeIDString = x[FusionAttributeTypeIDAttribute];

                if (string.IsNullOrEmpty(name)) name = FUSION_ATTRIBUTE_MISSING_NAME_NAME;

                if (string.IsNullOrEmpty(fusionTypeIDString))
                {
                    Log.WriteLine("INVALID KEY IN MODEL KEY VALUE COLLECTION");

                    continue;
                }

                var fusionTypeID = Convert.ToInt32(fusionTypeIDString);

                x.TryGetValue(ActionAttribute, out actionString);

                //get existing fusionattributeid

                int id = 0;
                //if existingItem is null something is wrong
                if (!_workArea.FusionSourceToIDMap.TryGetValue(sourceID, out id))
                {
                    Log.WriteLine($"UNABLE TO LOAD FUSIONATTRIBUTE ID FOR CURRENT ITEM SOURCE ID [{sourceID}] FIELD NAME [{name}] FUSION TYPE ID [{fusionTypeIDString}].");

                    continue;
                }

                foreach (var item in x)
                {
                    if (item.Key == SourceIDAttribute || item.Key == NameAttribute || item.Key == FusionAttributeTypeIDAttribute || item.Key == ParentSourceIDAttribute || item.Key == ActionAttribute)
                        continue;

                    if (string.IsNullOrEmpty(item.Key))
                    {
                        Log.WriteLine("ERROR NULL OR EMPTY FIELD NAME FOR FUSION ATTRIBUTE SOURCE ID : {0}", sourceID);

                        continue;
                    }

                    var fieldInfo = _workArea.FieldToAttributeMapping.FirstOrDefault(z => z.FusionAttributeTypeID == fusionTypeID && string.Compare(z.FieldTypeName, item.Key, true) == 0);

                    if (fieldInfo == null)
                    {
                        Log.WriteLine("Encountered unexpected field for a fusionattributetype.  Cannot find mapping for fusion attribute type id [" + fusionTypeID + "] to a field with the name [" + item.Key + "]");

                        continue;
                    }

                    Field fieldVal = new Field
                    {
                        ObjectID = id,
                        FieldTypeID = fieldInfo.FieldTypeID
                    };

                    if (!string.IsNullOrEmpty(item.Value))
                        fieldVal.Value = item.Value;

                    _workArea.FieldTempValues.Add(fieldVal);
                }
            }
        }

        private async Task DoFusionAttributeMerge(SqlConnection companyConnection, string functionName, IDictionary<string, string> properties, int companyID)
        {
            var sql = @"create table #tempFusionAttributes(FusionAttributeTypeID int, SourceID varchar(250), Name nvarchar(250), Deleted bit, ParentSourceID varchar(250));
                        CREATE UNIQUE CLUSTERED INDEX PK_tempFusionAttributes ON #tempFusionAttributes ([FusionAttributeTypeID] ASC,[SourceID] ASC);";

            await companyConnection.ExecuteAsync(sql, commandTimeout: ExecuteQueryTimeout);

            Log.WriteLine($"INSERTING {_workArea.FusionAttributeTempValues.Count} FUSION ATTRIBUTE VALUES TO #tempFusionAttributes TEMP TABLE.");

            using (var trans = companyConnection.BeginTransaction())
            {
                //do this in chunks of n max rows
                int chunkSize = MergeChunkSize;
                int chunks = (_workArea.FusionAttributeTempValues.Count / chunkSize) + 1;

                CoreFunction.AITrackTrace(
                    functionName,
                    $"DoFusionFieldMerge - INSERTING {_workArea.FusionAttributeTempValues.Count} FIELD VALUES TO #tempFusionAttributes TEMP TABLE IN {chunkSize} ROW CHUNKS - {chunks} TOTAL CHUNKS.",
                    properties,
                    companyID
                );

                for (var i = 0; i < chunks; i++)
                {
                    int startIndex = (i * chunkSize);
                    int endIndex = (_workArea.FusionAttributeTempValues.Count > (startIndex + chunkSize)) ? (startIndex + chunkSize) : (startIndex + (_workArea.FusionAttributeTempValues.Count % chunkSize));

                    if (i > 0)
                    {
                        await companyConnection.ExecuteAsync(@"TRUNCATE TABLE #tempFusionAttributes", commandTimeout: ExecuteQueryTimeout, transaction: trans);
                    }

                    var size = endIndex - startIndex;
                    //insert to the temp table
                    using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                    {
                        bulkCopy.BatchSize = size;
                        bulkCopy.DestinationTableName = "#tempFusionAttributes";
                        bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                        var table = new DataTable();
                        var columnName = "FusionAttributeTypeID";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "SourceID";
                        table.Columns.Add(columnName, typeof(string));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "Name";
                        table.Columns.Add(columnName, typeof(string));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "Deleted";
                        table.Columns.Add(columnName, typeof(bool));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "ParentSourceID";
                        table.Columns.Add(columnName, typeof(string));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        for (var j = startIndex; j < endIndex; j++)
                        {
                            var item = _workArea.FusionAttributeTempValues[j];
                            var row = table.NewRow();

                            row["FusionAttributeTypeID"] = item.FusionAttributeTypeID;
                            row["SourceID"] = item.SourceID;
                            row["Name"] = item.Name;
                            row["Deleted"] = item.IsDeleted();
                            row["ParentSourceID"] = item.ParentSourceID;

                            table.Rows.Add(row);
                        }

                        await bulkCopy.WriteToServerAsync(table);
                    }

                    //merge temp table with fusion attributes table
                    await companyConnection.ExecuteAsync(@"
                    merge FusionAttribute as T
                    using (
                        select FusionAttributeTypeID,
                            SourceID,
                            Name,
                            Deleted
                        from #tempFusionAttributes
                    ) as S
                    on T.FusionID = @fus and T.FusionAttributeTypeID = S.FusionAttributeTypeID and T.SourceID = S.SourceID
                    when matched then
                        update set T.Name = S.Name,
                                    T.Deleted = S.Deleted
                    when not matched then
                        insert (FusionID, FusionAttributeTypeID, SourceID, Name, Deleted)
                        values (@fus, S.FusionAttributeTypeID, S.SourceID, S.Name, S.Deleted);
                    ", new { fus = FusionID }, commandTimeout: ExecuteQueryTimeout, transaction: trans);
                }

                trans.Commit();
            }
        }

        private void GenerateFusionAttributeTableValues(List<Dictionary<string, string>> models)
        {
            //we need to know which models to update / vs insert
            // build in memory table that we will generate temp table from            
            foreach (var x in models)
            {
                if (x.ContainsKey(SourceIDAttribute))
                {
                    string actionString = string.Empty;

                    var sourceID = x[SourceIDAttribute];
                    var fusionTypeID = Convert.ToInt32(x[FusionAttributeTypeIDAttribute]);

                    string name = string.Empty;
                    string parentSourceID = string.Empty;

                    x.TryGetValue(NameAttribute, out name);
                    x.TryGetValue(ParentSourceIDAttribute, out parentSourceID);
                    x.TryGetValue(ActionAttribute, out actionString);

                    if (string.IsNullOrEmpty(name)) name = FUSION_ATTRIBUTE_MISSING_NAME_NAME;

                    if (name.Length > MAX_FIELD_NAME_LENGTH) name = name.Substring(0, MAX_FIELD_NAME_LENGTH);

                    FusionAttributeTempTableValue val = new FusionAttributeTempTableValue
                    {
                        SourceID = sourceID,
                        FusionAttributeTypeID = fusionTypeID,
                        Name = name,
                        DeletedBit = false,
                        ParentSourceID = parentSourceID
                    };

                    val.Action = FusionAttributeTempTableValue.ActionFromString(actionString);

                    if (val.Action == Action.Delete)
                        val.DeletedBit = true;

                    _workArea.FusionAttributeTempValues.Add(val);
                }
            }
        }

        private async Task LoadCurrentFusionFieldInfo(SqlConnection companyConnection)
        {
            // put the fusion attribute id list into a temp table and join to it 
            try
            {
                await companyConnection.ExecuteAsync(@"
                                                        set nocount on 
                                                        create table #tempSourceID(SourceID varchar(250) not null)
                                                        CREATE NONCLUSTERED INDEX [CIX_Temp_TempSourceID] ON #tempSourceID ( SourceID ASC );
                                                        set nocount off", commandTimeout: ExecuteQueryTimeout);

                Log.WriteLine($"INSERTING {_workArea.InSourceIDList.Count} FUSIONATTRIBUTE SOURCE IDS INTO #tempSourceID TEMP TABLE");

                using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
                {
                    bulkCopy.BatchSize = _workArea.InSourceIDList.Count;
                    bulkCopy.DestinationTableName = "#tempSourceID";
                    bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                    var table = new DataTable();
                    var columnName = "SourceID";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    foreach (var id in _workArea.InSourceIDList)
                    {
                        table.Rows.Add(id);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }

                _workArea.FieldValueCollection = await companyConnection.QueryAsync<Field>(@"
                    select 
                        f.ID as 'ObjectID',
	                    ft.ID as 'FieldTypeID',	                    		
	                    fi.Value as 'Value'                    
                    from 
	                    fusionattribute f
	                    inner join fieldtype ft on (f.fusionattributetypeid = ft.objectid and ft.[object] = 'FusionAttributeType')
					    left join field fi on (ft.id = fi.fieldtypeid and fi.objecttype = 'FusionAttribute' and f.id = fi.objectId)
                    where 
	                    f.sourceid in (select SourceID from #tempSourceID)
		                    AND
	                    f.fusionid = @inFusionID
                ", new { inFusionID = FusionID }, commandTimeout: ReadQueryTimeout);


            }
            catch (SqlException sqE)
            {
                Log.WriteLine("SQL ERROR IN LoadCurrentFusionFieldInfo ERROR MESSAGE :" + sqE.Message);
                throw sqE;
            }
        }

        private async Task LoadCurrentFusionAttributeMap(SqlConnection companyConnection)
        {
            //LOAD  FUSION ATTRIBUTE ID , FUSION ATTRIBUTE CURRENT NAME, FUSION ATTRIBUTE PARENT ID, FUSION ATTRIBUTE PARENT NAME
            var results = await companyConnection.QueryAsync<FusionAttributeTempTableValue>(@"
                select 	                
	                f.name as 'Name',	                
                    Upper(f.sourceId) as 'SourceID',
                    f.ID as 'ID',
                    f.FusionAttributeTypeID as 'FusionAttributeTypeID'
                from 
	                fusionattribute f	
                where 	                
	                f.fusionid = @inFusionID and f.deleted != 1
            ", new { inFusionID = FusionID }, commandTimeout: ReadQueryTimeout);

            IsFirstRun = true;

            var errorList = "";

            foreach (var item in results)
            {
                if (IsFirstRun)
                {
                    Log.WriteLine($"FOUND EXISTING DATA FOR FUSION ID {FusionID} SO THIS IS NOT THE FIRST RUN." );
                    IsFirstRun = false;
                }

                if (!string.IsNullOrEmpty(item.SourceID))
                {
                    try
                    {
                        _workArea.ExistingFusionAttributes[item.SourceID] = item;
                    }
                    catch// (Exception iex)
                    {
                        errorList += $"{item.SourceID}; ";
                    }
                }
            }

            if (!string.IsNullOrEmpty(errorList))
            {
                Trace.TraceWarning("Issues exist with the following sourceIDs: {0}", errorList);
            }

            if (IsFirstRun) Log.WriteLine($"NO EXISTING DATA FOUND FOR FUSION ID {FusionID}.  THIS IS THE FIRST RUN.");

            Log.WriteLine($"LOADED {_workArea.ExistingFusionAttributes.Count} EXISTING FUSION ATTRIBUTE VALUES" );
        }

        private async Task LoadCurrentFusionAttributeInfo(SqlConnection companyConnection)
        {
            //LOAD  FUSION ATTRIBUTE ID , FUSION ATTRIBUTE CURRENT NAME, FUSION ATTRIBUTE PARENT ID, FUSION ATTRIBUTE PARENT NAME
            var fusionAttributeInfo = await companyConnection.QueryAsync<FusionAttributeToParentMapping>(@"
                select 
	                f.id as 'ID',	                                
                    Upper(f.sourceId) as 'SourceID'                    	                
                from 
	                fusionattribute f	                	
                where 
	                f.sourceid in (select SourceID from #tempSourceID)
		                AND
	                f.fusionid = @inFusionID
            ", new { inFusionID = FusionID }, commandTimeout: ReadQueryTimeout);

            foreach (var item in fusionAttributeInfo)
            {
                _workArea.FusionSourceToIDMap[item.SourceID] = item.ID;
            }
        }

        private void RemoveRelationSpaces(FusionRelationshipModels relationships)
        {
            HashSet<string> existingRelations = new HashSet<string>();

            for (int i = relationships.Count - 1; i >= 0; i--)
            {
                relationships[i].EndID = (relationships[i].EndID ?? "").Replace(" ", string.Empty).ToUpper();
                relationships[i].StartID = (relationships[i].StartID ?? "").Replace(" ", string.Empty).ToUpper();

                if (string.IsNullOrEmpty(relationships[i].StartID))
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A RELATIONSHIP MISSING A VALID STARTID.  START ID:[{0}] END ID:[{1}] - IGNORING", relationships[i].StartID, relationships[i].EndID);

                    relationships.RemoveAt(i);

                    continue;
                }

                if (string.IsNullOrEmpty(relationships[i].EndID))
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A RELATIONSHIP MISSING A VALID ENDID.  START ID:[{0}] END ID:[{1}] - IGNORING", relationships[i].StartID, relationships[i].EndID);

                    relationships.RemoveAt(i);

                    continue;
                }

                if (relationships[i].StartID == relationships[i].EndID)
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A RELATIONSHIP THAT REFERENCES ITSELF.  START ID:[{0}] END ID:[{1}] - IGNORING", relationships[i].StartID, relationships[i].EndID);

                    relationships.RemoveAt(i);

                    continue;
                }

                var relKey = $"{relationships[i].EndID}-{relationships[i].StartID}";

                if (existingRelations.Contains(relKey))
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A DUPLICATE RELATION.  START ID:[{0}] END ID:[{1}] - IGNORING", relationships[i].StartID, relationships[i].EndID);

                    relationships.RemoveAt(i);

                    continue;
                }

                existingRelations.Add(relKey);
            }
        }

        private void CleanModelData(List<Dictionary<string, string>> models)
        {
            HashSet<string> existingSourceIDs = new HashSet<string>();
            HashSet<string> parentSourceIDs = new HashSet<string>();

            for (int i = models.Count - 1; i >= 0; i--)
            {
                string sourceID = string.Empty;
                string parentSourceID = string.Empty;
                //try to get the SourceID attribute 
                if (!models[i].TryGetValue(SourceIDAttribute, out sourceID))
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A NODE MISSING A VALID SOURCE ID.  DATA:[{0}]", string.Join(";", models[i]));

                    models.RemoveAt(i); // remove this item

                    continue;
                }

                if (string.IsNullOrEmpty(sourceID))
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A NODE MISSING A VALID SOURCE ID.  DATA:[{0}]", string.Join(";", models[i]));

                    models.RemoveAt(i); // remove this item

                    continue;
                }
                else
                {
                    sourceID = sourceID.Replace(" ", string.Empty).ToUpper();
                    models[i][SourceIDAttribute] = sourceID;

                    if (models[i].TryGetValue(ParentSourceIDAttribute, out parentSourceID))
                    {
                        if (!string.IsNullOrEmpty(parentSourceID))
                            models[i][ParentSourceIDAttribute] = parentSourceID.Replace(" ", string.Empty).ToUpper();
                    }

                    //make sure this item doesnt exist more than once
                    if (existingSourceIDs.Contains(sourceID))
                    {
                        Trace.TraceWarning("INPUT FUSION DATA CONTAINS THE SAME SOURCEID VALUE MULTIPLE TIMES.  SOURCE ID:[{0}] MODEL:[{1}]", sourceID, string.Join(";", models[i]));

                        models.RemoveAt(i);
                    }

                    existingSourceIDs.Add(sourceID);

                    _workArea.InSourceIDList.Add(sourceID);

                    if (!string.IsNullOrEmpty(parentSourceID))
                        parentSourceIDs.Add(parentSourceID);
                }
            }

            //add unique list of parent source ids to source id list
            if (parentSourceIDs.Count > 0)
                _workArea.InSourceIDList.AddRange(parentSourceIDs);
        }
    }
}
