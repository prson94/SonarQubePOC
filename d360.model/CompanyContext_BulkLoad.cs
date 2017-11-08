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
		coalesce(C_D.Name, 'Default') as ObjectName,
		L.Notes,
		'MyFile.' + L.Extension as FilePath,
		L.DateStarted,
		L.DateCompleted,
		case L.[Action]
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
		left join Cache.ObjectDetails C_D on C_D.[Object] = L.[Object] and C_D.ObjectID = L.ObjectID 		
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

                var numberOfRows = stats.NumberOfRows;
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
                catch (Exception ex)
                {

                }
                finally
                {
                    if (mustOpen)
                        Database.Connection.Close();
                }
            }
        }

        public void BulkLoadPromoteAction(int loadID)
        {
            var load = GetById<Load>(loadID//, 
                //i => i.LoadColumns, i => i.LoadItems, i => i.LoadItemColumns
                );

            var loadColumns = Filter<LoadColumn>(i => i.LoadID == loadID).ToList();
            var loadItems = Filter<LoadItem>(i => i.LoadID == loadID).ToList();
            var loadItemColumns = Filter<LoadItemColumn>(i => i.LoadID == loadID).ToList();

            if (load != null)
            {
                if (load.Action == "P")
                {
                    var fieldTypes = Filter<FieldType>(i => i.Object == load.Object && i.ObjectID == load.ObjectID).ToList();
                    var matchingItems = new List<BulkLoadMatchingModel>(); //This will be used when identifying existing assets based on key fields.

                    IntersectType intersectType = null; //NOTE: This accounts for only one relationship field in a type. There may be more than one.

                    if (load.Object == SystemObjects.ReferenceItemType.ToString())
                    {
                        fieldTypes.Add(new FieldType { ID = 0, Name = "Code", FriendlyName = "Code", IsPartOfKey = true });
                    }

                    //Loop through each field type and resolve to an underlying lookup reference, if required.
                    fieldTypes.ForEach(ft => {

                        var matchingLoadColumn = loadColumns.SingleOrDefault(lc => lc.Name == ft.Name);

                        #region Lookup field resolution

                        if (ft.Type == DataType.Lookup.ToString())
                        {
                            if (matchingLoadColumn != null)
                            {
                                var uniqueLoadItemValues = load.LoadItemColumns.Where(lic => lic.ColumnIndex == matchingLoadColumn.ColumnIndex).Select(lic => lic.Value).Distinct();
                                var resolvedLookupValues = Filter<FieldLookupValue>(o =>
                                    o.LookupObjectType == ft.LookupObjectType.Replace("Type", "") &&
                                    o.LookupObjectID == ft.LookupObjectID &&
                                    uniqueLoadItemValues.Contains(o.Text)
                                    ).ToList();

                                foreach (var lic in loadItemColumns.Where(o => o.ColumnIndex == matchingLoadColumn.ColumnIndex))
                                {
                                    if (ft.AllowAllValue && ft.AllowAllLabel.ToLower() == lic.Value.ToLower())
                                    {
                                        lic.LookupObject = ft.LookupObjectType.Replace("Type", "");
                                        lic.LookupObjectID = 0;
                                    }
                                    else
                                    {
                                        var resolvedLookupValue = resolvedLookupValues.FirstOrDefault(i => i.Text == lic.Value);
                                        if (resolvedLookupValue != null)
                                        {
                                            lic.LookupObject = resolvedLookupValue.LookupObjectType;
                                            lic.LookupObjectID = resolvedLookupValue.Value;
                                        }
                                    }
                                }

                                ChangeTracker.DetectChanges();
                                SaveChanges();
                            }
                        }

                        #endregion

                        #region Relation field resolution

                        if (ft.Type == DataType.Relationship.ToString())
                        {
                            if (matchingLoadColumn != null)
                            {
                                intersectType = GetById<IntersectType>(ft.LookupObjectID.Value);
                                if (intersectType != null)
                                {
                                    var uniqueLoadItemValues = load.LoadItemColumns.Where(lic => lic.ColumnIndex == matchingLoadColumn.ColumnIndex).Select(lic => lic.Value).Distinct();

                                    var isSubjectSide = (intersectType.Subject == load.Object && intersectType.SubjectID == load.ObjectID);
                                    var objToGet = isSubjectSide ? intersectType.Object : intersectType.Subject;
                                    var objIDToGet = isSubjectSide ? intersectType.ObjectID : intersectType.SubjectID;

                                    List<BulkLoadRelationModel> resolvedRelatableObjects = null;
                                    switch (objToGet)
                                    {
                                        case "ArtifactType":
                                            resolvedRelatableObjects = Filter<Artifact>(o =>
                                                o.ArtifactTypeID == objIDToGet &&
                                                uniqueLoadItemValues.Contains(o.DisplayValue)
                                                )
                                                .Select(o => new BulkLoadRelationModel
                                                {
                                                    DisplayValue = o.DisplayValue,
                                                    Object = "Artifact",
                                                    ObjectID = o.ID
                                                })
                                                .ToList();
                                            break;
                                    }

                                    foreach (var lic in loadItemColumns.Where(o => o.ColumnIndex == matchingLoadColumn.ColumnIndex))
                                    {
                                        var resolvedLookupValue = resolvedRelatableObjects.FirstOrDefault(i => i.DisplayValue == lic.Value);
                                        if (resolvedLookupValue != null)
                                        {
                                            lic.LookupObject = resolvedLookupValue.Object;
                                            lic.LookupObjectID = resolvedLookupValue.ObjectID;
                                        }
                                    }

                                    ChangeTracker.DetectChanges();
                                    SaveChanges();
                                }
                            }
                        }

                        #endregion

                        if (ft.IsPartOfKey && matchingLoadColumn != null)
                        {
                            matchingItems.Add(new BulkLoadMatchingModel { FieldTypeID = ft.ID, ColumnIndex = matchingLoadColumn.ColumnIndex });
                        }

                    });

                    //Load matching field groups based on values that are contained in the spreadsheet for that column, and no others.
                    foreach (var matchingItem in matchingItems)
                    {
                        var uniqueLoadItemValues = load.LoadItemColumns.Where(lic => lic.ColumnIndex == matchingItem.ColumnIndex).Select(lic => lic.LookupObjectID.HasValue ? lic.LookupObjectID.ToString() : lic.Value).Distinct();
                        matchingItem.Fields = Filter<Field>(f => 
                            f.FieldTypeID == matchingItem.FieldTypeID && 
                            uniqueLoadItemValues.Contains(f.Value)
                        )
                        .Select(o => new BulkLoadMatchingFieldModel {
                            ObjectID = o.ObjectID,
                            Value = o.Value
                        }).ToList();
                    }

                    //Now filter out those items that do not have matching ObjectIDs across all groups.
                    var objectIDs = (
                        from mi in matchingItems
                        from f in mi.Fields
                        select f.ObjectID
                    ).Distinct().ToList();

                    //matchingItems.ForEach(mi =>
                    //{
                    //    objectIDs.AddRange(mi.Fields.Select(f => f.ObjectID).Except(objectIDs));
                    //});

                    var objectIDsToKeep = new List<int>();
                    objectIDs.ForEach(objectID => {
                        var keep = true;

                        foreach (var mi in matchingItems)
                        {
                            if (!mi.Fields.Any(f => f.ObjectID == objectID))
                            {
                                keep = false;
                                break;
                            }
                        }

                        if (keep)
                            objectIDsToKeep.Add(objectID);
                    });

                    foreach (var mi in matchingItems)
                    {
                        mi.Fields.RemoveAll(f => !objectIDsToKeep.Contains(f.ObjectID));
                    }

                    var loadKeyColumnIndexes = matchingItems.Select(i => i.ColumnIndex).ToList();
                    foreach (var li in loadItems)
                    {
                        var loadKeyFieldValues = loadItemColumns.Where(i => 
                            i.RowIndex == li.RowIndex && 
                            loadKeyColumnIndexes.Contains(i.ColumnIndex)
                        ).ToList();

                        objectIDsToKeep.ForEach(oid =>
                        {
                            if (findFieldObjectByValue(matchingItems, loadKeyFieldValues, oid))
                            {
                                //This is the ID to use.
                                li.Object = load.Object.Replace("Type", "");
                                li.ObjectID = oid;
                            }
                        });
                    }

                    //Save the changes to the LoadItems, whether we were able to resolve any to existing assets.
                    ChangeTracker.DetectChanges();
                    SaveChanges();

                    //Compile list of field types that are computed and that we do not store an actual value for.
                    var fieldTypesToIgnore = new List<string> {
                        DataType.Attribute.ToString(),
                        DataType.ComplexRelationLookup.ToString(),
                        DataType.DataTableSelect.ToString(),
                        DataType.FieldFromRelationship.ToString(),
                        DataType.File.ToString(),
                        DataType.FilteredLookup.ToString(),
                        DataType.Hidden.ToString(),
                        DataType.OwnershipLookup.ToString(),
                        DataType.RefListRelationship.ToString()//,
                        //DataType.Relationship.ToString()
                    };

                    //These field types we will load data for.
                    var loadableFieldTypes = fieldTypes.Where(ft => !fieldTypesToIgnore.Contains(ft.Type)).ToList();

                    intersectType = null; //NOTE: This accounts for only one relationship field in a type. There may be more than one.

                    foreach (var li in loadItems)
                    {
                        var loadKeyFieldValues = loadItemColumns.Where(i => i.RowIndex == li.RowIndex).ToList();
                        if (string.IsNullOrEmpty(li.Object) && !li.ObjectID.HasValue)
                        {
                            #region NEW object creation

                            switch (load.Object)
                            {
                                case "ArtifactType":
                                    #region
                                    var newArtifact = new Artifact { ArtifactTypeID = load.ObjectID, Visible = true };
                                    Artifacts.Add(newArtifact);
                                    SaveChanges();
                                    li.Object = "Artifact";
                                    li.ObjectID = newArtifact.ID;
                                    break;
                                    #endregion
                                case "PolicyType":
                                    #region
                                    var newPolicy = new Policy { PolicyTypeID = load.ObjectID, Visible = true };
                                    Policies.Add(newPolicy);
                                    SaveChanges();
                                    li.Object = "Policy";
                                    li.ObjectID = newPolicy.ID;
                                    break;
                                #endregion
                                case "ReferenceItemType":
                                    #region
                                    //Code should always be the first column in this spreadsheet. NOTE: Is this the best way to do this?
                                    var newReferenceItem = new core.entities.ReferenceItem { ReferenceItemTypeID = load.ObjectID, Code = loadKeyFieldValues.First(c => c.ColumnIndex == 1).Value };
                                    ReferenceItems.Add(newReferenceItem);
                                    SaveChanges();
                                    li.Object = "ReferenceItem";
                                    li.ObjectID = newReferenceItem.ID;
                                    break;
                                #endregion
                                case "RuleType":
                                    #region
                                    var newRule = new core.entities.Rule { RuleTypeID = load.ObjectID, Visible = true };
                                    Rules.Add(newRule);
                                    SaveChanges();
                                    li.Object = "Rule";
                                    li.ObjectID = newRule.ID;
                                    break;
                                    #endregion
                                case "TaxonomyType":
                                    #region
                                    var newTaxonomy = new Taxonomy { TaxonomyTypeID = load.ObjectID, Visible = true };
                                    Taxonomies.Add(newTaxonomy);
                                    SaveChanges();
                                    li.Object = "Taxonomy";
                                    li.ObjectID = newTaxonomy.ID;
                                    break;
                                    #endregion
                            }

                            #endregion

                            loadableFieldTypes.ForEach(ft =>
                            {
                                if (ft.ID > 0) //We should only perform this on dynamic fields (not static fields where the field type ID is set to 0).
                                {
                                    var matchingLoadColumn = loadColumns.SingleOrDefault(lc => lc.Name == ft.Name);
                                    if (matchingLoadColumn != null)
                                    {
                                        var loadKeyFieldValue = loadKeyFieldValues.Single(v => v.ColumnIndex == matchingLoadColumn.ColumnIndex);

                                        if (ft.Type == DataType.Relationship.ToString())
                                        {
                                            // Create a relationship.
                                            if (!loadKeyFieldValue.LookupObjectID.HasValue)
                                            {
                                                if (intersectType == null)
                                                    intersectType = GetById<IntersectType>(ft.LookupObjectID.Value);

                                                if (intersectType != null)
                                                {
                                                    var intersect = new Intersect { IntersectTypeID = ft.LookupObjectID.Value };

                                                    try
                                                    {
                                                        var isSubjectSide = (intersectType.Subject == load.Object && intersectType.SubjectID == load.ObjectID);
                                                        if (isSubjectSide)
                                                        {
                                                            intersect.Subject = li.Object;
                                                            intersect.SubjectID = li.ObjectID.Value;
                                                            intersect.Object = loadKeyFieldValue.LookupObject;
                                                            intersect.ObjectID = loadKeyFieldValue.LookupObjectID.Value;
                                                        }
                                                        else
                                                        {
                                                            intersect.Subject = loadKeyFieldValue.LookupObject;
                                                            intersect.SubjectID = loadKeyFieldValue.LookupObjectID.Value;
                                                            intersect.Object = li.Object;
                                                            intersect.ObjectID = li.ObjectID.Value;
                                                        }
                                                        Intersects.Add(intersect);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        li.Status = false;
                                                        li.StatusMessage += ex.GetFullExceptionData();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Create the field.
                                            if (!string.IsNullOrEmpty(loadKeyFieldValue.Value))
                                            {
                                                Fields.Add(new Field
                                                {
                                                    FieldTypeID = ft.ID,
                                                    ObjectType = li.Object,
                                                    ObjectID = li.ObjectID.Value,
                                                    Value = loadKeyFieldValue.LookupObjectID.HasValue ? loadKeyFieldValue.LookupObjectID.Value.ToString() : loadKeyFieldValue.Value
                                                });
                                            }
                                        }
                                    }
                                }
                            });

                            SaveChanges();

                            if (!li.Status.HasValue)
                            {
                                li.Status = true;
                                li.StatusMessage = "Added";
                            }
                        }
                        else
                        {
                            // This is an EXISTING artifact.



                            SaveChanges();

                            li.Status = true;
                            li.StatusMessage = "Updated";
                        }
                    }

                    //Save the changes to the LoadItems, whether we were able to resolve any to existing assets.
                    ChangeTracker.DetectChanges();
                    SaveChanges();
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

                var col = columns.Where(x => string.Compare(x.Name, fusionAttributeType.TextPath, true) == 0).First();

                return col.ColumnIndex;
            }
            else
            {
                var col = columns.Where(x => string.Compare($"{objectName} Asset ID", x.Name, true) == 0).FirstOrDefault();

                if (col == null)
                    throw new Exception($"BULK LOAD CANNOT FIND ASSET ID COLUMN : [{objectName} Asset ID]");

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

            var subjectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Subject, intersectType.SubjectName, intersectType.SubjectID, columns);
            var objectAssetIDFieldIndex = getAssetIDFieldIndex(intersectType.Object, intersectType.ObjectName, intersectType.ObjectID, columns);
                        
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
            }

            var rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();
            
            while (rowData != null && rowData.Count > 0)
            {
                BulkLoadStatusMsg = "";

                int objectId = getItemIdFromKeyFields(rowData, objectAssetIDFieldIndex, intersectType.Object.Replace("Type", ""), intersectType.ObjectID);

                int subjectId = getItemIdFromKeyFields(rowData, subjectAssetIDFieldIndex, intersectType.Subject.Replace("Type", ""), intersectType.SubjectID);

                int intersectId = (operation == BulkRelationshipOperation.Relate) ?
                    RelateObjects(rowData, objectId, subjectId, intersectType.Object.Replace("Type", ""), intersectType.Subject.Replace("Type", ""), intersectType.ID, customFieldTypes, customFieldTypeMap) :
                    (await UnrelateObjects(objectId, subjectId, intersectType.Object.Replace("Type", ""), intersectType.Subject.Replace("Type", ""), intersectType.ID)); 
                
                // update status for this item
                var statusSql = "update LoadItem set [Object] = 'Intersect', ObjectID = @objectId, Status = 1, StatusMessage = @msg where LoadID = @loadId and RowIndex = @rowIndex";

                await QueryAsync<int>(statusSql, new { objectId = intersectId, msg = BulkLoadStatusMsg, loadId = loadId, rowIndex = currentRowIndex });
                
                //next row
                currentRowIndex++;

                rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();
            }
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
                    intersectId = existingIntersect.ID;

                    BulkLoadStatusMsg = "Relationship successfully removed.";

                    Intersects.Remove(existingIntersect);

                    SaveChanges();

                    //delete any fields to the relationship here
                    var deleteFieldsSql = @"delete from field where objecttype = 'Intersect' and objectid = @intersectId";

                    await QueryAsync<int>(deleteFieldsSql, new { intersectId = intersectId });
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
                            var existingField = Fields.Where(x => x.ObjectType == "Intersect" && x.ObjectID == intersectId).FirstOrDefault();

                            if (existingField != null)
                            {
                                existingField.Value = val.Value;
                            }
                            else
                            {
                                Fields.Add(new Field
                                {
                                    FieldTypeID = ft.ID,
                                    ObjectID = intersectId,
                                    ObjectType = "Intersect",
                                    Value = val.Value
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
