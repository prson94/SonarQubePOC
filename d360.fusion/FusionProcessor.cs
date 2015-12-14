using d360.core.entities;
using d360.utils.company;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data.SqlTypes;
using d360.extensions;
using System.IO;
using Newtonsoft.Json;
using System.Data;

namespace d360.fusion
{
    public class FusionProcessor
    {
        public int CompanyID { get; private set; }
        public int FusionID { get; private set; }
        public int ExecutionID { get; private set; }
        public string LogFileName { get; private set; }

        private FusionWorkArea _workArea = new FusionWorkArea();

        private static string SourceIDAttribute = "SourceID";

        private static int MAX_FIELD_VALUE_LENGTH = 4000;
        public async Task Process(FusionProcessingData fusionData)
        {
            
            ///TODO add fusion and company id to fusion data
            //this needs to come from the fusion data
            CompanyID = fusionData.CompanyID;

            //this needs to come from the fusion data
            FusionID = fusionData.FusionID;

            LogFileName = fusionData.LogFileName;

            IStorageProvider storageProvider = new d360.extensions.storage.AzureStorageProvider();
            BulkFusionImport data = null;
            var folderName = string.Format("bulk-fusion-{0}", fusionData.CompanyID);
            //load json from azure

            Stopwatch sw = Stopwatch.StartNew();
            Trace.WriteLine("STARTING JSON DATA READ");
            // TODO change azure read to async
            //   using (var s = storageProvider.GetFile(folderName, fusionData.LogFileName))
            {
           //     using (StreamReader r = new StreamReader(s))
                {
                    string json = storageProvider.GetFileContentsAsString(folderName, fusionData.LogFileName);
                    data = JsonConvert.DeserializeObject<BulkFusionImport>(json);
                }
            }

            if (data == null) throw new Exception("UNABLE TO LOAD FUSION DATA FROM AZURE STORAGE / NULL FUSION DATA OBJECT.");

            Trace.WriteLine(string.Format("COMPLETED JSON DATA READ\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // Sanitize data
            //remove spaces from values in models           
            // this can be done in parrellel
            var cTask = Task.Run(() => RemoveModelSpaces(data.Models));
            var fTask = Task.Run(() => RemoveRelationSpaces(data.Relationships));

            // wait for this to finish
            await cTask;
            await fTask;

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(CompanyID))
            {
                companyConnection.Open();
                //Generate an execution id
                sw.Restart();                
                ExecutionID = await LogExecution(companyConnection);
                Trace.WriteLine(string.Format("LogExecution\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

                //Process Models                
                await ProcessModels(companyConnection, data.Models);

                //Process Relationships
                await ProcessRelationships(data.Relationships);

                sw.Restart();
                await SaveChangedValuesLog(companyConnection);
                Trace.WriteLine(string.Format("SaveChangedValuesLog\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

                //Update the executionID to say this is done
                await UpdateExecutionWithStats(companyConnection);

            }
        }

        private async Task SaveChangedValuesLog(SqlConnection companyConnection)
        {
            if (_workArea.ChangedValues.Count <= 0) return;
            // TODO: Save changed fields / values to [fusion].[result] table.  Right now this uses the guid from fusion queue...
            //[fusion].[ResultEx]

            //bulk sql insert to the resultex table
            using (var bulkCopy = new SqlBulkCopy(companyConnection))
            {
                bulkCopy.BatchSize = _workArea.ChangedValues.Count();
                bulkCopy.DestinationTableName = "[fusion].[resultex]";

                var table = new DataTable();
                var columnName = "ExecutionID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FusionAttributeID";
                table.Columns.Add(columnName, typeof(int));
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

                foreach (var item in _workArea.ChangedValues)
                {
                    var row = table.NewRow();

                    row["ExecutionID"] = ExecutionID;
                    row["FusionAttributeID"] = item.FusionAttributeID;
                    row["FieldTypeID"] = item.FieldTypeID;
                    var fieldInfo = _workArea.FieldToAttributeMapping.FirstOrDefault(x => x.FieldTypeID == item.FieldTypeID);
                    if(fieldInfo != null)
                        row["FieldName"] = fieldInfo.FieldTypeName;
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
                ",new { date = DateTime.UtcNow, id = ExecutionID, a = _workArea.AddCount, u = _workArea.UpdateCount, d = _workArea.DeleteCount });
        }

        private async Task<int> LogExecution(SqlConnection companyConnection)
        {
            //insert a record into the fusion execution table that logs the start of this execution
            //insert into fusion.execution (queueID,fusionID,RawLogFileName,DateStarted)
            var result = await companyConnection.QueryAsync<int>(@"
                    insert 
                        into [fusion].[execution] ([queueID],[fusionID],[RawLogFileName],[DateStarted])
                        values('F4EEC459-9DEF-4A3D-BDCA-EC34849CAE08',@inFusionID,@log,@started);
                        SELECT CAST(SCOPE_IDENTITY() as int)
            ", new { inFusionID = FusionID, log = LogFileName, started = DateTime.UtcNow });

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Handles the relationships for the fusion data
        /// </summary>
        /// <param name="relationships"></param>
        /// <returns></returns>
        private async Task ProcessRelationships(FusionRelationshipModels relationships)
        {
            // TODO: Implement the relationship part
            //  we need to create a temp table in memory and join on fusion attributes to determine which
            // relatoinships exist dont exist...
        }

        /// <summary>
        /// Handles the models for the relationship data
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        private async Task ProcessModels(SqlConnection companyConnection, List<Dictionary<string, string>> models)
        {
            Stopwatch sw = Stopwatch.StartNew();   
            // RUN QUERY TO GET FIELDS INFO FOR THE SAME
            await LoadCurrentFusionFieldInfo(companyConnection);
            Trace.WriteLine(string.Format("LOADCURRENTFUSIONFIELD INFO TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            //build a table that contains all the fusionattributes we need to insert
            sw.Restart();
            GenerateFusionAttributeTableValues(models);
            Trace.WriteLine(string.Format("GENERATEFUSIONATTRIBUTETABLEVALUES TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // handle fusionattribute updates / inserts
            //we have two cases
            // items that 
            sw.Restart();
            await DoFusionAttributeMerge(companyConnection);
            Trace.WriteLine(string.Format("DoFusionAttributeMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // RUN QUERY TO GET FUSION ATTRIBUTE IDS
            sw.Restart();
            await LoadCurrentFusionAttributeInfo(companyConnection);
            Trace.WriteLine(string.Format("LoadCurrentFusionAttributeInfo TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // load all the fusionfield type ids
            sw.Restart();
            await LoadFusionAttributeToFieldTypeIDMap(companyConnection);
            Trace.WriteLine(string.Format("LoadFusionAttributeToFieldTypeIDMap TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // handle fields
            sw.Restart();
            GenerateFusionFieldTableValues(models);
            Trace.WriteLine(string.Format("GenerateFusionFieldTableValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            sw.Restart();
            await DoFusionFieldMerge(companyConnection);
            Trace.WriteLine(string.Format("DoFusionFieldMerge TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            // fields and attributes now updated need to update any parent ids
            sw.Restart();
            UpdateAttributesWithParentIDValues();
            Trace.WriteLine(string.Format("UpdateAttributesWithParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            //update the parentids by doing a merge
            sw.Restart();
            await MergeUpdatedParentIDValues(companyConnection);
            Trace.WriteLine(string.Format("MergeUpdatedParentIDValues TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            sw.Restart();
            await UpdateFusionAttributeTextPaths(companyConnection);
            Trace.WriteLine(string.Format("UpdateFusionAttributeTextPaths TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

            //update old values with values we             
            sw.Restart();
            DetermineChangedFields();
            Trace.WriteLine(string.Format("DetermineChangedFields TOOK\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
        }

        private async Task UpdateFusionAttributeTextPaths(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"
                UPDATE     FusionAttribute
                SET        TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
                WHERE   FusionID = @fus
            ", new { fus = FusionID });
        }

        private void DetermineChangedFields()
        {
            SortedList<string, string> oldFieldDict = new SortedList<string, string>();

            foreach (var item in _workArea.FieldValueCollection)
            {
                if (string.IsNullOrEmpty(item.CurrentValue)) continue;

                var key = string.Format("{0}_{1}", item.FieldTypeID, item.FusionAttributeID);

                oldFieldDict.Add(key, item.CurrentValue);
            }

          //  Parallel.ForEach(_workArea.FieldTempValues, x =>
            foreach( var x in _workArea.FieldTempValues)
            {
                //  var oldVal = _workArea.FieldValueCollection.FirstOrDefault(y => y.FieldTypeID == x.FieldTypeID && y.FusionAttributeID == x.FusionAttributeID);
                var key = string.Format("{0}_{1}", x.FieldTypeID, x.FusionAttributeID);
                string oldValue = string.Empty;

                if (!oldFieldDict.TryGetValue(key,out oldValue) && !string.IsNullOrEmpty(x.Value))
                {
                    x.Action = "A";
                    _workArea.AddCount++;
                }                
                else if((string.IsNullOrEmpty(x.Value) && string.IsNullOrEmpty(oldValue)) || (oldValue == x.Value))
                {
                    continue;
                }
                else
                {
                    x.Action = "U";
                    _workArea.UpdateCount++;
                }

                _workArea.ChangedValues.Add(x);
            }//);            
        }

        private async Task MergeUpdatedParentIDValues(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"create table #tempParentID([ID] int, [ParentID] int);");

            //insert to the temp table
            //await companyConnection.ExecuteAsync("insert into #tempParentID ([ID],[ParentID]) values(@ID,@ParentID)", _workArea.FusionAttributeTempValues);

            var parentsNeedingUpdates = _workArea.FusionAttributeTempValues.Where(x => x.ParentID > 0);

            using (var bulkCopy = new SqlBulkCopy(companyConnection))
            {
                bulkCopy.BatchSize = parentsNeedingUpdates.Count();
                bulkCopy.DestinationTableName = "#tempParentID";

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

            await companyConnection.ExecuteAsync(@"
                merge FusionAttribute as T
                using (
                    select ID,
                        ParentID
                    from #tempParentID
                ) as S
                on T.ID = S.ID
                when matched then
                    update set T.ParentID = S.ParentID;                                               
            ", new { fus = FusionID });
        }

        private void UpdateAttributesWithParentIDValues()
        {
            //fusionattributetempvalues doesnt have any id values need to get them from AttributeMappingCollection
            foreach (var item in _workArea.FusionAttributeTempValues)
            {
                if (string.IsNullOrEmpty(item.ParentSourceID)) continue; // only add this mapping for items that have parent / child relations

                var dbItem = _workArea.AttributeMappingCollection.FirstOrDefault(y => y.SourceID == item.SourceID);

                if(dbItem == null)
                {
                    Trace.WriteLine("Unable to resolve id of source id[" + item.SourceID + "] This should not happen as we should have inserted this already and reloaded.");

                    continue;
                }

                item.ID = dbItem.ID; //sets the id of this guy

                //AttributeMappingCollection HAS THE ID AND PARENT ID
                var parent = _workArea.AttributeMappingCollection.FirstOrDefault(y => y.SourceID == item.ParentSourceID);

                if (parent == null)
                {
                    Trace.WriteLine("Unable to resolve parent of source id[" + item.SourceID + "], Parent[" + item.ParentSourceID + "]");
                }
                else
                {
                    item.ParentID = parent.ID;
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
                ");
        }

        private async Task DoFusionFieldMerge(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"create table #tempFusionFields(FusionAttributeID int, FieldTypeID int, Value nvarchar(4000))");

            //insert to the temp table

            using (var bulkCopy = new SqlBulkCopy(companyConnection))
            {
                bulkCopy.BatchSize = _workArea.FieldTempValues.Count;
                bulkCopy.DestinationTableName = "#tempFusionFields";

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

                    row["FusionAttributeID"] = item.FusionAttributeID;
                    row["FieldTypeID"] = item.FieldTypeID;
                    row["Value"] = item.Value;                    

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }


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
            ", new { fus = FusionID }, null,10000);
        }

        private void GenerateFusionFieldTableValues(List<Dictionary<string, string>> models)
        {            
            //Parallel.ForEach(models, x =>
            foreach(var x in models)
            {
                if(x == null)
                {
                    Trace.WriteLine("INVALID MODEL IN MODELS COLLECTION");

                    continue;
                }
                //iterate through models
                // for each additonal field we need to add a new fusionfieldtemptablevalue
                string actionString = string.Empty;

                var sourceID = x["SourceID"];
                var name = x["Name"];
                var fusionTypeIDString = x["FusionAttributeTypeID"];
                

                if(string.IsNullOrEmpty(fusionTypeIDString))
                {
                    Trace.WriteLine("INVALID KEY IN MODEL KEY VALUE COLLECTION");

                    continue;
                }

                var fusionTypeID = Convert.ToInt32(fusionTypeIDString);

                x.TryGetValue("Action", out actionString);

                //get existing fusionattributeid
                var existingItem = _workArea.AttributeMappingCollection.FirstOrDefault(y => sourceID == y.SourceID);

                //if existingItem is null something is wrong
                if (existingItem == null) throw new Exception("UNABLE TO LOAD FUSIONATTRIBUTE ID FOR CURRENT ITEM.");

                foreach (var item in x)
                {
                    if (item.Key == "SourceID" || item.Key == "Name" || item.Key == "FusionAttributeTypeID" || item.Key == "ParentSourceID")
                        continue;

                    if(string.IsNullOrEmpty(item.Key))
                    {
                        Trace.WriteLine("INVALID KEY IN MODEL KEY VALUE COLLECTION");

                        continue;
                    }

                    var fieldInfo = _workArea.FieldToAttributeMapping.FirstOrDefault(z => z.FusionAttributeTypeID == fusionTypeID && z.FieldTypeName == item.Key);

                    if(fieldInfo == null)
                    {
                        Trace.WriteLine("Encountered unexpected field for a fusionattributetype.  Cannot find mapping for fusion attribute type id [" + fusionTypeID + "] to a field with the name [" + item.Key + "]");

                        continue;
                    }

                    FusionFieldTempTableValue fieldVal = new FusionFieldTempTableValue
                    {
                        FusionAttributeID = existingItem.ID,                        
                        Value = (item.Value.Length > MAX_FIELD_VALUE_LENGTH ? item.Value.Substring(0, MAX_FIELD_VALUE_LENGTH) : item.Value),
                        FieldTypeID = fieldInfo.FieldTypeID
                    };

                    _workArea.FieldTempValues.Add(fieldVal);
                }                
            }
        }

        private async Task DoFusionAttributeMerge(SqlConnection companyConnection)
        {
            await companyConnection.ExecuteAsync(@"create table #tempFusionAttributes(FusionAttributeTypeID int, SourceID varchar(250), Name nvarchar(250), Deleted bit, ParentSourceID varchar(250))");

            //insert to the temp table
            
            using (var bulkCopy = new SqlBulkCopy(companyConnection))
            {
                bulkCopy.BatchSize = _workArea.FusionAttributeTempValues.Count;
                bulkCopy.DestinationTableName = "#tempFusionAttributes";

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
            ", new { fus = FusionID });
        }

        private void GenerateFusionAttributeTableValues(List<Dictionary<string, string>> models)
        {
            //we need to know which models to update / vs insert
            // build in memory table that we will generate temp table from
            //Parallel.ForEach(models, x =>  //parallel for is slower with about 10 k models due to need for concurent bag
            foreach (var x in models)
            {
                string actionString = string.Empty;

                var sourceID = x["SourceID"];
                var name = x["Name"];
                var fusionTypeID = Convert.ToInt32(x["FusionAttributeTypeID"]);

                string parentSourceID = string.Empty;

                x.TryGetValue("ParentSourceID", out parentSourceID);


                x.TryGetValue("Action", out actionString);

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
                                                        set nocount off");

                var columnName = "SourceID";
                using (var bulkCopy = new SqlBulkCopy(companyConnection))
                {
                    bulkCopy.BatchSize = _workArea.InSourceIDList.Count;
                    bulkCopy.DestinationTableName = "#tempSourceID";

                    var table = new DataTable();
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    foreach (var id in _workArea.InSourceIDList)
                    {
                        table.Rows.Add(id);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }

                _workArea.FieldValueCollection = await companyConnection.QueryAsync<FusionFieldValues>(@"
                select 
                    f.ID as 'FusionAttributeID',
	                ft.ID as 'FieldTypeID',	                    		
	                fi.Value as 'CurrentValue'                    
                from 
	                fusionattribute f
	                inner join fieldtype ft on (f.fusionattributetypeid = ft.objectid and ft.[object] = 'FusionAttributeType')
					left join field fi on (ft.id = fi.fieldtypeid and fi.objecttype = 'FusionAttribute' and f.id = fi.objectId)
                where 
	                f.sourceid in (select * from #tempSourceID)
		                AND
	                f.fusionid = @inFusionID
                ", new { inFusionID = FusionID });


            }
            catch (SqlException sqE)
            {
                Trace.WriteLine("SQL ERROR IN LoadCurrentFusionFieldInfo ERROR MESSAGE :" + sqE.Message);
                throw sqE;
            }
        }

        private async Task LoadCurrentFusionAttributeInfo(SqlConnection companyConnection)
        {
            //LOAD  FUSION ATTRIBUTE ID , FUSION ATTRIBUTE CURRENT NAME, FUSION ATTRIBUTE PARENT ID, FUSION ATTRIBUTE PARENT NAME

            _workArea.AttributeMappingCollection = await companyConnection.QueryAsync<FusionAttributeToParentMapping>(@"
                select 
	                f.id as 'ID',
	                f.name as 'Name',
	                f.parentId as 'ParentID',
                    Upper(f.sourceId) as 'SourceID',                    
	                fP.Name as 'ParentName'
                from 
	                fusionattribute f
	                left join fusionattribute fP on f.ParentID = fP.ID
	
                where 
	                f.sourceid in (select * from #tempSourceID)
		                AND
	                f.fusionid = @inFusionID
            ", new { inFusionID = FusionID });
        }

        private void RemoveRelationSpaces(FusionRelationshipModels relationships)
        {
            foreach (var item in relationships)
            {
                item.EndID = item.EndID.Replace(" ", string.Empty).ToUpper();
                item.StartID = item.StartID.Replace(" ", string.Empty).ToUpper();
            }
        }

        private void RemoveModelSpaces(List<Dictionary<string, string>> models)
        {
            foreach (var item in models)
            {
                string sourceID = string.Empty;
                string parentSourceID = string.Empty;
                //try to get the SourceID attribute 
                if (!item.TryGetValue(SourceIDAttribute, out sourceID))
                {
                    Trace.WriteLine("RemoveModelSpaces found node in models with no SourceID value");

                    continue;
                }

                item[SourceIDAttribute] = sourceID.Replace(" ", string.Empty).ToUpper();

                if(item.TryGetValue("ParentSourceID", out parentSourceID))
                {
                    if(!string.IsNullOrEmpty(parentSourceID))
                        item["ParentSourceID"] = parentSourceID.Replace(" ", string.Empty).ToUpper();
                }

                _workArea.InSourceIDList.Add(sourceID);
            }
        }
    }
}
