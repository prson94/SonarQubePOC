using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.model.DataAccessLayer;
using Dapper;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    partial class CompanyContext : BaseContext
    {
        public string BulkLoadStatusMsg { get; set; }

        #region DbSets

        public DbSet<Load> Loads { get; set; }

        public DbSet<LoadItem> LoadItems { get; set; }

        public DbSet<LoadItemColumn> LoadItemColumns { get; set; }

        public DbSet<LoadColumn> LoadColumns { get; set; }

        #endregion

        #region Engine Methods

        #region Get Methods

        string LoadDetailBaseSql = @"select	L.ID,
		L.[Object],
		L.ObjectID,
		case 
			when L.[Action] = 'M' and L.ObjectID = 0 then 'Group Membership'
			when L.[Action] = 'M' and L.ObjectID = 1 then 'Users'
            when L.[Action] in ('P','R','U') then coalesce(C_D.[Name], '[Deleted]')  
			else coalesce(C_D.[Name], 'Default') 
		end as ObjectName,
		L.Notes, 
        coalesce(EA.ErrorMessage, '' ) + iif(EA.ErrorMessage is null, '', '; ') + coalesce(EE.ErrorMessage, '' ) as ErrorMessage,
		'MyFile.' + L.Extension as FilePath,
		L.DateStarted,
		case when L.Action = 'P' and L.[File] is null then
            case when (L.PutExecutionId is not null and EE.CompletedOn is null) or (L.PostExecutionId is not null and EA.CompletedOn is null) then
                null
            when coalesce(EE.CompletedOn, '1/1/1900') > coalesce(EA.CompletedOn, '1/1/1900') then
                EE.CompletedOn
            else
                EA.CompletedOn      
            end
        else 
            L.DateCompleted 
        end as DateCompleted,
		case L.[Action]
			when 'M' then 'Users/Groups'
            when 'P' then 'Promotion'
			when 'R' then 'Relation'
			when 'U' then 'Unrelation'
            when 'BL' then 'Lineage : Business'
            when 'L' then 'Lineage'
            when 'DL' then 'Remove Lineage : Business'
            when 'N' then 'Lineage : Business'
            when 'O' then 'Responsibilities'
            when 'T' then 'Lineage : Technical'
            when 'S' then 'Synonyms'
			when 'W' then 'Promotion (via Propose Workflow)'
		end as [Action],
        S.C as Success,
        E.C as Error,
        I.C as Incomplete,
		T.C as Total,
        R.FirstName + ' ' + R.LastName as Requestor
from	[Load] L
        left join api.Execution EE on EE.ExecutionId = L.PutExecutionID
        left join api.Execution EA on EA.ExecutionId = L.PostExecutionID
		left join (
			select [Name], [Object] ,ObjectID from AssetType
			union all
			select ITN.[Name] as [Name], 'IntersectType' as [Object], ID as ObjectID from IntersectType IT
			cross apply dbo.GetIntersectTypeNames(IT.ID) ITN

		) C_D on C_D.[Object] = L.[Object] and C_D.ObjectID = L.ObjectID 
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy       
        {0}";

        public IEnumerable<LoadDetail> GetLoadDetails()
        {
            var countSql = @"
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T
";
            return Query<LoadDetail>(string.Format(LoadDetailBaseSql, countSql) + " order by L.ID desc");
        }

        public LoadDetail GetLoadDetail(int id)
        {
            var countSql = @"
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T
";

            var load = GetById<Load>(id);

            if (load.Action == "P" && (load.PostExecutionID != null || load.PutExecutionID != null))
            {
                countSql = @"
		cross apply (
				select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 1
			) S
		cross apply (
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 0
				union all
				select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				) R
			) E
		cross apply (
				select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success is null
			) I
		cross apply (
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				union all
				select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				) R
			) T";
            }

            return Query<LoadDetail>(string.Format(LoadDetailBaseSql, countSql) + " where L.ID = " + id).SingleOrDefault();
        }

        public IEnumerable<dynamic> GetLoadColumnDetails(int id)
        {
            return Query<dynamic>(@"
select		'Column' + cast(ColumnIndex as varchar) as datafield,
			Name as text
from		LoadColumn
where		LoadID = @id
order by	ColumnIndex", new { id });
        }

        public IEnumerable<dynamic> GetLoadItemDetails(int id)
        {
            var load = GetById<Load>(id);
            var useExecutionTable = false;

            if (load.Action == "P" && (load.PutExecutionID.HasValue || load.PostExecutionID.HasValue))
                useExecutionTable = true;

            var columns = Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var sql = "";
            var sqlColumns = "";
            var sqlTables = "";

            if (useExecutionTable)
            {
                AssetType assetType = Filter<AssetType>(a => a.uid == load.AssetTypeUid).FirstOrDefault();
                AssetType parentAssetType = assetType == null ? null : GetParentTypeById(assetType.ID);

                sqlColumns = $"select @id as LoadID, I.RowIndex as RowIndex\n";
                sqlTables = @"from (
		select ExecutionId, ItemNumber, ExecutionItemUid, ParentAssetID, Message, Success from api.ExecutionAsset where ExecutionId = {0}
		union all
		select ExecutionID, ItemNumber, ExecutionItemUid, null as ParentAssetID, Message, cast(0 as bit) as Success from api.ExecutionAssetError where ExecutionId = {0}
	 ) EA
     left join LoadItem I on I.LoadID = @id and I.ExecutionItemUid = EA.ExecutionItemUid";
                columns.ForEach(c =>
                {
                    var i = c.ColumnIndex;
                    if (parentAssetType != null && c.Name == parentAssetType.Name)
                    {
                        sqlColumns += $",EF{i}.DisplayValue + ' [' + cast(EF{i}.[uid] as varchar(50)) + ']' as Column{i}\n";
                        sqlTables += $" left join AssetDetail EF{i} on EF{i}.ID = EA.ParentAssetID\n";
                    }
                    else
                    {
                        sqlColumns += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                        sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";
                        sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";
                    }

                });
                sqlColumns += $", case EA.Success when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                sqlColumns += ", case when EA.Message is null and EA.Success = 1 then '{0}' else  EA.Message end as StatusMessage\n";

                sql = $"select * from ({string.Format(sqlColumns, "Item successfully updated.")} {string.Format(sqlTables, "@putExecutionID")} where EA.ExecutionID = @putExecutionID\n";
                sql += $"union all\n";
                sql += $"{string.Format(sqlColumns, "Item successfully added.")} {string.Format(sqlTables, "@postExecutionID")} where EA.ExecutionID = @postExecutionID) R order by R.RowIndex";

                return Query<dynamic>(sql, new { id, putExecutionID = load.PutExecutionID, postExecutionID = load.PostExecutionID });
            }
            else
            {
                sqlColumns = "select I.LoadID, I.RowIndex";
                sqlTables = "from LoadItem I";
                columns.ForEach(c =>
                {
                    sqlColumns += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                    sqlTables += string.Format(" left join LoadItemColumn C{0} on C{0}.LoadID = I.LoadID and C{0}.RowIndex = I.RowIndex and C{0}.ColumnIndex = {0}", c.ColumnIndex);
                });
                sqlColumns += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage";

                sql += sqlColumns + " " + sqlTables + " where I.LoadID = @id order by I.RowIndex";

                return Query<dynamic>(sql, new { id });
            }


        }

        public BulkLoadGetLoadColumnsModel GetLoadColumns(string action, SystemObjects type, int id, bool includeLookupValues)
        {
            return GetLoadColumns(action, type.ToString(), id, includeLookupValues);
        }

        public BulkLoadGetLoadColumnsModel GetLoadColumns(string action, string type, int id, bool includeLookupValues)
        {
            var jsonItems = Query<string>($"bulkload.GetLoadColumns @action, @type, @id, @getLookups", new { action, type = type, id, getLookups = includeLookupValues });
            var json = string.Concat(jsonItems);
            var model = JsonConvert.DeserializeObject<BulkLoadGetLoadColumnsModel>(json);

            return model;
        }

        #endregion Get Methods

        #region Parse Spreadsheet Methods

        public void BulkLoadParseFile(int loadID)
        {
            var load = GetById<Load>(loadID, i => i.LoadColumns);

            var loadItemRowCount = Query<int>("select count(1) from LoadItem where LoadID = @id", new { id = loadID }).Single();

            if (loadItemRowCount <= 0)
            {
                var memoryStream = new MemoryStream(load.File);
                var xls = new SLDocument(memoryStream);

                var stats = xls.GetWorksheetStatistics();

                var rowIndex = stats.StartRowIndex + 1;
                var numberOfColumns = load.LoadColumns.Count;

                var loadItems = new List<LoadItem>();
                var loadItemColumns = new List<LoadItemColumn>();

                while (rowIndex <= stats.EndRowIndex)
                {
                    // Empty row validation.
                    var numberOfEmptyColumns = 0;
                    foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
                    {
                        var testValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();
                        if (string.IsNullOrEmpty(testValue))
                            numberOfEmptyColumns++;
                    }

                    // Empty row check.
                    if (numberOfEmptyColumns < numberOfColumns)
                    {
                        var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex };
                        loadItems.Add(loadItem);

                        foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
                        {
                            var format = xls.GetCellStyle(rowIndex, c.ColumnIndex).FormatCode;
                            var isDate = false;

                            if (format.Contains("[$-404]") || format.Contains("m/d") || format.Contains("m-d") || format.Contains("d-m") ||
                                format.Contains("[$-F400]") || format.Contains("[$-409]"))
                                isDate = true;

                            var loadValue = string.Empty;

                            if (isDate)
                            {
                                loadValue = xls.GetCellValueAsDateTime(rowIndex, c.ColumnIndex).ToShortDateString();
                            }
                            else
                            {
                                loadValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();
                            }

                            loadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = loadValue });
                        }
                    }
                    rowIndex++;
                }

                var mustOpen = Database.Connection.State != ConnectionState.Open;
                try
                {
                    if (mustOpen)
                        Database.Connection.Open();

                    // Load Items processing
                    using (var bulkCopy = new SqlBulkCopy((Database.Connection) as SqlConnection))
                    {
                        bulkCopy.BatchSize = loadItems.Count;
                        bulkCopy.DestinationTableName = "dbo.LoadItem";
                        bulkCopy.BulkCopyTimeout = 3600;

                        var table = new DataTable();
                        var columnName = "LoadID";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "RowIndex";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        foreach (var item in loadItems)
                        {
                            var row = table.NewRow();

                            row["LoadID"] = item.LoadID;
                            row["RowIndex"] = item.RowIndex;

                            table.Rows.Add(row);
                        }

                        bulkCopy.WriteToServer(table);
                    }

                    // Load Item Columns Processing
                    using (var bulkCopy = new SqlBulkCopy((Database.Connection) as SqlConnection))
                    {
                        bulkCopy.BatchSize = loadItemColumns.Count;
                        bulkCopy.DestinationTableName = "dbo.LoadItemColumn";
                        bulkCopy.BulkCopyTimeout = 3600;

                        var table = new System.Data.DataTable();
                        var columnName = "LoadID";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "RowIndex";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "ColumnIndex";
                        table.Columns.Add(columnName, typeof(int));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        columnName = "Value";
                        table.Columns.Add(columnName, typeof(string));
                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                        foreach (var item in loadItemColumns)
                        {
                            var row = table.NewRow();

                            row["LoadID"] = item.LoadID;
                            row["RowIndex"] = item.RowIndex;
                            row["ColumnIndex"] = item.ColumnIndex;
                            if (string.IsNullOrEmpty(item.Value))
                                row["Value"] = DBNull.Value;
                            else
                                row["Value"] = item.Value;

                            table.Rows.Add(row);
                        }

                        bulkCopy.WriteToServer(table);
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (mustOpen)
                        Database.Connection.Close();
                }
            }
        }

        private bool findFieldObjectByValue(
            List<BulkLoadMatchingModel> matchingItems,
            List<LoadItemColumn> loadKeyFieldValues,
            int objectIDToFind,
            int groupIndex = 0
            )
        {
            var inList = false;
            var loadKeyValue = loadKeyFieldValues.Single(v => v.ColumnIndex == matchingItems[groupIndex].ColumnIndex);
            if (matchingItems[groupIndex].Fields.Any(f => f.ObjectID == objectIDToFind && f.Value == loadKeyValue.Value))
            {
                inList = true;

                if (groupIndex < matchingItems.Count - 1)
                {
                    //Recurse.
                    inList = findFieldObjectByValue(matchingItems, loadKeyFieldValues, objectIDToFind, groupIndex + 1);
                }
            }

            return inList;
        }

        #endregion Parse Spreadsheet Methods


        #region Process Data Methods

        private int getAssetIDFieldIndex(string objectType, string objectName, int objectId, List<LoadColumn> columns)
        {
            if (objectType == "FusionAttributeType")
            {
                //get the fusionattributetype name
                var fusionAttributeType = FusionAttributeTypes.Where(x => x.ID == objectId).FirstOrDefault();

                if (fusionAttributeType == null)
                    throw new Exception($"BULK LOAD INTERSECT CANNOT COMPLETE AS SUBJECT REFERENCES INVALID FUSION ATTRIBUTE TYPE ID {objectId}");

                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare(x.Name, fusionAttributeType.TextPath, true) == 0).First();
                var index = col.ColumnIndex;

                columns.Remove(col);

                return index;
            }
            else if (objectType == "IntersectType")
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName}", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET ID COLUMN : [{objectName}]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
            else if (objectType == "ReferenceItemType" && objectId == 0)
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName} Asset Type ID", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET ID COLUMN : [{objectName} Asset ID]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
            else
            {
                var col = columns.OrderBy(x => x.ColumnIndex).Where(x => string.Compare($"{objectName} Asset ID", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET ID COLUMN : [{objectName} Asset ID]");

                columns.Remove(col);

                return col.ColumnIndex;
            }
        }

        public async Task PerformBulkRelationshipOperation(int loadId, BulkRelationshipOperation operation)
        {
            // get load properties
            var load = Loads.Where(x => x.ID == loadId).FirstOrDefault();

            if (load == null)
            {
                throw new Exception($"Bulk load relate cannot find the load job to run [{loadId}].");
            }

            var intersectType = IntersectTypeDetails.Where(x => x.ID == load.ObjectID).FirstOrDefault();

            if (intersectType == null)
            {
                throw new Exception($"Bulk load relate cannot find the intersect type [{load.ObjectID}] specified by the load job [{loadId}]");
            }


            // get the load columns
            var columns = LoadColumns.Where(x => x.LoadID == loadId).ToList();

            if (columns == null)
            {
                throw new Exception($"Bulk load data doesnt contain any columns in LoadColumn table.  Load ID [{loadId}]");
            }

            var loaddata = LoadItemColumns.Where(x => x.LoadID == loadId);

            //loop throw rows until there are no more indexes start at 2
            int currentRowIndex = 2;

            var fieldColumns = columns.ToList();
            var subjectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Subject, intersectType.SubjectName, intersectType.SubjectID, fieldColumns);
            var objectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Object, intersectType.ObjectName, intersectType.ObjectID, fieldColumns);

            //load any custom field types for this relationship type
            var customFieldTypes = FieldTypes.Where(x => x.Object == "IntersectType" && x.ObjectID == intersectType.ID);
            Dictionary<int, int> customFieldTypeMap = new Dictionary<int, int>();

            if (operation == BulkRelationshipOperation.Relate && customFieldTypes.Any())
            {
                foreach (var item in customFieldTypes)
                {
                    var col = columns.Where(x => string.Compare(x.Name, item.Name, true) == 0).FirstOrDefault();

                    if (col != null)
                    {
                        customFieldTypeMap[item.ID] = col.ColumnIndex;
                    }
                }

                // call the proc to get lookup values for any custom values
                await Database.Connection.ExecuteAsync("exec[bulkload].[UpdateDynamicLookupFieldColumns] @loadId", new { loadId = loadId });
            }

            var rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();

            while (rowData != null && rowData.Count > 0)
            {
                BulkLoadStatusMsg = "";

                var subjectTypeName = (intersectType.Subject == "ReferenceItemType" && intersectType.SubjectID == 0) ? "ReferenceItemType" : intersectType.Subject.Replace("Type", "");
                var objectTypeName = (intersectType.Object == "ReferenceItemType" && intersectType.ObjectID == 0) ? "ReferenceItemType" : intersectType.Object.Replace("Type", "");

                int subjectId = getItemIdFromKeyFields(rowData, subjectAssetIDFieldIndex, subjectTypeName, intersectType.SubjectID);
                int objectId = getItemIdFromKeyFields(rowData, objectAssetIDFieldIndex, objectTypeName, intersectType.ObjectID);
                string errorMsg = string.Empty;
                int intersectId = 0;

                bool isValidCardinality = operation == BulkRelationshipOperation.Unrelate ? true : IsValidCardinality(intersectType, objectId, subjectId, objectTypeName, subjectTypeName, out errorMsg);

                if (isValidCardinality)
                {
                    intersectId = (operation == BulkRelationshipOperation.Relate) ?
                       RelateObjects(rowData, objectId, subjectId, objectTypeName, subjectTypeName, intersectType.ID, customFieldTypes, customFieldTypeMap) :
                       (UnrelateObjects(objectId, subjectId, objectTypeName, subjectTypeName, intersectType.ID));

                }
                else
                {
                    BulkLoadStatusMsg = errorMsg;
                }

                // update status for this item
                var statusSql = "update LoadItem set [Object] = 'Intersect', ObjectID = @objectId, Status = @status, StatusMessage = @msg where LoadID = @loadId and RowIndex = @rowIndex";

                await QueryAsync<int>(statusSql, new { objectId = intersectId, msg = BulkLoadStatusMsg, loadId = loadId, rowIndex = currentRowIndex, status = (intersectId > 0 ? 1 : 0) });

                //next row
                currentRowIndex++;

                rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();
            }
        }

        private bool IsValidCardinality(IntersectTypeDetail intersectType, int objectId, int subjectId, string objectType, string subjectType, out string message)
        {
            message = string.Empty;
            bool found = false;
            IQueryable<Intersect> intersects = Intersects.Where(x => x.IntersectTypeID == intersectType.ID);

            if (intersectType.SubjectCardinality == Cardinality.One)
            {
                found = intersects.Any(x => x.Object == objectType && x.ObjectID == objectId);
                message = found ? $"{objectType}  does not satisfy relationship cardinality " : string.Empty;

                if (found) return false;
            }

            if(intersectType.ObjectCardinality == Cardinality.One)
            {
                found = intersects.Any(x => x.Subject == subjectType && x.SubjectID == subjectId);
                message = found ? $" {subjectType}  does not satisfy relationship cardinality " : string.Empty;

                if (found) return false;
            }

            return true;
        }
        private int UnrelateObjects(int objectId, int subjectId, string objectType, string subjectType, int intersectTypeId)
        {
            var intersectId = 0;

            if (objectId > 0 && subjectId > 0)
            {

                var existingIntersect = Intersects.Where(x => x.Subject == subjectType && x.Object == objectType && x.IntersectTypeID == intersectTypeId && x.ObjectID == objectId && x.SubjectID == subjectId).FirstOrDefault();

                if (existingIntersect == null)
                {
                    BulkLoadStatusMsg = "Relationship doesnt exist.";
                }
                else
                {
                    try
                    {
                        intersectId = existingIntersect.ID;

                        DeleteRelationship(intersectId);

                        BulkLoadStatusMsg = "Relationship successfully removed.";
                    }
                    catch (core.exceptions.ConflictException ex)
                    {
                        intersectId = 0;

                        BulkLoadStatusMsg = $"Relationship could not be removed.  {ex.StatusDescription}";
                    }
                    catch (Exception ex)
                    {
                        intersectId = 0;

                        BulkLoadStatusMsg = $"Relationship could not be removed.  {ex.Message}";
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(BulkLoadStatusMsg))
                {
                    if (objectId <= 0) BulkLoadStatusMsg = "Cannot find the object object for the relationship";
                    else if (subjectId <= 0) BulkLoadStatusMsg = "Cannot find the subject object for the relationship";
                    else BulkLoadStatusMsg = "Unknown error"; // shouldnt happen
                }
            }

            return intersectId;
        }

        private int RelateObjects(List<LoadItemColumn> rowData, int objectId, int subjectId, string objectType, string subjectType, int intersectTypeId, IQueryable<FieldType> customFieldTypes, Dictionary<int, int> customFieldTypeMap)
        {
            var intersectId = 0;
            if (objectId > 0 && subjectId > 0)
            {
                if ( (objectId == subjectId) && (string.Compare(subjectType, objectType, StringComparison.OrdinalIgnoreCase) == 0) )
                {
                    BulkLoadStatusMsg = "Object cannot be related to itself";
                    return 0;
                }
                var existingIntersect = Intersects.Where(x => x.Subject == subjectType && x.Object == objectType && x.IntersectTypeID == intersectTypeId && x.ObjectID == objectId && x.SubjectID == subjectId).FirstOrDefault();

                if (existingIntersect == null)
                {
                    var newIntersect = new Intersect
                    {
                        IntersectTypeID = intersectTypeId,
                        Subject = subjectType,
                        SubjectID = subjectId,
                        Object = objectType,
                        ObjectID = objectId
                    };

                    Intersects.Add(newIntersect);

                    SaveChanges();

                    intersectId = newIntersect.ID;

                    BulkLoadStatusMsg = "Item successfully added.";
                }
                else
                {
                    intersectId = existingIntersect.ID;

                    BulkLoadStatusMsg = "Item successfully updated.";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(BulkLoadStatusMsg))
                {
                    if (objectId <= 0) BulkLoadStatusMsg = $"Cannot find the object object for the relationship";
                    else if (subjectId <= 0) BulkLoadStatusMsg = "Cannot find the subject object for the relationship";
                    else BulkLoadStatusMsg = "Unknown error"; // shouldnt happen
                }
            }

            //add any fields to the relationship here

            if (customFieldTypes.Any() && customFieldTypeMap.Any())
            {
                foreach (var ft in customFieldTypes)
                {
                    if (customFieldTypeMap.ContainsKey(ft.ID))
                    {
                        var val = rowData.Where(x => x.ColumnIndex == customFieldTypeMap[ft.ID]).FirstOrDefault();

                        if (val != null && !string.IsNullOrWhiteSpace(val.Value))
                        {
                            var existingField = Fields.Where(x => x.ObjectType == "Intersect" && x.ObjectID == intersectId && x.FieldTypeID == ft.ID).FirstOrDefault();
                            var value = val.Value;

                            if (ft.Type == "Lookup" && val.LookupObjectID.HasValue)
                                value = val.LookupObjectID.ToString();

                            if (existingField != null)
                            {
                                existingField.Value = value;
                            }
                            else
                            {
                                Fields.Add(new Field
                                {
                                    FieldTypeID = ft.ID,
                                    ObjectID = intersectId,
                                    ObjectType = "Intersect",
                                    Value = value
                                });
                            }
                        }
                    }
                }

                SaveChanges();
            }

            return intersectId;
        }
        
        private int getItemIdFromKeyFields(List<LoadItemColumn> rowData, int assetIdIndex, string @object, int objectTypeId)
        {
            var valItem = rowData.Where(x => x.ColumnIndex == assetIdIndex).FirstOrDefault();

            if (valItem == null) throw new Exception($"Cannot find relationship load data for the name field");

            if (@object == "FusionAttribute")
            {
                //load the fusion attribute where the fusionattribute type id matches the type in the intersecttype and the value 
                var fusionItem = FusionAttributes.Where(x => x.FusionAttributeTypeID == objectTypeId && string.Compare(x.TextPath, valItem.Value, true) == 0).FirstOrDefault();

                if (fusionItem == null) return -1;

                return fusionItem.ID;
            }
            else if (@object == "Intersect")
            {
                if (!int.TryParse(valItem.Value, out int intersectId))
                {
                    BulkLoadStatusMsg = $"Error intersect id is not a number {valItem.Value}";

                    return -1;
                }

                return intersectId;
            }
            else if (@object == "Intersect")
            {
                if (!int.TryParse(valItem.Value, out int intersectId))
                {
                    BulkLoadStatusMsg = $"Error intersect id is not a number {valItem.Value}";

                    return -1;
                }

                return intersectId;
            }
            else if (@object == "ReferenceItemType")
            {
                if (!int.TryParse(valItem.Value, out int assetTypeId))
                {
                    BulkLoadStatusMsg = $"Error asset type id is not a number {valItem.Value}";

                    return -1;
                }

                var assetType = AssetTypes.Where(x => x.ID == assetTypeId).FirstOrDefault();

                if (assetType == null)
                {
                    BulkLoadStatusMsg = $"Specified asset id doesnt exist in the asset table[{valItem.Value}]";

                    return -1;
                }

                return assetType.ObjectID;
            }
            else
            {
                if (!int.TryParse(valItem.Value, out int assetId))
                {
                    BulkLoadStatusMsg = $"Error asset id is not a number {valItem.Value}";

                    return -1;
                }

                var asset = Assets.Where(x => x.ID == assetId).Include(x => x.AssetType).FirstOrDefault();

                if (asset == null)
                {
                    BulkLoadStatusMsg = $"Specified asset id doesnt exist in the asset table[{valItem.Value}]";

                    return -1;
                }

                if (asset.AssetType == null || asset.AssetType.ObjectID != objectTypeId)
                {
                    BulkLoadStatusMsg = $"Specified asset id type doesnt match those required by the intersect type {asset.ObjectID}";

                    return -1;
                }

                return asset.ObjectID;
            }
        }

        #endregion

        #region Bulk Promote Methods

        public async Task BulkLoadAssets(Load load, IAssetRepository repository)
        {
            const int timeout = 3600;

            if (load == null)
                throw new ArgumentNullException("load cannot be null");

            if (!load.AssetTypeUid.HasValue)
                throw new ArgumentNullException("asset type uid cannot be null");

            try
            {
                var assetTypeUid = (Guid)load.AssetTypeUid;
                AssetType assetType = repository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    throw new Exception($"Asset type with uid {assetTypeUid} not found");


                var hasLookups = FieldTypes.Any(f => f.AssetTypeID == assetType.ID && f.LookupObjectID != null);
                
                await GenerateExecutionItemUids(load, timeout);

                //get parent type info if applicable
                var parentAssetType = GetParentType(assetType.ObjectID, SystemObjectHelper.GetSystemObjects(assetType.Class));
                int? intersectTypeId = null;
                PredicateType? predicateType = null;
                switch(assetType.Class)
                {
                    case AssetTypeClass.BusinessAsset:
                    case AssetTypeClass.TechnicalAsset:
                    case AssetTypeClass.FusionAttribute:
                    case AssetTypeClass.ReferenceItemType:
                        predicateType = PredicateType.InterTypeHierarchy;
                        break;
                    case AssetTypeClass.Policy:
                    case AssetTypeClass.Model:
                        predicateType = PredicateType.IntraTypeHierarchy;
                        break;
                }

                if (predicateType.HasValue)
                {
                    var intersectType = Filter<IntersectType>(o => o.Object == assetType.Object && o.ObjectID == assetType.ObjectID && o.Predicate.Type == predicateType).FirstOrDefault();
                    intersectTypeId = intersectType?.ID;
                }



                await Connection.OpenAsync();
                //calculate key hashes and resolve lookup values
                using (var trans = Connection.BeginTransaction())
                {
                    try
                    {
                        var executionID = Guid.NewGuid();

                        await Connection.ExecuteAsync(@"
                        drop table if exists #BulkExecutionAsset;
                        create table #BulkExecutionAsset (ExecutionID uniqueidentifier, ItemNumber int, ParentUid uniqueidentifier, ProposedKey varchar(32), AssetUid uniqueidentifier, AssetID bigint, Success bit, Message nvarchar(max))

                        drop table if exists #BulkExecutionField;
                        create table #BulkExecutionField (ExecutionID uniqueidentifier, ItemNumber int, FieldName nvarchar(250), FieldValue nvarchar(max), FieldTypeID int, LookupValue nvarchar(max), Ignore bit, ColumnIndex int);
                        ", transaction: trans);

                        //load temp tables and calculate key hashes
                        await Connection.ExecuteAsync(@"
                        insert into #BulkExecutionAsset
                        select	@executionID as ExecutionID,
		                        RowIndex as ItemNumber,
		                        ParentAssetUid as ParentUid,
		                        null as ProposedKey,
                                null as AssetUid,
                                null as AssetID,
                                null as [Success],
                                null as [Message]
                        from	[LoadItem] L
                        where	L.LoadID = @ID

                        insert into #BulkExecutionField
                        select	BA.ExecutionID,
		                        I.RowIndex as ItemNumber,
		                        FT.[Name] as FieldName,
		                        I.[Value] as FieldValue,
		                        FT.ID as FieldTypeID,
		                        null as LookupValue,
		                        null as Ignore,
                                I.ColumnIndex
                        from    [Load] L
                                inner join AssetType T on T.[Object] = L.[Object] and T.ObjectID = L.ObjectID
                                inner join LoadColumn LC on LC.LoadID = L.ID
                                inner join LoadItemColumn I on I.LoadID = L.ID and I.ColumnIndex = LC.ColumnIndex
		                        inner join #BulkExecutionAsset BA on BA.ItemNumber = I.RowIndex
                                inner join FieldType FT on FT.[Name] = LC.[Name] and FT.[Object] = T.[Object] and FT.ObjectID = T.ObjectID
                        where   L.ID = @ID;
                        "
                            , new { executionID, load.ID }, transaction: trans);

                        if (hasLookups)
                        {
                            ResolveFieldLookupValues(executionID, "#BulkExecutionField", timeout, trans);
                            await Connection.ExecuteAsync(@"
                            update  I
                            set     I.LookupValue = B.LookupValue
                            from    LoadItemColumn I
                                    inner join #BulkExecutionField B on B.ItemNumber = I.RowIndex and B.ColumnIndex = I.ColumnIndex
                            where   B.LookupValue is not null and I.LoadID = @ID
                            ", new { load.ID }, transaction: trans);
                        }

                        CalculateProposedKeyHashes(assetType, executionID, timeout, intersectTypeId, trans, "#BulkExecutionAsset", "#BulkExecutionField");

                        await Connection.ExecuteAsync(@"
                        update T
                        set T.AssetUid = K.Uid
                        from #BulkExecutionAsset T 
                        cross apply (
                        select		A.Uid,
			                        utility.GetHash(cast(@atID as nvarchar) + '|' + STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey 
                        from		Asset A 
			                        inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			                        left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
                        where	    A.AssetTypeID = @atID
                        group by    A.Uid
                        ) K 
                        where K.ActiveKey = T.ProposedKey

                        update L
                        set L.AssetUid = T.AssetUid
                        from LoadItem L
                        inner join #BulkExecutionAsset T on T.ItemNumber = L.RowIndex
                        where L.LoadID = @ID
                    ", new { atID = assetType.ID, load.ID }, transaction: trans);

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }

                var putAssets = new List<AssetUpdate>();
                var postAssets = new List<AssetInsert>();

                var loadItems = new List<LoadItem>();
                var loadColumns = Query<LoadColumn>("select * from LoadColumn LC where LoadID = @id", new { id = load.ID }).ToList();
                var loadItemColumns = Query<LoadItemColumn>("select * from LoadItemColumn where LoadID = @id", new { id = load.ID }).ToList();

                var assetTypeLevels = new Dictionary<int, string>();

                //build level info for models
                if (assetType.Class == AssetTypeClass.Model)
                {
                    for (var i = 1; i <= assetType.HierarchyMaximumDepth; i++)
                    {
                        var level = AssetTypeLevels.Where(l => l.AssetTypeID == assetType.ID).FirstOrDefault(l => l.Level == i);
                        if (level != null)
                            assetTypeLevels.Add(i, level.Name);
                        else
                            assetTypeLevels.Add(i, $"Level {i}");
                    }

                    loadItems = (await QueryAsync<LoadItem>(@"
                        select I.*, L.[Level] from LoadItem I
                        outer apply (
                            select      coalesce(max(L.[Level]), 1) as [Level]
                                from		AssetType ATT
			                                inner join AssetTypeLevel L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')
			                                inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
			                                inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = I.RowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
                                where		ATT.[ObjectID] = @ObjectID
                        ) L
                        where I.LoadID = @id ", new { id = load.ID, assetType.ObjectID }, timeout: timeout)).ToList();
                }
                else
                {
                    loadItems = Query<LoadItem>("select * from LoadItem where LoadID = @id", new { id = load.ID }).ToList();
                }

                //create API models
                foreach (var item in loadItems)
                {
                    var fieldsToSkip = new List<string>();
                    string assetTypeLevel = null;

                    var rowColumns = loadItemColumns.Where(l => l.RowIndex == item.RowIndex).ToList();

                    if (assetType.Class == AssetTypeClass.Model)
                    {
                        assetTypeLevel = assetTypeLevels[item.Level];

                        //ignore parent key fields, not needed for API
                        var keyFields = FieldTypes.Where(f => f.Object == assetType.Object && f.ObjectID == assetType.ObjectID && f.IsPartOfKey);
                        foreach (var k in keyFields)
                            fieldsToSkip.AddRange(assetTypeLevels.ToList().Where(l => l.Key != item.Level).Select(l => $"{l.Value} {k.Name}"));
                    }

                    if (!item.AssetUid.HasValue)
                    {
                        var insert = new AssetInsert();
                        insert.ExecutionItemUid = item.ExecutionItemUid;
                        insert.Fields = new Dictionary<string, string>();

                        //resolve model parent
                        if (assetType.Class == AssetTypeClass.Model && item.Level > 1)
                        {
                            var parentKeyHash = await GetModelKeyHashForLevel(item, assetType, item.Level - 1);

                            Guid? parentUid = (await QueryAsync<Guid?>(@"select [uid] from asset a
                                cross apply GetAssetKeyHashById(A.ID) S
                                where a.AssetTypeID = @assetTypeId and S.KeyHash = @parentKeyHash", new { parentKeyHash, assetTypeId = assetType.ID })).FirstOrDefault();

                            if (parentUid.HasValue)
                                insert.ParentUid = parentUid;
                        }
                      
                        foreach (var field in rowColumns)
                        {
                            var col = loadColumns.FirstOrDefault(c => c.ColumnIndex == field.ColumnIndex);

                            //resolve parent
                            if (parentAssetType != null && col.Name == parentAssetType.Name)
                            {
                                if (!string.IsNullOrWhiteSpace(field.Value))
                                {
                                    string parentUid = "";
                                    int endIndex = field.Value.LastIndexOf(']');
                                    int startIndex = field.Value.LastIndexOf('[') + 1;
                                    if (startIndex > -1 && endIndex > -1 && startIndex < endIndex)
                                    {
                                        parentUid = field.Value.Substring(startIndex, (endIndex - startIndex));
                                        insert.ParentUid = new Guid(parentUid);
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(field.Value) && !fieldsToSkip.Contains(col.Name))
                                {
                                    if (!string.IsNullOrEmpty(assetTypeLevel) && col.Name.StartsWith($"{assetTypeLevel} "))
                                        insert.Fields.Add(col.Name.Replace($"{assetTypeLevel} ", ""), field.Value);
                                    else
                                        insert.Fields.Add(col.Name, field.Value);
                                }
                            }
                        }
                        postAssets.Add(insert);
                    }
                    else
                    {
                        var update = new AssetUpdate();
                        update.ExecutionItemUid = item.ExecutionItemUid;

                        if (parentAssetType != null && item.ParentAssetUid.HasValue)
                                update.ParentUid = item.ParentAssetUid;

                        update.Uid = ((Guid)item.AssetUid);
                        update.Fields = new Dictionary<string, string>();

                        foreach (var field in rowColumns)
                        {
                            var col = loadColumns.FirstOrDefault(c => c.ColumnIndex == field.ColumnIndex); 

                            if (!fieldsToSkip.Contains(col.Name))
                            {
                                if (assetTypeLevel != null && col.Name.StartsWith($"{assetTypeLevel} "))
                                    update.Fields.Add(col.Name.Replace($"{assetTypeLevel} ", ""), field.Value);
                                else
                                    update.Fields.Add(col.Name, field.Value);
                            }
                        }
                        putAssets.Add(update);
                    }
                }

                if (putAssets.Any())
                {
                    var execution = getApiExecution(load, putAssets.Count);
                    ApiExecutionInfo executionInfo = await repository.PutBulkAssets(assetTypeUid, putAssets, execution, false);
                    load.PutExecutionID = executionInfo.ExecutionID;
                }

                if (postAssets.Any())
                {
                    var execution = getApiExecution(load, postAssets.Count);
                    ApiExecutionInfo executionInfo = await repository.PostBulkAssets(postAssets, execution, false);
                    load.PostExecutionID = executionInfo.ExecutionID;
                }

                await SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal ApiExecution getApiExecution(Load load, int total)
        {
            
            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = null,
                Method = null,
                ResourceID = load.UpdatedBy ?? 0,
                Total = total,
                Fields = load.AssetTypeUid.HasValue ? JsonConvert.SerializeObject(
                    new BulkLoadExecutionFields_Assets 
                    { 
                        AssetTypeUid = (Guid)load.AssetTypeUid, 
                        LoadID = load.ID 
                    }) : null,
                Error = 0,
                Processed = 0
            };

            return execution;
        }

        internal class BulkLoadExecutionFields_Assets
        {
            public Guid AssetTypeUid { get; set; }
            public int LoadID { get; set; }
        }

        private async Task GenerateExecutionItemUids(Load load, int timeout = 90)
        {
            await QueryAsync<int>(@"update LoadItem set ExecutionItemUid = newid() where LoadID = @id and ExecutionItemUid is null", new { id = load.ID }, timeout: timeout);
        }

        private async Task<string> GetModelKeyHashForLevel(LoadItem item, AssetType assetType, int level)
        {
            return (await QueryAsync<string>(@"select
       K.KeyHash
from    LoadItem T
        left join	(
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					        2) as KeyHash
	        from		(
					        select top 100 percent
						        IC.RowIndex, 
						        FT.ID as FieldTypeID, 
						        coalesce(cast(IC.LookupObjectID as varchar(100)), IC.[Value],'') as [Value] 
					        from LoadColumn LC
					        inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @rowIndex and IC.ColumnIndex = LC.ColumnIndex
					        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
					        where LC.LoadID = @id and LC.ColumnIndex in (
			 			        select		LC.ColumnIndex 
						        from		AssetType ATT
									        inner join AssetTypeLevel L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')																	
									        inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
									        inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @rowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
						        where		ATT.ObjectID = @ObjectID and L.[Level] = @currLevel
						        )
				        ) A
	        group by	A.RowIndex
        ) K on K.RowIndex = T.RowIndex
where	T.LoadID = @id and T.RowIndex = @rowIndex;", new { id = item.LoadID, rowIndex = item.RowIndex, currLevel = level, @object = new DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = assetType.Object }, objectID = assetType.ObjectID })).FirstOrDefault();
        }

        #endregion

        #endregion
    }
}
