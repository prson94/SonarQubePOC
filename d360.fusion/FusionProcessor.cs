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
using Microsoft.ApplicationInsights;

namespace d360.fusion
{
    public class FusionProcessor
    {
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

        private FusionWorkArea _workArea = new FusionWorkArea();

        private static string SourceIDAttribute = "SourceID";
        private static string ParentSourceIDAttribute = "ParentSourceID";
        private static string NameAttribute = "Name";
        private static string ActionAttribute = "Action";
        private static string FusionAttributeTypeIDAttribute = "FusionAttributeTypeID";

        private static int MAX_FIELD_VALUE_LENGTH = 4000;
        private static int MAX_SOURCEID_LENGTH = 250;
        private static string FUSION_ATTRIBUTE_MISSING_NAME_NAME = "Name not resolved";
        private static string FUSION_PROCESSOR_AI_NAME = "FusionProcessor";

        public async Task Process(FusionProcessingData fusionData, int bulkTimeout, int readTimeout, int executeTimeout)
        {
            BulkFusionImport data = null;

            var jobDuration = System.Diagnostics.Stopwatch.StartNew();
            var ai = new TelemetryClient();
            var metrics = new Dictionary<string, double>();

            ai.Context.Operation.Id = Guid.NewGuid().ToString();
            ai.Context.Operation.Name = FUSION_PROCESSOR_AI_NAME;
            
            BulkCopyTimeout = bulkTimeout;
            ReadQueryTimeout = readTimeout;
            ExecuteQueryTimeout = executeTimeout;
            
            CompanyID = fusionData.CompanyID;

            FusionID = fusionData.FusionID;

            LogFileName = fusionData.LogFileName;

            if (CompanyID <= 0) throw new Exception("Invalid company id specified.");

            if (string.IsNullOrEmpty(LogFileName)) throw new Exception("Error invalid or no file specified to process fusion data from");

            if (FusionID <= 0) throw new Exception("Invalid fusion id specified.");

            ai.Context.Properties["CompanyID"] = CompanyID.ToString();
            ai.Context.Properties["FusionID"] = FusionID.ToString();
            ai.Context.Properties["FileName"] = LogFileName;
            ai.Context.Properties["SQLBulkCopyTimeout"] = BulkCopyTimeout.ToString();
            ai.Context.Properties["SQLReadQueryTimeout"] = ReadQueryTimeout.ToString();
            ai.Context.Properties["SQLExecutionTimeout"] = ExecuteQueryTimeout.ToString();
            
            Trace.TraceInformation("====================================================================================================");
            Trace.TraceInformation("STARTING FUSION JOB FOR FUSION ID: {0} COMPANY ID: {1} FILE: {2}", FusionID, CompanyID, LogFileName);            
                                   
            IStorageProvider storageProvider = new d360.extensions.storage.AzureStorageProvider();
                
            var folderName = string.Format("bulk-fusion-{0}", fusionData.CompanyID);
            //load json from azure

            Stopwatch sw = Stopwatch.StartNew();
            Trace.TraceInformation("STARTING JSON DATA READ");

            string json = storageProvider.GetFileContentsAsString(folderName, fusionData.LogFileName, Encoding.UTF8);
            data = JsonConvert.DeserializeObject<BulkFusionImport>(json);

            if (data == null) throw new Exception("UNABLE TO LOAD FUSION DATA FROM AZURE STORAGE / NULL FUSION DATA OBJECT.");

            Trace.TraceInformation(string.Format("COMPLETED JSON DATA READ\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            Trace.TraceInformation("FUSION JOB HAS {0} MODELS, {1} RELATIONS", data.Models.Count, data.Relationships.Count);

            metrics["MODELS"] = data.Models.Count;
            metrics["QUERYITEMS"] = data.QueryItems.Count;
            metrics["RELATIONS"] = data.Relationships.Count;
            metrics["JSON DATA SIZE"] = json.Length;

            ai.TrackEvent("Fusion Job Starting", null, metrics);

            // Sanitize data
            //remove spaces from values in models           
            // this can be done in parrellel
            var cTask = Task.Run(() => CleanModelData(data.Models));
            var fTask = Task.Run(() => RemoveRelationSpaces(data.Relationships));

            // wait for this to finish
            await cTask;
            await fTask;
            
            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(CompanyID))
            {
                try
                {                    
                    companyConnection.Open();
                    //Generate an execution id                                        

                    sw.Restart();
                    ExecutionID = await LogExecution(companyConnection,data.Version);
                    Trace.TraceInformation(string.Format("LogExecution\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

                    Trace.TraceInformation("Processing fusion execution ID: [{0}]", ExecutionID);
                    
                    //Process Models                
                    await ProcessModels(companyConnection, data.Models);

                    if (data.QueryItems != null)
                    {
                        await ProcessQueryItems(companyConnection, data.QueryItems);
                    }

                    //Process Relationships
                    await ProcessRelationships(companyConnection, data.Relationships);


                    sw.Restart();
                    await SaveChangedValuesLog(companyConnection);
                    Trace.TraceInformation(string.Format("SaveChangedValuesLog\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

                    //Update the executionID to say this is done
                    await UpdateExecutionWithStats(companyConnection);

                    //If any changes were made add record to queue.task
                    await UpdateQueue(companyConnection);                    
                }
                catch (AggregateException exception)
                {                                           
                    ai.TrackException(exception);

                    Trace.TraceError("FusionProcessor::Process encountered and error while running fusion job.");
                    foreach (Exception ex in exception.InnerExceptions)
                    {
                        Trace.TraceError("Exception details [{0}]", ex.Message);
                        LogFusionError(companyConnection, ex);
                    }
                    
                    throw exception;
                }
                catch (Exception ex)
                {   
                    ai.TrackException(ex);

                    Trace.TraceError("FusionProcessor::Process encountered and error while running fusion job.  Exception details [{0}]", ex.Message);

                    LogFusionError(companyConnection, ex);

                    throw ex;
                }
            }
            jobDuration.Stop();

            metrics["Duration(s)"] = jobDuration.ElapsedMilliseconds / 1000;
            
            ai.TrackEvent("Fusion Job Complete", null, metrics);

            ai.TrackRequest(FUSION_PROCESSOR_AI_NAME, DateTime.Now, jobDuration.Elapsed, "", true); 
        }

        private async Task UpdateQueue(SqlConnection companyConnection)
        {
            if (_workArea.Changes.AddCount > 0 || _workArea.Changes.UpdateCount > 0 || _workArea.Changes.DeleteCount > 0)
            {
                await companyConnection.ExecuteAsync(@"
                    insert into [queue].[task] ([Action], [Object], [ObjectID]) values ('Notify','FusionExecution',@id)                    
                ", new { id = ExecutionID }, commandTimeout: ExecuteQueryTimeout);

                await companyConnection.ExecuteAsync(@"
                    insert into [queue].[task] ([Action], [Object], [ObjectID]) values ('FusionCache','Fusion',@id)                    
                ", new { id = FusionID  }, commandTimeout: ExecuteQueryTimeout);
            }
        }
        
        private void LogFusionError(SqlConnection companyConnection, Exception ex)
        {
            if (ex == null || ExecutionID <= 0)
            {
                Trace.TraceError("UNABLE TO LOG ERROR TO [FUSION].[ERROR] TABLE EXECUTION ID IS NULL OR EXCEPTION OBJECT IS NULL");

                return;
            }
            companyConnection.Execute(@"
                                            insert into [fusion].[error] ([ExecutionID],[Date],[Error]) values(@ID,CURRENT_TIMESTAMP,@message);
                                        ", new { message = ex.ToString(), ID = ExecutionID });
        }

        private async Task SaveChangedValuesLog(SqlConnection companyConnection)
        {
            if (_workArea.Changes.ChangedValues.Count <= 0) return;
            
            //bulk sql insert to the resultex table
            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock,null))
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
                    if(!string.IsNullOrEmpty(item.OldValue) && item.OldValue.Length > 250)
                        row["OldValue"] = item.OldValue.Substring(0,250);
                    else
                        row["OldValue"] = item.OldValue;

                    if(!string.IsNullOrEmpty(item.Value) && item.Value.Length > 250)
                        row["NewValue"] = item.Value.Substring(0,250);
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
                ",new { date = DateTime.UtcNow, id = ExecutionID, a = _workArea.Changes.AddCount, u = _workArea.Changes.UpdateCount, d = _workArea.Changes.DeleteCount }, commandTimeout: ExecuteQueryTimeout);
        }

        private async Task<int> LogExecution(SqlConnection companyConnection,string version)
        {
            if (string.IsNullOrEmpty(version)) version = "unknown";
            //TODO : remove queueID from fusion.execution table or make it nullable.
            //insert a record into the fusion execution table that logs the start of this execution
            //insert into fusion.execution (queueID,fusionID,RawLogFileName,DateStarted)
            var result = await companyConnection.QueryAsync<int>(@"
                    insert 
                        into [fusion].[execution] ([queueID],[fusionID],[RawLogFileName],[DateStarted],[Version])
                        values('F4EEC459-9DEF-4A3D-BDCA-EC34849CAE08',@inFusionID,@log,@started,@ver);
                        SELECT CAST(SCOPE_IDENTITY() as int)
            ", new { inFusionID = FusionID, log = LogFileName, started = DateTime.UtcNow,ver =version }, commandTimeout:ReadQueryTimeout);
            
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Handles the relationships for the fusion data
        /// </summary>
        /// <param name="relationships"></param>
        /// <returns></returns>
        private async Task ProcessRelationships(SqlConnection companyConnection, FusionRelationshipModels relationships)
        {
            if(relationships.Count == 0)
            {
                Trace.TraceInformation("NO RELATIONS SPECIFIED AS PART OF FUSION JOB SKIPPING PROCESSRELATIONSHIPS.");

                return;
            }

            //Load the intersect types
            await LoadFusionIntersectTypes(companyConnection);
                        
            //build mapping of fusion attributes ids to intersect types
            GenerateRelationshipInsertData(relationships);
            
            // insert unresolved relations to the stagingrelationunresolved table
            await DoUnresolvedRelationsInsert(companyConnection);

            // determine which relations already exist and remove them
            await DoResolvedRelationsInsert(companyConnection);
        }
        
        private async Task DoResolvedRelationsInsert(SqlConnection companyConnection)
        {
            // insert all the resolved relation into into a temp table
            await companyConnection.ExecuteAsync(@"create table #tempResolvedRel([ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, IntersectTypeID int, SourceIntersectTypeID int, TargetIntersectTypeID int,  StartFusionAttributeID int, EndFusionAttributeID int)", commandTimeout: ExecuteQueryTimeout);

            Trace.TraceInformation("WRITING {0} RESOLVED RELATIONSHIPS TO #TEMPRESOLVEDREL TEMP TABLE.", _workArea.Relationships.ResolvedRelationshipData.Count);

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


            // delete any relations that already exist from the temp table
            // delete from temp table where 
            var rowsDeleted = await companyConnection.ExecuteAsync(@"
delete  #tempResolvedRel 
where 	[ID] in (
                select  sr.id
				from    #tempResolvedRel sr 
                        inner join [Intersect] I on I.Subject = 'FusionAttribute' and 
                                                    I.Object = 'FusionAttribute' and 
                                                    (
                                                        ( I.SubjectID = sr.startfusionattributeid and I.ObjectID = sr.endfusionattributeid ) OR
                                                        ( I.SubjectID = sr.endfusionattributeid and I.ObjectID = sr.startfusionattributeid  )
                                                    )
                );", commandTimeout: ExecuteQueryTimeout);

            Trace.TraceInformation("DELETED {0} RELATIONS FROM TEMPRESOLVEDREL TABLE AS PRE-EXISTING RELATIONSHIPS.", rowsDeleted);

            if(_workArea.Relationships.ResolvedRelationshipData.Count == rowsDeleted)
            {
                Trace.TraceInformation("NO NEW RELATIONS TO INSERT EXITING");
                return;
            }

            // do the 3 inserts into the db using the temp table
            await companyConnection.ExecuteAsync(@"
declare @Intersects IDTable;
declare @objectType varchar(50) = 'FusionAttribute';			
Declare @IDList Table(IntersectID int, StageID Int);
			
MERGE
	INTO    [Intersect] d
	USING   (
			SELECT	IntersectTypeID, 
					ID,
					StartFusionAttributeID,
					EndFusionAttributeID
			FROM	[fusion].stagingrelation
			where	ExecutionID = @executionID 
					and IntersectID is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
	VALUES  (S.IntersectTypeID, 2, NULL, @objectType, StartFusionAttributeID, @objectType, EndFusionAttributeID)
	OUTPUT  INSERTED.ID, S.ID into @IDList;
	
--update StagingRelation to have the id's we used in intersect table.
UPDATE	T
SET		T.IntersectID = S.IntersectID
from	[fusion].[StagingRelation] T
		inner join @IDList S on T.ExecutionID = @executionID and T.ID = S.StageID;

insert into @Intersects 
	select	IntersectID 
	from	@IDList;
	
declare @IntersectCount int
select @IntersectCount = count(1) from @Intersects
if @IntersectCount > 0 
begin
	EXEC cache.SynchronizeRelationships @Intersects
end", new { executionID = ExecutionID }, commandTimeout: ExecuteQueryTimeout);
        }

        /// <summary>
        /// Insert relationships between start /end id's we cant figure out 
        /// into the unresolved relations table
        /// </summary>
        /// <returns></returns>
        private async Task DoUnresolvedRelationsInsert(SqlConnection companyConnection)
        {
            if(_workArea.Relationships.UnresolvedRelationshipData.Count == 0)
            {
                Trace.TraceInformation("NO UNRESOLVED RELATIONS EXITING DoUnresolvedRelationsInsert.");

                return;
            }

            await companyConnection.ExecuteAsync(@"create table #tempUnresolvedRel(StartID varchar(250), EndID nvarchar(250))", commandTimeout: ExecuteQueryTimeout);

            Trace.TraceInformation("INSERTING {0} UNRESOLVED RELATIONSHIPS INTO TEMPUNRESOLVEDREL TEMP TABLE.", _workArea.Relationships.UnresolvedRelationshipData.Count);

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
            Dictionary<string, FusionAttributeTempTableValue> sourceToIDMapping = new Dictionary<string, FusionAttributeTempTableValue>();

            //existing fusion values, there may be some that we didnt update 
            foreach (var item in _workArea.ExistingFusionAttributes)
            {
                sourceToIDMapping[item.Key] = item.Value;
            }

            // this is the id's of the updated items / new items
            foreach (var item in _workArea.FusionAttributeTempValues)
            {
                FusionAttributeTempTableValue temp;
                if (sourceToIDMapping.TryGetValue(item.SourceID, out temp))
                {
                    if(item.ID > 0)
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
                if(string.IsNullOrEmpty(item.StartID) || string.IsNullOrEmpty(item.EndID))
                {
                    Trace.TraceInformation("FOUND INVALID RELATIONSHIP CONTAINING NULL START/END VALUE FOR STARTID [" + item.StartID + "] ENDID [" + item.EndID + "].  DISREGARDING AS INVALID");

                    continue;
                }

                if(item.StartID.Length > MAX_SOURCEID_LENGTH)
                {
                    Trace.TraceInformation("FOUND INVALID STARTID STARTID [" + item.StartID + "] IS GREATER THAN MAX SOURCEID LENGTH OF [" + MAX_SOURCEID_LENGTH + "].  DISREGARDING AS INVALID.");

                    continue;
                }

                if (item.EndID.Length > MAX_SOURCEID_LENGTH)
                {
                    Trace.TraceInformation("FOUND INVALID ENDID STARTID [" + item.EndID + "] IS GREATER THAN MAX SOURCEID LENGTH OF [" + MAX_SOURCEID_LENGTH + "].  DISREGARDING AS INVALID.");

                    continue;
                }
                FusionRelationshipTableData relData = new FusionRelationshipTableData
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

                if(relData.StartFusionAttributeID > 0 && relData.EndFusionAttributeID > 0)
                {
                    var intersectInfo = _workArea.Relationships.IntersectTypeMapping.FirstOrDefault(x => x.SubjectID == sourceAttributeTypeID && x.ObjectID == targetAttributeTypeID);

                    if(intersectInfo == null)
                    {
                        Trace.TraceWarning("ENCOUNTERED INTERSECT MAPPING THAT DOESNT HAVE A RELATIONSHIP IN DB. SOURCE ATTRIBUTE TYPE ID [{0}] TARGET ATTRIBUTE TYPE ID [{1}]", sourceAttributeTypeID, targetAttributeTypeID);

                        continue;
                    }

                    //relData.EndIntersectTypeID = intersectInfo.TargetIntersectTypeNodeID;
                    //relData.StartIntersectTypeID = intersectInfo.SourceIntersectTypeNodeID;
                    relData.IntersectTypeID = intersectInfo.ID;

                    _workArea.Relationships.ResolvedRelationshipData.Add(relData);
                }
                else
                {                    
                    Trace.TraceInformation("FOUND UNRESOLVED RELATIONSHIP BETWEEN START SOURCEID:[{0}] AND END SOURCEID:[{1}]", item.StartID, item.EndID);

                    _workArea.Relationships.UnresolvedRelationshipData.Add(relData);
                }
            }
        }

        private async Task LoadFusionIntersectTypes(SqlConnection companyConnection)
        {
            _workArea.Relationships.IntersectTypeMapping = await companyConnection.QueryAsync<FusionIntersectMapping>(@"
select  ID, SubjectID, ObjectID
from    [IntersectType]
where   Subject = 'FusionAttributeType'", commandTimeout: ReadQueryTimeout);

            Trace.TraceInformation("LOADED {0} INTERSECT TYPE MAPPINGS FROM IntersectType table.", _workArea.Relationships.IntersectTypeMapping.Count());
        }

        /// <summary>
        /// Handles the models for the relationship data
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        private async Task ProcessModels(SqlConnection companyConnection, List<Dictionary<string, string>> models)
        {            
            Stopwatch sw = Stopwatch.StartNew();   
            // RUN QUERY TO GET FIELDS INFO FOR THE FIELDS IN THIS RUN
            await LoadCurrentFusionFieldInfo(companyConnection);
            Trace.TraceInformation(string.Format("LOADCURRENTFUSIONFIELD INFO TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // RUN QUERY TO GET THE EXISTING FUSIONATTRIBUTES IN THIS RUN
            await LoadCurrentFusionAttributeMap(companyConnection);

            //build a table that contains all the fusionattributes we need to insert
            sw.Restart();
            GenerateFusionAttributeTableValues(models);
            Trace.TraceInformation(string.Format("GENERATEFUSIONATTRIBUTETABLEVALUES TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // handle fusionattribute updates / inserts
            //we have two cases
            // items that 
            sw.Restart();
            await DoFusionAttributeMerge(companyConnection);
            Trace.TraceInformation(string.Format("DoFusionAttributeMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // RUN QUERY TO PUT FUSION ATTRIBUTES INTO CACHE
            sw.Restart();
            await DoFusionAttributeCache(companyConnection);
            Trace.TraceInformation(string.Format("DoFusionAttributeCache TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // RUN QUERY TO GET FUSION ATTRIBUTE IDS
            sw.Restart();
            await LoadCurrentFusionAttributeInfo(companyConnection);
            Trace.TraceInformation(string.Format("LoadCurrentFusionAttributeInfo TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // load all the fusionfield type ids
            sw.Restart();
            await LoadFusionAttributeToFieldTypeIDMap(companyConnection);
            Trace.TraceInformation(string.Format("LoadFusionAttributeToFieldTypeIDMap TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // handle fields
            sw.Restart();
            GenerateFusionFieldTableValues(models);
            Trace.TraceInformation(string.Format("GenerateFusionFieldTableValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            sw.Restart();
            await DoFusionFieldMerge(companyConnection);
            Trace.TraceInformation(string.Format("DoFusionFieldMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // fields and attributes now updated need to update any parent ids
            sw.Restart();
            UpdateAttributesWithParentIDValues();
            Trace.TraceInformation(string.Format("UpdateAttributesWithParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            //update the parentids by doing a merge
            sw.Restart();
            await UpdateFusionAttributeParentIDs(companyConnection);
            Trace.TraceInformation(string.Format("MergeUpdatedParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            if (models.Count > 0)
            {
                sw.Restart();
                await UpdateFusionAttributeTextPaths(companyConnection);
                Trace.TraceInformation(string.Format("UpdateFusionAttributeTextPaths TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
            }
            else
            {
                Trace.TraceInformation("NO MODELS SPECIFIED SKIPPING UPDATEFUSIONATTRIBUTETEXTPATHS");
            }

            //update old values with values we             
            sw.Restart();
            DetermineChangedFields();
            Trace.TraceInformation(string.Format("DetermineChangedFields TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            sw.Restart();
            DetermineChangedFusionAttributes();
            Trace.TraceInformation(string.Format("DetermineChangedFusionAttributes TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
        }

        /// <summary>
        /// Handles the query items for custom queries generated by a company administrator.
        /// </summary>
        /// <param name="queryItems"></param>
        /// <returns></returns>
        private async Task ProcessQueryItems(SqlConnection companyConnection, List<IDictionary<string, string>> queryItems)
        {
            Trace.TraceInformation($"Working with {queryItems.Count} FUSIONQUERYATTRIBUTES");

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
                                dr["Action"] = "A";// queryItem["Action"];
                                table.Rows.Add(dr);
                            }

                            await bulkCopy.WriteToServerAsync(table);
                        }

                        Trace.TraceInformation($"LOAD QUERY ATTRIBUTES into TEMP table (#FusionQueryAttribute) TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

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
        when    not matched then 
                insert (FusionQueryAttributeTypeID, SourceID, Deleted, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy) 
                values (S.FusionQueryAttributeTypeID, S.SourceID, 0, getutcdate(), 0, getutcdate(), 0)
        output  inserted.ID, S.FusionQueryAttributeTypeID, S.SourceID, S.[Action] into #MergeOutputFusionQueryAttribute;", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        //merge temp table with fusion query attributes table
                        await companyConnection.ExecuteAsync(@"
        update  T 
        set     T.ID = S.ID
        from    #FusionQueryAttribute T
                inner join #MergeOutputFusionQueryAttribute S on 
                    S.FusionQueryAttributeTypeID = T.FusionQueryAttributeTypeID 
                    and S.SourceID = T.SourceID;", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        Trace.TraceInformation($"MERGE query attributes TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

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
                                        var queryAttributeFieldType = queryAttributeFieldTypes.SingleOrDefault(i => i.FusionQueryAttributeTypeID == fusionQueryAttributeTypeID && i.FieldTypeName == key);
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

                        Trace.TraceInformation($"LOAD QUERY ATTRIBUTES FIELDS into TEMP table (#FusionQueryAttributeField) TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        #region MERGE query attribute fields.

                        sw.Restart();

                        // Update the query fields table with the query attribute ID loaded into #FusionQueryAttribute temp table above.
                        await companyConnection.ExecuteAsync(@"
        update  T 
        set     T.FusionQueryAttributeID = S.ID
        from    #FusionQueryAttributeField T
                inner join #FusionQueryAttribute S on 
                    S.FusionQueryAttributeTypeID = T.FusionQueryAttributeTypeID 
                    and S.SourceID = T.SourceID;", commandTimeout: ExecuteQueryTimeout, transaction: trans);

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
                update set T.Value = S.Value 
        when    not matched then 
                insert (ObjectType, ObjectID, FieldTypeID, Value) 
                values ('FusionQueryAttribute', S.FusionQueryAttributeID, S.FieldTypeID, S.Value);", commandTimeout: ExecuteQueryTimeout, transaction: trans);

                        Trace.TraceInformation($"MERGE query attribute fields TOOK\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                        #endregion

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"ERROR IN ProcessQueryItems: {ex.GetFullExceptionData()}");
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
            await companyConnection.ExecuteAsync(@"
                UPDATE     FusionAttribute
                SET        TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
                WHERE   FusionID = @fus
            ", new { fus = FusionID }, commandTimeout:ExecuteQueryTimeout);
        }

        /// <summary>
        /// Change values that have changed from previous run for fusionattribute table
        /// </summary>
        private void DetermineChangedFusionAttributes()
        {
            if (IsFirstRun)
            {
                Trace.TraceInformation("NOT LOGGING ANY CHANGED FUSION ATTRIBUTE INFO AS THIS IS THE FIRST RUN FOR THIS FUSION ID.");

                return;
            }

            //COMPARE FUSION ATTRIBUTE INITIAL VALUE TO NEW ONE           
            foreach (var x in _workArea.FusionAttributeTempValues)
            {
                
                string oldValue = string.Empty;
                string action = string.Empty;

                DetermineItemChange(_workArea.ExistingFusionAttributes, x.Name, x.SourceID, out action, out oldValue);

                if(!string.IsNullOrEmpty(action))
                    _workArea.Changes.ChangedValues.Add(new FusionChangeTableValue(x,oldValue,action));
            }

        }

       
        /// <summary>
        /// Find values from the fields table that have changed since previous run
        /// </summary>
        private void DetermineChangedFields()
        {
            if(IsFirstRun)
            {
                Trace.TraceInformation("NOT LOGGING ANY CHANGED FIELD INFO AS THIS IS THE FIRST RUN FOR THIS FUSION ID.");

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
            
            foreach( var x in _workArea.FieldTempValues)
            {                
                var key = string.Format("{0}_{1}", x.FieldTypeID, x.ObjectID);
                string oldValue = string.Empty;
                string action = string.Empty;

                DetermineItemChange(oldFieldDict, x.Value, key, out action, out oldValue);

                if (!string.IsNullOrEmpty(action))
                    _workArea.Changes.ChangedValues.Add(new FusionChangeTableValue(x,oldValue, action));
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
        private void DetermineItemChange(Dictionary<string,string> oldValueList, string value, string sourceID, out string action, out string oldValue)
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
            await companyConnection.ExecuteAsync(@"create table #tempParentID([ID] int PRIMARY KEY, [ParentID] int);", commandTimeout: ExecuteQueryTimeout);

            //insert to the temp table            
            var parentsNeedingUpdates = _workArea.FusionAttributeTempValues.Where(x => x.ParentID > 0);

            var count = parentsNeedingUpdates.Count();

            if (count <= 0) return;

            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, null))
            {                
                Trace.TraceInformation("INSERTING {0} PARENT/CHILD RELATIONSHIP MAPPINGS INTO TEMPPARENTID TEMP TABLE.", count);

                bulkCopy.BatchSize = count;
                bulkCopy.DestinationTableName = "#tempParentID";
                bulkCopy.BulkCopyTimeout = BulkCopyTimeout;

                var table = new DataTable();
                var columnName = "ID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "ParentID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in parentsNeedingUpdates)
                {
                    var row = table.NewRow();

                    if (item.ID <= 0 || item.ParentID <= 0)
                        throw new Exception("ERROR INVALID PARENT CHILD MAPPING. CHILD - " + item.ID.ToString() + " PARENT - " + item.ParentID.ToString());

                    row["ID"] = item.ID;
                    row["ParentID"] = item.ParentID;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            Trace.TraceInformation("BULK COPY TO #tempParentID COMPLETED.  UPDATING FUSIONATTRIBUTE PARENTID COLUMN WITH NEW VALUES");

            await companyConnection.ExecuteAsync(@"
                update	T
			    set		T.ParentID = S.ParentID
			    from	FusionAttribute T
					    inner join #tempParentID S on T.ID = S.ID;
            ", commandTimeout: ExecuteQueryTimeout);
        }

        private void UpdateAttributesWithParentIDValues()
        {
            Trace.TraceInformation("UpdateAttributesWithParentIDValues - Updating fusionattributes with the parent id values from the insert process.");
            //fusionattributetempvalues doesnt have any id values need to get them from AttributeMappingCollection
            foreach (var item in _workArea.FusionAttributeTempValues)
            {
                if (string.IsNullOrEmpty(item.ParentSourceID)) continue; // only add this mapping for items that have parent / child relations

                int id = 0;
                if (!_workArea.FusionSourceToIDMap.TryGetValue(item.SourceID, out id))
                {
                    Trace.TraceInformation("Unable to resolve id of source id[" + item.SourceID + "] This should not happen as we should have inserted this already and reloaded.");

                    continue;
                }

                item.ID = id; //sets the id of this guy

                //AttributeMappingCollection HAS THE ID AND PARENT ID                
                int parentId = 0;

                if (!_workArea.FusionSourceToIDMap.TryGetValue(item.ParentSourceID, out parentId))
                {
                    Trace.TraceInformation("Unable to resolve parent of source id[" + item.SourceID + "], Parent[" + item.ParentSourceID + "]");
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
                ", commandTimeout:ReadQueryTimeout);
        }

        private async Task DoFusionFieldMerge(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"
                    create table #tempFusionFields(FusionAttributeID int, FieldTypeID int, Value nvarchar(4000));
                    CREATE UNIQUE CLUSTERED INDEX PK_tempFusionFields ON #tempFusionFields ([FusionAttributeID] ASC,[FieldTypeID] ASC);
            ", commandTimeout: ExecuteQueryTimeout);


            using (var trans = companyConnection.BeginTransaction())
            {

                Trace.TraceInformation("DoFusionFieldMerge - INSERTING {0} FIELD VALUES TO #tempFusionFields TEMP TABLE.", _workArea.FieldTempValues.Count);
                //insert to the temp table

                using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                {
                    bulkCopy.BatchSize = _workArea.FieldTempValues.Count;
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

                    foreach (var item in _workArea.FieldTempValues)
                    {
                        var row = table.NewRow();

                        row["FusionAttributeID"] = item.ObjectID;
                        row["FieldTypeID"] = item.FieldTypeID;
                        row["Value"] = item.Value;

                        table.Rows.Add(row);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }

                Trace.TraceInformation("DoFusionFieldMerge - INSERTED {0} FIELD VALUES TO #tempFusionFields TEMP TABLE.", _workArea.FieldTempValues.Count);

                Trace.TraceInformation("DoFusionFieldMerge - Starting to merge #tempFusionFields with [dbo].[field]");

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
                            update set T.Value = S.Value                                
                        when not matched then
                            insert (FieldTypeID, ObjectType, ObjectID, Value)
                            values (S.FieldTypeID, 'FusionAttribute', S.ObjectID, S.Value);
                    ", new { fus = FusionID }, commandTimeout: ExecuteQueryTimeout, transaction:trans);

                trans.Commit();

                Trace.TraceInformation("DoFusionFieldMerge - Completed merge of #tempFusionFields with [dbo].[field]");
            }
        }

        private void GenerateFusionFieldTableValues(List<Dictionary<string, string>> models)
        {               
            foreach(var x in models)
            {
                if(x == null)
                {
                    Trace.TraceInformation("INVALID MODEL IN MODELS COLLECTION");

                    continue;
                }
                //iterate through models
                // for each additonal field we need to add a new fusionfieldtemptablevalue
                string actionString = string.Empty;

                var sourceID = x[SourceIDAttribute];
                var name = x[NameAttribute];
                var fusionTypeIDString = x[FusionAttributeTypeIDAttribute];
                

                if(string.IsNullOrEmpty(fusionTypeIDString))
                {
                    Trace.TraceInformation("INVALID KEY IN MODEL KEY VALUE COLLECTION");

                    continue;
                }

                var fusionTypeID = Convert.ToInt32(fusionTypeIDString);

                x.TryGetValue(ActionAttribute, out actionString);

                //get existing fusionattributeid
                
                int id = 0;
                //if existingItem is null something is wrong
                if (!_workArea.FusionSourceToIDMap.TryGetValue(sourceID, out id))
                {
                    Trace.TraceError("UNABLE TO LOAD FUSIONATTRIBUTE ID FOR CURRENT ITEM SOURCE ID [{0}] FIELD NAME [{1}] FUSION TYPE ID [{2}].", sourceID,name, fusionTypeIDString);

                    continue;
                }

                foreach (var item in x)
                {
                    if (item.Key == SourceIDAttribute || item.Key == NameAttribute || item.Key == FusionAttributeTypeIDAttribute || item.Key == ParentSourceIDAttribute)
                        continue;

                    if(string.IsNullOrEmpty(item.Key))
                    {
                        Trace.TraceInformation("ERROR NULL OR EMPTY FIELD NAME FOR FUSION ATTRIBUTE SOURCE ID : {0}", sourceID);

                        continue;
                    }

                    var fieldInfo = _workArea.FieldToAttributeMapping.FirstOrDefault(z => z.FusionAttributeTypeID == fusionTypeID && string.Compare(z.FieldTypeName,item.Key, true) == 0);

                    if(fieldInfo == null)
                    {
                        Trace.TraceInformation("Encountered unexpected field for a fusionattributetype.  Cannot find mapping for fusion attribute type id [" + fusionTypeID + "] to a field with the name [" + item.Key + "]");

                        continue;
                    }

                    Field fieldVal = new Field
                    {                    
                        ObjectID = id,                                                
                        FieldTypeID = fieldInfo.FieldTypeID
                    };

                    if (!string.IsNullOrEmpty(item.Value))
                        fieldVal.Value = (item.Value.Length > MAX_FIELD_VALUE_LENGTH ? item.Value.Substring(0, MAX_FIELD_VALUE_LENGTH) : item.Value);

                    _workArea.FieldTempValues.Add(fieldVal);
                }                
            }
        }

        private async Task DoFusionAttributeMerge(SqlConnection companyConnection)
        {               
            var sql = @"create table #tempFusionAttributes(FusionAttributeTypeID int, SourceID varchar(250), Name nvarchar(250), Deleted bit, ParentSourceID varchar(250));
                        CREATE UNIQUE CLUSTERED INDEX PK_tempFusionAttributes ON #tempFusionAttributes ([FusionAttributeTypeID] ASC,[SourceID] ASC);";
            
            await companyConnection.ExecuteAsync(sql, commandTimeout: ExecuteQueryTimeout);
            
            Trace.TraceInformation("INSERTING {0} FUSION ATTRIBUTE VALUES TO #tempFusionAttributes TEMP TABLE.", _workArea.FusionAttributeTempValues.Count);

            using (var trans = companyConnection.BeginTransaction())
            {
                //insert to the temp table
                using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, trans))
                {
                    bulkCopy.BatchSize = _workArea.FusionAttributeTempValues.Count;
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

                    foreach (var item in _workArea.FusionAttributeTempValues)
                    {
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

                trans.Commit();
            }
        }

        private async Task DoFusionAttributeCache(SqlConnection companyConnection)
        {
            Trace.TraceInformation("INSERTING {0} FUSION ATTRIBUTE IDs TO cache.Object TABLE.", _workArea.FusionAttributeTempValues.Count);

            using (var trans = companyConnection.BeginTransaction())
            {
                //merge temp table with fusion attributes table
                await companyConnection.ExecuteAsync(@"
merge	cache.[Object] as T
using	(
		SELECT	'FusionAttribute' as [Object],
				ID as ObjectID,
				'FusionAttributeType' as ObjectType,
				FusionAttributeTypeID as ObjectTypeID
		FROM	FusionAttribute
		where	FusionID = @id
		) as S
on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
		)
when matched then
		update	
		set		T.ObjectType = S.ObjectType,
				T.ObjectTypeID = S.ObjectTypeID
when not matched then
		insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
		values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );", 
        new { id = FusionID }, commandTimeout: ExecuteQueryTimeout, transaction: trans);

                trans.Commit();
            }
        }

        private void GenerateFusionAttributeTableValues(List<Dictionary<string, string>> models)
        {
            //we need to know which models to update / vs insert
            // build in memory table that we will generate temp table from            
            foreach (var x in models)
            {
                string actionString = string.Empty;

                var sourceID = x[SourceIDAttribute];
                var name = x[NameAttribute];
                var fusionTypeID = Convert.ToInt32(x[FusionAttributeTypeIDAttribute]);

                string parentSourceID = string.Empty;

                x.TryGetValue(ParentSourceIDAttribute, out parentSourceID);                
                x.TryGetValue(ActionAttribute, out actionString);

                if (string.IsNullOrEmpty(name)) name = FUSION_ATTRIBUTE_MISSING_NAME_NAME;

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

        private async Task LoadCurrentFusionFieldInfo(SqlConnection companyConnection)
        {
            // put the fusion attribute id list into a temp table and join to it 
            try {
                await companyConnection.ExecuteAsync(@"
                                                        set nocount on 
                                                        create table #tempSourceID(SourceID varchar(250) not null)
                                                        set nocount off", commandTimeout: ExecuteQueryTimeout);

                Trace.TraceInformation("INSERTING {0} FUSIONATTRIBUTE SOURCE IDS INTO #tempSourceID TEMP TABLE", _workArea.InSourceIDList.Count);
                
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
	                    f.sourceid in (select * from #tempSourceID)
		                    AND
	                    f.fusionid = @inFusionID
                ", new { inFusionID = FusionID },commandTimeout:ReadQueryTimeout);


            }
            catch (SqlException sqE)
            {
                Trace.TraceInformation("SQL ERROR IN LoadCurrentFusionFieldInfo ERROR MESSAGE :" + sqE.Message);
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
	                f.fusionid = @inFusionID
            ", new { inFusionID = FusionID },commandTimeout:ReadQueryTimeout);

            IsFirstRun = true;

            foreach (var item in results)
            {
                if (IsFirstRun)
                {
                    Trace.TraceInformation("FOUND EXISTING DATA FOR FUSION ID {0} SO THIS IS NOT THE FIRST RUN.", FusionID);
                    IsFirstRun = false;
                }
                               
                _workArea.ExistingFusionAttributes[item.SourceID] = item;
            }

            if(IsFirstRun) Trace.TraceInformation("NO EXISTING DATA FOUND FOR FUSION ID {0}.  THIS IS THE FIRST RUN.", FusionID);

            Trace.TraceInformation("LOADED {0} EXISTING FUSION ATTRIBUTE VALUES", _workArea.ExistingFusionAttributes.Count);
        }

        private async Task LoadCurrentFusionAttributeInfo(SqlConnection companyConnection)
        {
            //LOAD  FUSION ATTRIBUTE ID , FUSION ATTRIBUTE CURRENT NAME, FUSION ATTRIBUTE PARENT ID, FUSION ATTRIBUTE PARENT NAME
            IEnumerable<FusionAttributeToParentMapping> fusionAttributeInfo  = await companyConnection.QueryAsync<FusionAttributeToParentMapping>(@"
                select 
	                f.id as 'ID',	                                
                    Upper(f.sourceId) as 'SourceID'                    	                
                from 
	                fusionattribute f	                	
                where 
	                f.sourceid in (select * from #tempSourceID)
		                AND
	                f.fusionid = @inFusionID
            ", new { inFusionID = FusionID },commandTimeout:ReadQueryTimeout);

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

                if(string.IsNullOrEmpty(relationships[i].StartID))
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

                if(relationships[i].StartID == relationships[i].EndID)
                {
                    Trace.TraceWarning("FUSION PROCESSING FOUND A RELATIONSHIP THAT REFERENCES ITSELF.  START ID:[{0}] END ID:[{1}] - IGNORING", relationships[i].StartID, relationships[i].EndID);

                    relationships.RemoveAt(i);

                    continue;
                }

                var relKey = $"{relationships[i].EndID}-{relationships[i].StartID}";

                if(existingRelations.Contains(relKey))
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
                }
            }            
        }
    }
}
