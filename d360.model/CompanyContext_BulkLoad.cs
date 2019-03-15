using d360.core;
using d360.core.entities;
using d360.core.enums;
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

namespace d360.model
{
    partial class CompanyContext: BaseContext
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
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T";

        public IEnumerable<LoadDetail> GetLoadDetails()
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " order by L.ID desc");
        }

        public LoadDetail GetLoadDetail(int id)
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " where L.ID = " + id).SingleOrDefault();
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
            var columns = Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var sql = "";
            var sqlColumns = "select I.LoadID, I.RowIndex";
            var sqlTables = "from LoadItem I";
            columns.ForEach(c =>
            {
                sqlColumns += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                sqlTables += string.Format(" left join LoadItemColumn C{0} on C{0}.LoadID = I.LoadID and C{0}.RowIndex = I.RowIndex and C{0}.ColumnIndex = {0}", c.ColumnIndex);
            });
            sqlColumns += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage";
            sql += sqlColumns + " " + sqlTables + " where I.LoadID = @id order by I.RowIndex";
            return Query<dynamic>(sql, new { id });
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
            else if(objectType == "IntersectType")
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

            if(load == null)
            {
                throw new Exception($"Bulk load relate cannot find the load job to run [{loadId}].");
            }

            var intersectType = IntersectTypeDetails.Where(x => x.ID == load.ObjectID).FirstOrDefault();

            if(intersectType == null)
            {
                throw new Exception($"Bulk load relate cannot find the intersect type [{load.ObjectID}] specified by the load job [{loadId}]");
            }


            // get the load columns
            var columns = LoadColumns.Where(x => x.LoadID == loadId).ToList();

            if(columns == null)
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

            if(operation == BulkRelationshipOperation.Relate && customFieldTypes.Any())
            {
                foreach (var item in customFieldTypes)
                {
                    var col = columns.Where(x => string.Compare(x.Name, item.Name, true) == 0).FirstOrDefault();

                    if(col != null)
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

        private bool IsValidCardinality(IntersectTypeDetail intersectType, int objectId, int subjectId, string objectType, string subjectType,out string message)
        {
            message = string.Empty;
            bool found = false;


            if (intersectType.ObjectCardinality== Cardinality.One && intersectType.SubjectCardinality == Cardinality.One)
            {
                found = Intersects.Any((x => x.Object == objectType && x.IntersectTypeID == intersectType.ID && x.ObjectID == objectId) );
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
                    catch(core.exceptions.ConflictException ex)
                    {
                        intersectId = 0;

                        BulkLoadStatusMsg = $"Relationship could not be removed.  {ex.StatusDescription}";
                    }
                    catch(Exception ex)
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
                var fusionItem = FusionAttributes.Where(x => x.FusionAttributeTypeID == objectTypeId && string.Compare(x.TextPath,valItem.Value,true) == 0).FirstOrDefault();

                if (fusionItem == null) return -1;

                return fusionItem.ID;
            }
            else if(@object == "Intersect")
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

                var asset = Assets.Where(x => x.ID == assetId).Include(x=>x.AssetType).FirstOrDefault();

                if(asset == null)
                {
                    BulkLoadStatusMsg = $"Specified asset id doesnt exist in the asset table[{valItem.Value}]";

                    return -1;
                }

                if(asset.AssetType == null || asset.AssetType.ObjectID != objectTypeId)
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

        #endregion
    }
}
