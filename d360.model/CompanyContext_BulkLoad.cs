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
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

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
			else coalesce(C_D.[Name], 'Default') 
		end as ObjectName,
		L.Notes,
		'MyFile.' + L.Extension as FilePath,
		L.DateStarted,
		L.DateCompleted,
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
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PostExecutionID and Success = 1
				union all
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PutExecutionID and Success = 1 
				) R
			) S
		cross apply (
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PostExecutionID and Success = 0
				union all
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PutExecutionID and Success = 0 
				) R
			) E
		cross apply (
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PostExecutionID and Success is null
				union all
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PutExecutionID and Success is null 
				) R
			) I
		cross apply (
			select sum(I) as C from (
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PostExecutionID 
				union all
				select count(*) as I from api.ExecutionAsset where ExecutionID = L.PutExecutionID
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
                var assetType = Filter<AssetType>(a => a.uid == load.AssetTypeUid).FirstOrDefault();

                var parentAssetType = GetParentTypeById(assetType.ID);

                sqlColumns = $"select @id as LoadID, EA.ItemNumber as RowIndex\n";
                sqlTables = "from api.ExecutionAsset EA\n";
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
                        sqlColumns += $",EF{i}.FieldValue as Column{i}\n";
                        sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";
                    }

                });
                sqlColumns += $", case EA.Success when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], EA.Message as StatusMessage";

                sql = $"select * from ({sqlColumns} {sqlTables} where EA.ExecutionID = @putExecutionID union all {sqlColumns} {sqlTables} where EA.ExecutionID = @postExecutionID) R order by R.RowIndex";
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
                string errorMsg;
                int intersectId = 0;
                if (IsValidCardinality(intersectType, objectId, subjectId, objectTypeName, subjectTypeName, out errorMsg))
                {
                    intersectId = (operation == BulkRelationshipOperation.Relate) ?
                       RelateObjects(rowData, objectId, subjectId, objectTypeName, subjectTypeName, intersectType.ID, customFieldTypes, customFieldTypeMap) :
                       (await UnrelateObjects(objectId, subjectId, objectTypeName, subjectTypeName, intersectType.ID));

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


            if (intersectType.ObjectCardinality == Cardinality.One && intersectType.SubjectCardinality == Cardinality.One)
            {
                found = Intersects.Any((x => x.Object == objectType && x.IntersectTypeID == intersectType.ID && x.ObjectID == objectId));
                message = found ? $"{objectType}  does not satisfy relationship cardinality " : string.Empty;

                if (found) return false;

                found = Intersects.Any(x => x.Subject == subjectType && x.IntersectTypeID == intersectType.ID && x.SubjectID == subjectId);
                message = found ? $" {subjectType}  does not satisfy relationship cardinality " : string.Empty;

                if (found) return false;

            }

            return true;
        }
        private async Task<int> UnrelateObjects(int objectId, int subjectId, string objectType, string subjectType, int intersectTypeId)
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

        private void mapKeyFields(List<FieldType> subjectKeyFields, Dictionary<int, int> columnToFieldTypeIdMap, string objectName, List<LoadColumn> columns)
        {
            foreach (var field in subjectKeyFields)
            {
                var col = columns.Where(x => string.Compare(x.Name, $"{objectName} {field.Name}", true) == 0).First();

                columnToFieldTypeIdMap[field.ID] = col.ColumnIndex;
            }
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

        private void validateKeyFields(List<FieldType> keyFieldType, List<LoadColumn> columns, string objectTypeName)
        {
            foreach (var field in keyFieldType)
            {
                //find the field in the input load columns
                if (!columns.Any(x => string.Compare(x.Name, $"{objectTypeName} {field.Name}", true) == 0))
                    throw new Exception($"Bulk Relate cannot find key field {field.Name} id {field.ID} friendly name {field.FriendlyName}");
            }
        }

        #endregion

        #region Bulk Promote Methods

        public async Task BulkLoadAssets(Load load, IAssetRepository repository)
        {

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

                //get parent type if applicable
                var parentAssetType = GetParentType(assetType.ObjectID, SystemObjectHelper.GetSystemObjects(assetType.Class));

                //need to calculate key hashes to figure out which assets to put and post
                await LoadLookupValues(load, assetType);
                await CalculateHashes(load, assetType);

                var putAssets = new List<AssetUpdate>();
                var postAssets = new List<AssetInsert>();

                var loadItems = Filter<LoadItem>(l => l.LoadID == load.ID).ToList();

                foreach (var item in loadItems)
                {
                    var loadItemColumns = Filter<LoadItemColumn>(l => l.LoadID == load.ID && l.RowIndex == item.RowIndex).ToList();
                    if (!item.ObjectID.HasValue)
                    {
                        var insert = new AssetInsert();
                        insert.Fields = new Dictionary<string, string>();

                        foreach (var field in loadItemColumns)
                        {
                            
                            var col = load.LoadColumns.Where(c => c.LoadID == load.ID && c.ColumnIndex == field.ColumnIndex).FirstOrDefault();
                            
                            if (parentAssetType != null && col.Name == parentAssetType.Name)
                            {
                                    string parentUid = "";
                                    int endIndex = field.Value.LastIndexOf(']');
                                    int startIndex = field.Value.LastIndexOf('[') + 1;
                                    if (startIndex < endIndex)
                                        parentUid = field.Value.Substring(startIndex, (endIndex - startIndex));
                                    insert.ParentUid = new Guid(parentUid);
                            }
                            else
                            {
                                insert.Fields.Add(col.Name, field.Value);
                            }
                        }
                        postAssets.Add(insert);
                    }
                    else
                    {
                        var update = new AssetUpdate();
                        var asset = Query<Asset>("select * from Asset Where Object = @object and ObjectID = @objectID", new { @object = item.Object, objectID = item.ObjectID }).FirstOrDefault();
                        AssetDetail parent = null;
                        if (parentAssetType != null)
                        {
                            if (Enum.TryParse(asset.Object, out SystemObjects obj))
                            {
                                parent = GetParentObject(asset.ObjectID, obj);
                            }
                            else
                            {
                                item.StatusMessage = $"Could not parse system object {parent.Object}";
                            }

                            if (parent != null)
                                update.ParentUid = parent.uid;
                        }

                        update.Uid = asset.uid;
                        update.Fields = new Dictionary<string, string>();

                        foreach (var field in loadItemColumns)
                        {
                            var col = load.LoadColumns.Where(c => c.LoadID == load.ID && c.ColumnIndex == field.ColumnIndex).FirstOrDefault();
                            update.Fields.Add(col.Name, field.Value);
                        }
                        putAssets.Add(update);
                    }
                }


                if (putAssets.Any())
                {
                    var execution = getApiExecution(putAssets.Count, new BulkLoadExecutionFields_Assets { AssetTypeUid = assetTypeUid, LoadID = load.ID });
                    ApiExecutionInfo executionInfo = await repository.PutBulkAssets(assetTypeUid, putAssets, execution);
                    load.PutExecutionID = executionInfo.ExecutionID;
                }

                if (postAssets.Any())
                {
                    var execution = getApiExecution(postAssets.Count, new BulkLoadExecutionFields_Assets { AssetTypeUid = assetTypeUid, LoadID = load.ID });
                    ApiExecutionInfo executionInfo = await repository.PostBulkAssets(postAssets, execution);
                    load.PostExecutionID = executionInfo.ExecutionID;
                }

                SaveChanges();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal ApiExecution getApiExecution(int total = 0, object fields = null, int error = 0, int processed = 0)
        {

            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = null,
                Method = null,
                ResourceID = CurrentResourceID,
                Total = total,
                Fields = fields == null ? "" : JsonConvert.SerializeObject(fields),
                Error = error,
                Processed = processed
            };

            return execution;
        }

        internal class BulkLoadExecutionFields_Assets
        {
            public Guid AssetTypeUid { get; set; }
            public int LoadID { get; set; }
        }


        private async Task LoadLookupValues(Load load, AssetType assetType)
        {
            var resolveLookupSql = @"
begin

	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when ((L_A.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType')) ) then 'Artifact'									
									else NULL
								end as LookupObject,
								case 
									when L_A.ObjectID is not null then L_A.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex								
								inner join AssetDetail L_A on L_A.[Object] = 'Artifact' and L_A.TypeID = F.LookupObjectID and (L_A.DisplayValue = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Artifact', 'ArtifactType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_D.ObjectID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType') ) then 'ReferenceItemType'									
									else NULL
								end as LookupObject,
								case 									
									when L_D.ObjectID is not null then L_D.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0																		
									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join AssetType L_D on L_D.[Object] = 'ReferenceItemType' and L_D.[Name] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItemType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex


if exists (select 1 from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name								
				where F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem')
	begin
		
		update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,								
										case
											
											when ( (FLV.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'									
											else NULL
										end as LookupObject,
										case 									
											
											when FLV.Value is not null then FLV.Value
											when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
											else NULL
										end as LookupObjectID 
								from	FieldType F
										inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
										inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										
										cross apply [dbo].[FieldLookupValueByFieldTypeID](F.ID) FLV
										
								where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem' and  FLV.Text = IC.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
	end

	update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case
										when ( (L_F.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType') ) then 'FusionAttribute'									
										else NULL
									end as LookupObject,
									case 									
										when L_F.ID is not null then L_F.ID
										when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

										else NULL
									end as LookupObjectID 
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

									inner join FusionAttribute L_F on L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)								
							where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'FusionAttributeType'
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case 									
										when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup') ) then 'Lookup'									
										else NULL
									end as LookupObject,
									case 									
										when L_L.Value is not null then L_L.Value
										when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

										else NULL
									end as LookupObjectID 
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

									left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
							where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Lookup'
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case 									
										when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Resource') ) then 'Resource'									
										else NULL
									end as LookupObject,
									case 									
										when L_L.Value is not null then L_L.Value
										when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

										else NULL
									end as LookupObjectID 
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

									left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Resource' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
							where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Resource'
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case
										when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
										else NULL
									end as LookupObject,
									case 									
										when L_T.ObjectID is not null then L_T.ObjectID
										when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
										else NULL
									end as LookupObjectID 
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

									inner join AssetDetail L_T on L_T.[Object] = 'Taxonomy' and L_T.TypeID = F.LookupObjectID and (L_T.[DisplayValue] = IC.Value )
							where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Taxonomy', 'TaxonomyType')
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex


	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ObjectID is not null then L_T.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex

								inner join AssetType L_T on L_T.[Object] = 'TaxonomyType'  and (L_T.Name = IC.Value )
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'TaxonomyType' and F.LookupObjectID = 0
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
	if exists (select 1 from LoadItem LI
						inner join LoadColumn C on C.LoadID = LI.LoadID
						inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where
					FT.AllowMultipleValues = 1 and LI.LoadID = @id )
	begin
		

		update	IC
		set		IC.LookupObject = MV.LookupObject,
				IC.LookupValue = MV.LookupValue
		from	LoadItemColumn IC
				inner join	(
							select		IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										'ReferenceItem' as LookupObject,
										string_agg(AD.ObjectID, ',') as LookupValue
							from		LoadItem LI
										inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
										inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
										inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
										cross apply string_split(IC.Value, ',') VS									
										left join AssetWithType AD on AD.Object = 'ReferenceItem' and AD.TypeID = FT.LookupObjectID
										CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ObjectID, FT.ID) GRIDV
							where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
							group by	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex			
							) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex
	end

	update	IC
		set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
				IC.LookupObjectID = 0,
				IC.LookupValue = 0
		from	LoadItemColumn IC
				inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
				inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;
	
end

";

            await QueryAsync<int>(resolveLookupSql, new { id = load.ID, @object = assetType.Object, objectId = assetType.ObjectID });

        }

        private async Task CalculateHashes(Load load, AssetType assetType)
        {
            var parentAssetType = GetParentTypeById(assetType.ID);

            #region Hash Sql

            var glossaryHashSql = @"
update  L
set     L.KeyHash = K.KeyHash,
		L.FieldHash = F.FieldHash
from    LoadItem L
        inner join (
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
					        2) as KeyHash
	        from		(
				        select		top 1000000000 
							        I.RowIndex,
							        FT.ID as FieldTypeID,
							        coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
				        from		LoadItem I
							        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
							        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
							        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
				        order by	I.RowIndex,
							        FT.ID
				        ) A
	        group by	A.RowIndex
        ) K on K.RowIndex = L.RowIndex
        inner join (
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					        2) as FieldHash
	        from		(
				        select		top 100 percent
							        I.RowIndex,
							        FT.ID as FieldTypeID,
							        coalesce(IC.Value, '') as Value
				        from		LoadItem I
							        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
							        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
							        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				        order by	I.RowIndex,
							        FT.ID
				        ) A
	        group by	A.RowIndex	
        ) F on F.RowIndex = L.RowIndex
where	L.LoadID = @id
";
            var glossaryHashWithParentSql = @"
update  L
set     L.KeyHash = K.KeyHash,
		L.FieldHash = F.FieldHash
from    LoadItem L
        inner join	(
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' +Value, char(59))), 3, 32), 
					        2) as KeyHash
	        from		(
								
				        select		top 1000000000 
							        I.RowIndex,
							        FT.ID as FieldTypeID,
							        coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
				        from		LoadItem I
							        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
							        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
							        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
													
				        union
				        select
					        top 1000000000
					        I.RowIndex,
					        -1 as FieldTypeID,
					        coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
				        from
					        LoadItem I
					        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
					        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
					        inner join ASSETTYPE ATT on ATT.Object = @Object and ATT.ObjectID = @parentTypeID 
					        inner join ASSET A on A.AssetTypeID = ATT.ID
					        inner join AssetDisplayValue AD on A.ID = AD.AssetID and AD.DisplayValue = IC.Value
				        order by	RowIndex,
							        FieldTypeID
				        ) A								
	        group by	A.RowIndex
        ) K on K.RowIndex = L.RowIndex
        inner join	(
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					        2) as FieldHash
	        from		(
				        select		top 100 percent
							        I.RowIndex,
							        FT.ID as FieldTypeID,
							        coalesce(IC.Value, '') as Value
				        from		LoadItem I
							        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
							        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
							        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				        order by	I.RowIndex,
							        FT.ID
				        ) A
	        group by	A.RowIndex	
        ) F on F.RowIndex = L.RowIndex
where	L.LoadID = @id
";
            var modelHashSql = @"
update  T
set     T.KeyHash = K.KeyHash,
		T.FieldHash = F.FieldHash
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
        inner join	(
	        select		RowIndex,
				        CONVERT(
					        varchar(32), 
					        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					        2) as FieldHash
	        from		(
				        select		top 100 percent
							        I.RowIndex,
							        FT.ID as FieldTypeID,
							        coalesce(IC.Value, '') as Value
				        from		LoadItem I
							        inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
							        inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
							        inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				        order by	I.RowIndex,
							        FT.ID
				        ) A
	        group by	A.RowIndex	
        ) F on F.RowIndex = T.RowIndex
where	T.LoadID = @id and T.RowIndex = @rowIndex;
";
            var referenceItemHashSql = @"
update	T
set		T.KeyHash = CONVERT(
							varchar(32), 
							SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
							2),
		T.FieldHash = V.FieldHash
from	LoadItem T
		inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
		inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
		inner join	(
					select		RowIndex,
								CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
									2) as FieldHash
					from		(
								select		top 100 percent
											I.RowIndex,
											FT.ID as FieldTypeID,
											coalesce(cast(IC.LookupObjectID as varchar(100)), IC.Value, '') as Value
								from		LoadItem I
											inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
											inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
											left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
											left join Asset RI on RI.Object = 'ReferenceItem' and C.Name = 'Code' and RI.ObjectID = @ObjectID
								order by	I.RowIndex,
											FT.ID
								) A
					group by	A.RowIndex	
					) V on V.RowIndex = T.RowIndex
where	T.LoadID = @id;
";

            #endregion

            if (assetType.Class == AssetTypeClass.Glossary)
            {
                if (parentAssetType != null)
                {
                    await QueryAsync<int>(glossaryHashWithParentSql, new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID, parentTypeID = parentAssetType.ObjectID });
                }
                else
                {
                    await QueryAsync<int>(glossaryHashSql, new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID });
                }
            }
            else if (assetType.Class == AssetTypeClass.Model)
            {
                foreach (var item in load.LoadItems)
                {

                    var currLevel = (await QueryAsync<int>(@"
                        select      coalesce(max(L.[Level]), 1) 
                        from		AssetType ATT
			                        inner join AssetTypeLevel L on (L.AssetTypeID = ATT.ID and ATT.[Object] = 'TaxonomyType')
			                        inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
			                        inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @rowIndex and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
                        where		ATT.[ObjectID] = @ObjectID"
                        , new { id = load.ID, rowIndex = item.RowIndex, objectID = assetType.ObjectID })).FirstOrDefault();

                    await QueryAsync<int>(modelHashSql, new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID, rowIndex = item.RowIndex, currLevel });

                }
            }
            else if (assetType.Class == AssetTypeClass.Reference)
            {
                await QueryAsync<int>(referenceItemHashSql, new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID });
            }

            //find object/objectid for existing items
            if (assetType.Class == AssetTypeClass.Glossary && parentAssetType != null)
            {
                await QueryAsync<int>(@"
        update	T
        set     T.Object = A.Object,
                T.ObjectID = A.ObjectID
        from LoadItem T
                inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
                inner join Asset A on A.AssetTypeID = ST.ID
                cross apply[GetArtifactKeyHashByIdWithParent](A.ID) S
        where S.KeyHash = T.KeyHash and T.LoadID = @id", new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID });

            }
            else
            {
                await QueryAsync<int>(@"
        update	T
        set     T.Object = A.Object,
                T.ObjectID = A.ObjectID
        from LoadItem T
                inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
                inner join Asset A on A.AssetTypeID = ST.ID
                cross apply GetAssetKeyHashById(A.ID) S
        where S.KeyHash = T.KeyHash and T.LoadID = @id",
        new { @object = assetType.Object, objectID = assetType.ObjectID, id = load.ID });

            }
        }

        #endregion

        #endregion
    }
}
