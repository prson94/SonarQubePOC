using d360.core;
using d360.core.entities;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
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
		coalesce(D.TextPath, 'Default') as ObjectName,
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
		left join cache.ObjectDetails D on D.[Object] = L.[Object] and D.ObjectID = L.ObjectID
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy       
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T 
";

        public IEnumerable<LoadDetail> GetLoadDetails()
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " order by L.ID desc");
        }

        public LoadDetail GetLoadDetail(int id)
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " where ID = " + id).SingleOrDefault();
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
                                                    var intersect = new Intersect { IntersectTypeID = ft.LookupObjectID.Value, Deleted = false, Visible = true };

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

        # endregion Parse Spreadsheet Methods

        #endregion
    }
}
