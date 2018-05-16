using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<IntegrationExecution> IntegrationExecutions { get; set; }

        public DbSet<IntegrationExecutionAsset> IntegrationExecutionAssets { get; set; }

        public DbSet<IntegrationExecutionAssetType> IntegrationExecutionAssetTypes { get; set; }

        public DbSet<IntegrationSetting> IntegrationSettings { get; set; }

        public DbSet<IntegrationAssetType> IntegrationAssetTypes { get; set; }

        public DbSet<IntegrationAssetTypeFieldItem> IntegrationAssetTypeFieldItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItem> IntegrationAssetTypeRelationItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItemTarget> IntegrationAssetTypeRelationItemTargets { get; set; }

        public DbSet<IntegrationAssetTypeRoleItem> IntegrationAssetTypeRoleItems { get; set; }

        public DbSet<IntegrationUnresolvedRelationItem> IntegrationUnresolvedRelationItems { get; set; }

        #endregion

        #region Engine Methods

        public List<AssetImportResult> BulkAssetsImport(SystemObjects ot, int otid, BulkAssetImport import)
        {
            var results = new List<AssetImportResult>();

            var sType = ot.ToString();
            var assetTable = new System.Data.DataTable();
            var assetFieldTable = new System.Data.DataTable();

            assetTable.Columns.Add("ItemNumber", typeof(int));
            assetTable.Columns.Add("SourceID", typeof(string));
            assetTable.Columns.Add("Message", typeof(string));
            assetTable.Columns.Add("Success", typeof(bool));
            assetTable.Columns.Add("ParentID", typeof(int));
            assetTable.Columns.Add("Object", typeof(string));
            assetTable.Columns.Add("ObjectID", typeof(int));
            assetTable.Columns.Add("Name", typeof(string));     // For Fusion Data
            assetTable.Columns.Add("OptionalID", typeof(int));  // For Fusion Data (FusionID)
            assetTable.Columns.Add("IsNew", typeof(bool));

            assetFieldTable.Columns.Add("ItemNumber", typeof(int));
            assetFieldTable.Columns.Add("FieldName", typeof(string));
            assetFieldTable.Columns.Add("FieldValue", typeof(string));
            assetFieldTable.Columns.Add("FieldTypeID", typeof(int));

            #region Generate data sets

            #region Parent validation. Do they exist?

            var predicateType = PredicateType.InterTypeHierarchy;

            if (ot == SystemObjects.PolicyType || ot == SystemObjects.TaxonomyType)
            {
                predicateType = PredicateType.IntraTypeHierarchy;
            }

            var parentIntersectType = Filter<IntersectType>(i => i.Object == sType && i.ObjectID == otid && i.Predicate.Type == predicateType).FirstOrDefault();

            var unvalidatedParentSourceIDs = new List<string>();
            var unvalidatedParents = new List<int>();
            var validatedParents = new Dictionary<int, string>();
            var parentArtifactTypeName = "";
            if (parentIntersectType != null)
            {
                for (int i = 1; i <= import.Count; i++)
                {
                    var model = import[i - 1];
                    if (model.ContainsKey("ParentID"))
                    {
                        int pID;
                        if (int.TryParse(model["ParentID"].ToString(), out pID))
                        {
                            if (!unvalidatedParents.Contains(pID))
                            {
                                unvalidatedParents.Add(pID);
                            }
                        }
                    }
                    if (model.ContainsKey("ParentSourceID"))
                    {
                        unvalidatedParentSourceIDs.Add(model["ParentSourceID"].ToString().Trim());
                    }
                }

                if (unvalidatedParents.Count > 0)
                {
                    var pList = Filter<Asset>(i =>
                            i.AssetType.Object == parentIntersectType.Subject &&
                            i.AssetType.ObjectID == parentIntersectType.SubjectID &&
                            unvalidatedParents.Contains(i.ObjectID)
                        ).Select(i => new { k = i.ObjectID, v = i.ID.ToString() }).ToList();

                    pList.ForEach(i =>
                    {
                        validatedParents.Add(i.k, i.v);
                    });
                }
                if (unvalidatedParentSourceIDs.Count > 0)
                {
                    var pList = Filter<Asset>(i =>
                        i.AssetType.Object == parentIntersectType.Subject &&
                        i.AssetType.ObjectID == parentIntersectType.SubjectID &&
                        unvalidatedParentSourceIDs.Contains(i.SourceID)
                    ).Select(i => new { k = i.ObjectID, v = i.SourceID }).ToList();

                    pList.ForEach(i =>
                    {
                        if (!validatedParents.ContainsKey(i.k))
                            validatedParents.Add(i.k, i.v);
                    });
                }

                var parentAssetType = Filter<AssetType>(i => i.Object == parentIntersectType.Subject && i.ObjectID == parentIntersectType.SubjectID).FirstOrDefault();
                if (parentAssetType != null)
                    parentArtifactTypeName = parentAssetType.Name.ToLower();
            }

            #endregion

            for (int i = 1; i <= import.Count; i++)
            {
                var model = import[i - 1];
                var result = new AssetImportResult { ItemNumber = i, Message = "", Success = true };

                if (parentIntersectType != null)
                {
                    if (model.ContainsKey("ParentID"))
                    {
                        if (!validatedParents.ContainsKey(int.Parse(model["ParentID"].ToString())))
                        {
                            result.Message += $"Value ({model["ParentID"].ToString()}) in ParentID property does not correspond to a known {parentArtifactTypeName}.; ";
                            result.Success = false;
                        }
                    }
                    else if (model.ContainsKey("ParentSourceID"))
                    {
                        if (validatedParents.ContainsValue(model["ParentSourceID"].ToString().Trim()))
                        {
                            model["ParentID"] = validatedParents.First(vP => vP.Value == model["ParentSourceID"].ToString().Trim()).Key.ToString();
                        }
                        //else
                        //{
                        //    result.Message += $"Value ({model["ParentSourceID"]}) in ParentSourceID property does not correspond to a known {parentArtifactTypeName}.; ";
                        //    result.Success = false;
                        //}
                    }
                    else
                    {
                        if (ot == SystemObjects.ArtifactType || ot == SystemObjects.FusionAttributeType)
                        {
                            result.Message += "Neither ParentID nor ParentSourceID properties are present for asset;";
                            result.Success = false;
                        }
                    }
                }

                if (model.ContainsKey("SourceID"))
                {
                    result.SourceID = model["SourceID"].ToString();
                }
                else
                {
                    result.Success = false;
                    result.Message = "No SourceID specified for this asset. A SourceID must be present.";
                }

                if (result.Success)
                {
                    var row = assetTable.NewRow();

                    row["ItemNumber"] = result.ItemNumber;
                    row["SourceID"] = result.SourceID;
                    if (model.ContainsKey("ParentID"))
                    {
                        row["ParentID"] = int.Parse(model["ParentID"].ToString());
                    }

                    if (model.ContainsKey("Name"))
                    {
                        row["Name"] = model["Name"].ToString();
                    }

                    if (model.ContainsKey("FusionID"))
                    {
                        row["OptionalID"] = int.Parse(model["FusionID"].ToString());
                    }

                    assetTable.Rows.Add(row);

                    foreach (var k in model.Keys)
                    {
                        if (k != "ParentID" && k != "ParentSourceID" && k != "SourceID")
                        {
                            if (!string.IsNullOrEmpty(model[k]))
                            {
                                var fieldRow = assetFieldTable.NewRow();

                                fieldRow["ItemNumber"] = result.ItemNumber;
                                fieldRow["FieldName"] = k.Trim();
                                fieldRow["FieldValue"] = (model[k] + "").Trim();

                                assetFieldTable.Rows.Add(fieldRow);
                            }
                        }
                    }
                }

                results.Add(result);
            }

            #endregion

            #region

            List<DatabaseBulkAssetResult> retResults = null;

            if ((Database.Connection as SqlConnection).State != System.Data.ConnectionState.Open)
                (Database.Connection as SqlConnection).Open();

            Database.Connection.Execute("DROP TABLE IF EXISTS #AssetTable");
            Database.Connection.Execute("DROP TABLE IF EXISTS #AssetFieldTable");
            Database.Connection.Execute("DROP TABLE IF EXISTS #ObjectMergeTableResult");

            using (var trans = (Database.Connection as SqlConnection).BeginTransaction())
            {
                #region Asset Bulk Copy

                Database.Connection.Execute(@"
create table #AssetTable (
    ItemNumber int not null,
    SourceID nvarchar(1000) null,
    Message nvarchar(2500) null,
    Success bit null,
    ParentID int null,
    Object varchar(50) null,
    ObjectID int null,
    Name nvarchar(250) null,
    OptionalID int null,
    IsNew bit null
)", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetTable ON #AssetTable ( [SourceID] ASC ) INCLUDE ( ItemNumber )", transaction: trans);

                var assetBulkCopy = new SqlBulkCopy(
                    (SqlConnection)Database.Connection,
                    SqlBulkCopyOptions.Default,
                    trans);

                assetBulkCopy.BatchSize = assetTable.Rows.Count;
                assetBulkCopy.DestinationTableName = "#AssetTable";
                assetBulkCopy.BulkCopyTimeout = 3600;

                assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                assetBulkCopy.ColumnMappings.Add("Message", "Message");
                assetBulkCopy.ColumnMappings.Add("Success", "Success");
                assetBulkCopy.ColumnMappings.Add("ParentID", "ParentID");
                assetBulkCopy.ColumnMappings.Add("Object", "Object");
                assetBulkCopy.ColumnMappings.Add("ObjectID", "ObjectID");
                assetBulkCopy.ColumnMappings.Add("Name", "Name");               // For Fusion Data
                assetBulkCopy.ColumnMappings.Add("OptionalID", "OptionalID");   // For Fusion Data
                assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                assetBulkCopy.WriteToServer(assetTable);

                #endregion

                #region Asset Field Bulk Copy

                Database.Connection.Execute(@"
create table #AssetFieldTable (
    ItemNumber int not null,
    FieldName nvarchar(250) not null,
    FieldValue nvarchar(max) null,
    FieldTypeID int null
)", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetFieldTable ON #AssetFieldTable ( ItemNumber ASC ) INCLUDE ( FieldTypeID )", transaction: trans);

                var assetFieldBulkCopy = new SqlBulkCopy(
                    (SqlConnection)Database.Connection,
                    SqlBulkCopyOptions.Default,
                    trans);

                assetFieldBulkCopy.BatchSize = assetTable.Rows.Count;
                assetFieldBulkCopy.DestinationTableName = "#AssetFieldTable";
                assetFieldBulkCopy.BulkCopyTimeout = 3600;

                assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                assetBulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                assetBulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                assetBulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                assetFieldBulkCopy.WriteToServer(assetFieldTable);

                #endregion

                Database.Connection.Execute($@"create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));", transaction: trans);
                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC )", transaction: trans);

                var o = ot.ToString().Replace("Type", "");

                switch (ot)
                {
                    case SystemObjects.ArtifactType:
                        #region
                        Database.Connection.Execute($@"
merge into  Artifact T
using       (
            select      min(ItemNumber) as ItemNumber,
                        SourceID
            from        #AssetTable
            group by    SourceID
            ) S
on          (
                T.ArtifactTypeID = @id and 
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (ArtifactTypeID, SourceID, CreatedOn, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.SourceID, getutcdate(), @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid, @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                        break;
                    #endregion
                    case SystemObjects.FusionAttributeType:
                        #region
                        Database.Connection.Execute($@"
merge into  FusionAttribute T
using       (
            select      min(ItemNumber) as ItemNumber,
                        ParentID,
                        OptionalID, 
                        Name, 
                        SourceID
            from        #AssetTable
            group by    ParentID, OptionalID, Name, SourceID
            ) S
on          (
                T.FusionAttributeTypeID = @id and 
                T.FusionID = S.OptionalID and
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.Deleted = 0,
            T.ParentID = S.ParentID
when not matched by target then
    insert  (FusionAttributeTypeID, FusionID, ParentID, Name, SourceID)
    values  (@id, S.OptionalID, S.ParentID, S.Name, S.SourceID)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid }, transaction: trans, commandTimeout: 1200);
                        break;
                    #endregion
                    case SystemObjects.PolicyType:
                        #region
                        Database.Connection.Execute($@"
merge into  [Policy] T
using       (
            select      min(ItemNumber) as ItemNumber,
                        SourceID
            from        #AssetTable
            group by    SourceID
            ) S
on          (
                T.PolicyTypeID = @id and 
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (PolicyTypeID, SourceID, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.SourceID, @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid, @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                        break;
                    #endregion
                    case SystemObjects.ReferenceItemType:
                        #region
                        Database.Connection.Execute($@"
merge into  [ReferenceItem] T
using       (
            select      min(A.ItemNumber) as ItemNumber,
                        A.SourceID,
                        F.FieldValue as Code
            from        #AssetTable A
                        left join #AssetFieldTable F on F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
            group by    A.SourceID, F.FieldValue
            ) S
on          (
                T.ReferenceItemTypeID = @id and 
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate(),
            T.Code = S.Code
when not matched by target then
    insert  (ReferenceItemTypeID, SourceID, Code, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.SourceID, S.Code, @r, getutcdate(), @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid, @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                        break;
                    #endregion
                    case SystemObjects.RuleType:
                        #region
                        Database.Connection.Execute($@"
merge into  [Rule] T
using       (
            select      min(ItemNumber) as ItemNumber,
                        SourceID
            from        #AssetTable
            group by    SourceID
            ) S
on          (
                T.RuleTypeID = @id and 
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (RuleTypeID, SourceID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.SourceID, @r, getutcdate(), @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid, @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                        break;
                    #endregion
                    case SystemObjects.TaxonomyType:
                        #region
                        Database.Connection.Execute($@"
merge into  Taxonomy T
using       (
            select      min(ItemNumber) as ItemNumber,
                        SourceID
            from        #AssetTable
            group by    SourceID
            ) S
on          (
                T.TaxonomyTypeID = @id and 
                S.SourceID is not null and 
                S.SourceID <> '' and 
                S.SourceID = T.SourceID
            )
when matched then
    update set
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (TaxonomyTypeID, SourceID, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.SourceID, @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
", new { id = otid, @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                        break;
                        #endregion
                }

                Database.Connection.Execute($@"
update  T
set     T.Object = @o,
        T.ObjectID = S.ID,
        T.IsNew = case when S.[Action] = 'INSERT' then 1 else 0 end,
        T.Success = case when S.ID is not null then 1 else 0 end
from    #AssetTable T
        left join #ObjectMergeTableResult S on S.ItemNumber = T.ItemNumber;
", new { id = otid, @r = CurrentResourceID, o }, transaction: trans, commandTimeout: 1200);

                #region Deal with parent relationship if required

                if (parentIntersectType != null)
                {
                    Database.Connection.Execute($@"
merge into  [Intersect] T
using       (
            select  Object as Subject, 
                    ParentID as SubjectID, 
                    Object as Object, 
                    ObjectID as ObjectID 
            from    #AssetTable 
            where   ParentID is not null 
                    and ObjectID is not null 
            ) S
on          (
                T.IntersectTypeID = {parentIntersectType.ID} and 
                T.Subject = S.Subject and 
                T.SubjectID = S.SubjectID and 
                T.Object = S.Object and 
                T.ObjectID = S.ObjectID
            )
when not matched by target then
    insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
    values  ({parentIntersectType.ID}, S.Subject, S.SubjectID, S.Object, S.ObjectID, @r, @r);
", new { @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);
                }

                #endregion

                #region Update the asset field temp table with the proper FieldTypeID

                Database.Connection.Execute(@"
update  T
set     T.FieldTypeID = S.ID
from    #AssetFieldTable T
        inner join FieldType S on S.Object = @ot and S.ObjectID = @otid and S.Name = T.FieldName
", new { otid, ot = ot.ToString() }, transaction: trans, commandTimeout: 1200);

                #endregion

                #region Merge into the Field table

                Database.Connection.Execute(@"
merge into  Field T
using       (
            select  A.Object,
                    A.ObjectID,
                    F.*,
                    FT.Type, 
                    FT.LookupDisplayFormat, 
                    FT.LookupObjectType, 
                    FT.LookupObjectID, 
                    FT.AllowMultipleValues
            from    #AssetFieldTable F
                    inner join #AssetTable A on A.ItemNumber = F.ItemNumber 
                        and A.ObjectID is not null 
                        and F.FieldTypeID is not null
                    inner join FieldType FT on FT.ID = F.FieldTypeID 
                                and FT.[Type] not in ('Attribute', 'FilteredLookup', 'ComplexRelationLookup', 'DataTableSelect', 'OwnershipLookup', 'Relationship', 'FieldFromRelationship', 'RefListRelationship') 
                                and FT.[Type] <> 'Lookup' 
            ) S
on          (
                T.FieldTypeID = S.FieldTypeID and 
                T.ObjectType = S.Object and
                T.ObjectID = S.ObjectID
            )
when matched then
    update set
            T.Value = S.FieldValue,
            T.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.FieldValue, S.AllowMultipleValues)
when not matched by target then
    insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
    values  (S.FieldTypeID, S.Object, S.ObjectID, S.FieldValue, utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.FieldValue, S.AllowMultipleValues));
", new { id = otid }, transaction: trans, commandTimeout: 1200);

                Database.Connection.Execute(@"
merge into  Field T
using       (
            select  distinct 
                    A.Object, 
                    A.ObjectID, 
                    F.FieldTypeID,
                    LV.Value,
                    FT.Type, 
                    FT.LookupDisplayFormat, 
                    FT.LookupObjectType, 
                    FT.LookupObjectID, 
                    FT.AllowMultipleValues
            from    #AssetFieldTable F
                    inner join #AssetTable A on A.ItemNumber = F.ItemNumber 
                        and A.ObjectID is not null 
                        and F.FieldTypeID is not null
                    inner join FieldType FT on FT.ID = F.FieldTypeID and FT.[Type] = 'Lookup' 
                    inner join FieldLookupValue LV on LV.LookupObjectType = FT.LookupObjectType and LV.LookupObjectID = FT.LookupObjectID and LV.Text = F.FieldValue
            ) S
on          (
                T.FieldTypeID = S.FieldTypeID and 
                T.ObjectType = S.Object and 
                T.ObjectID = S.ObjectID 
            )
when matched then
    update set
            T.Value = S.Value,
            T.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.Value, S.AllowMultipleValues)
when not matched by target then
    insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
    values  (S.FieldTypeID, S.Object, S.ObjectID, S.Value, utility.GetFormattedFieldLookupValueWithMultiple(S.Type, S.LookupDisplayFormat, S.LookupObjectType, S.LookupObjectID, S.Value, S.AllowMultipleValues));
", new { id = otid }, transaction: trans, commandTimeout: 1200);

                #endregion

                retResults = Database.Connection.Query<DatabaseBulkAssetResult>("select ItemNumber, ObjectID, SourceID, Message, Success, IsNew from #AssetTable", transaction: trans).ToList();
                trans.Commit();
            }

            #region Cycle through the return results form the database, and update the results collection to send back to the caller.

            var events = new List<EventInfo>();
            var sObject = (SystemObjects)Enum.Parse(typeof(SystemObjects), sType.Replace("Type", ""));
            retResults.ForEach(d =>
            {
                var cr = results.SingleOrDefault(i => i.ItemNumber == d.ItemNumber);
                if (cr != null)
                {
                    if (string.IsNullOrEmpty(cr.Message))
                    {
                        cr.Success = d.Success;
                        cr.Message = d.Success ? (d.IsNew ? "Created" : "Updated") : "Failed";
                        if (d.Success)
                        {
                            events.Add(new EventInfo
                            {
                                CompanyID = CurrentCompanyID,
                                DomainPrefix = CurrentCompanyDomain,
                                ResourceID = CurrentResourceID,
                                Action = d.IsNew ? ChangeType.Add : ChangeType.Update,
                                Object = new EventObjectInfo { Object = sObject, ObjectType = ot, ObjectID = d.ObjectID, ObjectTypeID = otid }
                            });

                            if (events.Count > 50)
                            {
                                QueueSource.CreateTopicMessages(events);
                                events.Clear();
                            }
                        }
                    }
                }
            });

            if (events.Count > 0)
            {
                QueueSource.CreateTopicMessages(events);
            }

            #endregion

            return results;

            #endregion
        }

        public List<dynamic> BulkOwnersImport(BulkOwnerImport import)
        {
            var ownerTable = new System.Data.DataTable();

            ownerTable.Columns.Add("ItemNumber", typeof(int));
            ownerTable.Columns.Add("SourceID", typeof(string));
            ownerTable.Columns.Add("RoleName", typeof(string));
            ownerTable.Columns.Add("UserId", typeof(string));
            ownerTable.Columns.Add("UserIdFieldName", typeof(string));
            ownerTable.Columns.Add("Message", typeof(string));
            ownerTable.Columns.Add("Success", typeof(bool));
            ownerTable.Columns.Add("IsNew", typeof(bool));

            #region Generate data sets

            for (int i = 1; i <= import.Items.Count; i++)
            {
                var model = import.Items[i - 1];
                model.ItemNumber = i;

                var row = ownerTable.NewRow();

                row["ItemNumber"] = model.ItemNumber;
                row["SourceID"] = model.SourceID;
                row["RoleName"] = model.RoleName;
                row["UserId"] = model.UserId;
                row["UserIdFieldName"] = import.UserIdFieldName;

                ownerTable.Rows.Add(row);
            }

            #endregion

            #region

            List<dynamic> retResults = null;

            if ((Database.Connection as SqlConnection).State != System.Data.ConnectionState.Open)
                (Database.Connection as SqlConnection).Open();

            Database.Connection.Execute("DROP TABLE IF EXISTS #OwnershipTable");
            Database.Connection.Execute("DROP TABLE IF EXISTS #UserTableResult");

            using (var trans = (Database.Connection as SqlConnection).BeginTransaction())
            {
                #region Asset Bulk Copy

                Database.Connection.Execute(@"
create table #OwnershipTable (
    ItemNumber int not null,
    SourceID nvarchar(1000) null,
    RoleName nvarchar(1000) null,
    UserId nvarchar(1000) null,
    UserIdFieldName nvarchar(50) null,
    Message nvarchar(2500) null,
    Success bit null,
    IsNew bit null
)", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempOwnershipTable ON #OwnershipTable ( SourceID ASC ) INCLUDE ( ItemNumber, RoleName )", transaction: trans);

                var assetBulkCopy = new SqlBulkCopy(
                    (SqlConnection)Database.Connection,
                    SqlBulkCopyOptions.Default,
                    trans);

                assetBulkCopy.BatchSize = ownerTable.Rows.Count;
                assetBulkCopy.DestinationTableName = "#OwnershipTable";
                assetBulkCopy.BulkCopyTimeout = 3600;

                assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                assetBulkCopy.ColumnMappings.Add("RoleName", "RoleName");
                assetBulkCopy.ColumnMappings.Add("UserId", "UserId");
                assetBulkCopy.ColumnMappings.Add("UserIdFieldName", "UserIdFieldName");
                assetBulkCopy.ColumnMappings.Add("Message", "Message");
                assetBulkCopy.ColumnMappings.Add("Success", "Success");
                assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                assetBulkCopy.WriteToServer(ownerTable);

                #endregion

                Database.Connection.Execute($@"create table #UserTableResult (ItemNumber int, ResourceID int, UserId nvarchar(1000) null, UserIdFieldName nvarchar(50) null);", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempUserTableResult ON #UserTableResult ( UserId ASC ) INCLUDE ( ItemNumber, UserIdFieldName )", transaction: trans);

                Database.Connection.Execute($@"
insert into #UserTableResult 
    select ItemNumber, null, UserId, UserIdFieldName from #OwnershipTable; 

update  T 
set     T.ResourceID = S.ResourceID 
from    #UserTableResult  T 
        inner join reporting.Global_Resource S on S.Email = T.UserId and lower(ltrim(rtrim(T.UserIdFieldName))) in ('username', 'email'); 

update  T 
set     T.ResourceID = F.ObjectID 
from    #UserTableResult  T 
        inner join FieldType FT on FT.Object = 'ResourceType' and FT.ObjectID = 1 and lower(ltrim(rtrim(FT.Name))) = lower(ltrim(rtrim(T.UserIdFieldName))) 
        inner join Field F on F.FieldTypeID = FT.ID and F.FormattedValue = T.UserId; ", transaction: trans);

                Database.Connection.Execute($@"create table #OwnershipMergeTableResult (ID bigint, ItemNumber int, [Action] nvarchar(10));", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempOwnershipMergeTableResult ON #OwnershipMergeTableResult ( ItemNumber ASC ) INCLUDE ( ID )", transaction: trans);

                Database.Connection.Execute($@"
merge into  [ResponsibilityTypeRelationOverrideItem] T
using       (
            select  R.ItemNumber,
                    RTR.ResponsibilityTypeID,
                    S.ID as AssetID,
		            U.ResourceID
            from    #OwnershipTable R
		            inner join Asset S on S.SourceID = R.SourceID
		            inner join AssetType ST on ST.ID = S.AssetTypeID

		            inner join ResponsibilityTypeRelation	RTR on RTR.ObjectType = ST.Object and RTR.ObjectID = ST.ObjectID
                    inner join ResponsibilityType           RT on RTR.ResponsibilityTypeID = RT.ID and LOWER(RT.Name) = LOWER(RTRIM(LTRIM(R.RoleName)))
                    inner join #UserTableResult             U on U.ItemNumber = R.ItemNumber and U.ResourceID is not null
            ) S
on          (
                T.ResponsibilityTypeID = S.ResponsibilityTypeID and 
                T.AssetID = S.AssetID and 
                T.SecurityAsset = 'R' and 
                T.SecurityAssetID = S.ResourceID
            )
when not matched by target then
    insert  (ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID)
    values  (S.ResponsibilityTypeID, S.AssetID, 'R', S.ResourceID)
output inserted.ID, S.ItemNumber, $action into #OwnershipMergeTableResult;

update  T
set     T.IsNew = case when S.[Action] = 'INSERT' then 1 else 0 end
from    #OwnershipTable T
        inner join #OwnershipMergeTableResult S on S.ItemNumber = T.ItemNumber;
", new { @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);


                retResults = Database.Connection.Query<dynamic>("select * from #OwnershipTable", transaction: trans).ToList();

                trans.Commit();
            }

            #endregion

            return retResults;
        }

        public List<dynamic> BulkRelationshipsImport(BulkRelationshipImport import)
        {
            var relationshipTable = new System.Data.DataTable();

            relationshipTable.Columns.Add("ItemNumber", typeof(int));
            relationshipTable.Columns.Add("SubjectSourceID", typeof(string));
            relationshipTable.Columns.Add("ObjectSourceID", typeof(string));
            relationshipTable.Columns.Add("PredicateType", typeof(int));
            relationshipTable.Columns.Add("IntersectTypeID", typeof(int));
            relationshipTable.Columns.Add("Message", typeof(string));
            relationshipTable.Columns.Add("Success", typeof(bool));
            relationshipTable.Columns.Add("IntersectID", typeof(int));
            relationshipTable.Columns.Add("IsNew", typeof(bool));

            #region Generate data sets

            for (int i = 1; i <= import.Count; i++)
            {
                var model = import[i - 1];
                model.ItemNumber = i;

                var row = relationshipTable.NewRow();

                row["ItemNumber"] = model.ItemNumber;
                row["SubjectSourceID"] = model.SubjectSourceID;
                row["ObjectSourceID"] = model.ObjectSourceID;
                row["PredicateType"] = model.PredicateType;
                row["IntersectTypeID"] = model.IntersectTypeID;

                relationshipTable.Rows.Add(row);
            }

            #endregion

            List<dynamic> retResults = null;

            #region

            if ((Database.Connection as SqlConnection).State != System.Data.ConnectionState.Open)
                (Database.Connection as SqlConnection).Open();

            Database.Connection.Execute("DROP TABLE IF EXISTS #RelationshipTable");
            Database.Connection.Execute("DROP TABLE IF EXISTS #RelationshipMergeTableResult");

            using (var trans = (Database.Connection as SqlConnection).BeginTransaction())
            {
                #region Asset Bulk Copy

                Database.Connection.Execute(@"
create table #RelationshipTable (
    ItemNumber int not null,
    SubjectSourceID nvarchar(1000) null,
    ObjectSourceID nvarchar(1000) null,
    PredicateType int null,
    IntersectTypeID int null,
    Message nvarchar(2500) null,
    Success bit null,
    IntersectID int null,
    IsNew bit null
)", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempRelationshipTable ON #RelationshipTable ( IntersectTypeID ASC, SubjectSourceID ASC, ObjectSourceID ASC ) INCLUDE ( ItemNumber )", transaction: trans);

                var assetBulkCopy = new SqlBulkCopy(
                    (SqlConnection)Database.Connection,
                    SqlBulkCopyOptions.Default,
                    trans);

                assetBulkCopy.BatchSize = relationshipTable.Rows.Count;
                assetBulkCopy.DestinationTableName = "#RelationshipTable";
                assetBulkCopy.BulkCopyTimeout = 3600;

                assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                assetBulkCopy.ColumnMappings.Add("SubjectSourceID", "SubjectSourceID");
                assetBulkCopy.ColumnMappings.Add("ObjectSourceID", "ObjectSourceID");
                assetBulkCopy.ColumnMappings.Add("PredicateType", "PredicateType");
                assetBulkCopy.ColumnMappings.Add("IntersectTypeID", "IntersectTypeID");
                assetBulkCopy.ColumnMappings.Add("Message", "Message");
                assetBulkCopy.ColumnMappings.Add("Success", "Success");
                assetBulkCopy.ColumnMappings.Add("IntersectID", "IntersectID");
                assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                assetBulkCopy.WriteToServer(relationshipTable);

                #endregion

                Database.Connection.Execute($@"create table #RelationshipMergeTableResult (IntersectID int, ItemNumber int, [Action] nvarchar(10));", transaction: trans);

                Database.Connection.Execute(@"CREATE NONCLUSTERED INDEX IX_TempRelationshipMergeTableResult ON #RelationshipMergeTableResult ( ItemNumber ASC ) INCLUDE ( IntersectID )", transaction: trans);

                Database.Connection.Execute($@"
merge into  [Intersect] T
using       (
            select  R.ItemNumber,
                    IT.ID as IntersectTypeID,
		            S.Object as Subject,
		            S.ObjectID as SubjectID,
		            T.Object,
		            T.ObjectID
            from    #RelationshipTable R
		            inner join Asset S on S.SourceID = R.SubjectSourceID
		            inner join AssetType ST on ST.ID = S.AssetTypeID

		            inner join Asset T on T.SourceID = R.ObjectSourceID
		            inner join AssetType TT on TT.ID = T.AssetTypeID

		            inner join IntersectTypeDetail	IT on IT.Subject = ST.Object and IT.SubjectID = ST.ObjectID and 
										            IT.Object = TT.Object and IT.ObjectID = TT.ObjectID and
										            IT.ID = R.IntersectTypeID
            ) S
on          (
                T.IntersectTypeID = S.IntersectTypeID and 
                T.Subject = S.Subject and 
                T.SubjectID = S.SubjectID and 
                T.Object = S.Object and 
                T.ObjectID = S.ObjectID
            )
when not matched by target then
    insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
    values  (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, @r, @r)
output inserted.ID, S.ItemNumber, $action into #RelationshipMergeTableResult; 

update  T
set     T.IntersectID = S.IntersectID,
        T.IsNew = case when S.[Action] = 'INSERT' then 1 else 0 end
from    #RelationshipTable T
        inner join #RelationshipMergeTableResult S on S.ItemNumber = T.ItemNumber; 

update  T
set     T.IntersectID = I.ID
from    #RelationshipTable T
        left join Asset S on S.SourceID = T.SubjectSourceID
        left join Asset O on O.SourceID = T.ObjectSourceID
        left join [Intersect] I on T.IntersectTypeID = I.IntersectTypeID 
                and I.Subject = S.Object 
                and I.SubjectID = S.ObjectID 
                and I.Object = O.Object 
                and I.ObjectID = O.ObjectID;",
            new { @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);

                Database.Connection.Execute($@"
merge into  [integration].[UnresolvedRelationItem] T
using       (
            select  SubjectSourceID,
                    ObjectSourceID,
                    IntersectTypeID
            from    #RelationshipTable
            where   IntersectID is null
            ) S
on          (
                T.IntersectTypeID = S.IntersectTypeID and 
                T.SubjectSourceID = S.SubjectSourceID and 
                T.ObjectSourceID = S.ObjectSourceID
            )
when matched then
    update set
        T.AttemptCount = T.AttemptCount + 1,
        T.MostRecentAttemptOn = getutcdate()
when not matched by target then
    insert  (IntersectTypeID, SubjectSourceID, ObjectSourceID, AttemptCount, MostRecentAttemptOn)
    values  (S.IntersectTypeID, S.SubjectSourceID, S.ObjectSourceID, 1, getutcdate());
", new { @r = CurrentResourceID }, transaction: trans, commandTimeout: 1200);

                retResults = Database.Connection.Query<dynamic>("select * from #RelationshipTable", transaction: trans).ToList();

                trans.Commit();
            }

            try
            {
                var events = new List<EventInfo>();
                retResults.ForEach(r => {
                    if (r.IsNew == true && r.IntersectID != null)
                    {
                        events.Add(new EventInfo
                        {
                            CompanyID = CurrentCompanyID,
                            DomainPrefix = CurrentCompanyDomain,
                            ResourceID = CurrentResourceID,
                            Action = ChangeType.Add,
                            Object = new EventObjectInfo { Object = SystemObjects.Intersect, ObjectType = SystemObjects.IntersectType, ObjectID = r.IntersectID, ObjectTypeID = r.IntersectTypeID }
                        });
                        if (events.Count > 50)
                        {
                            QueueSource.CreateTopicMessages(events);
                            events.Clear();
                        }
                    }
                });
                if (events.Count > 0)
                {
                    QueueSource.CreateTopicMessages(events);
                }
            }
            catch (Exception ex)
            {
            }

            #endregion

            return retResults;
        }

        public void BulkExecutionAssetLoad(List<IntegrationExecutionAsset> assets)
        {
            var assetTable = new System.Data.DataTable();

            assetTable.Columns.Add("ExecutionID", typeof(long));
            assetTable.Columns.Add("RawObject", typeof(string));
            assetTable.Columns.Add("ErrorMessages", typeof(string));
            assetTable.Columns.Add("SourceID", typeof(string));
            assetTable.Columns.Add("SynchedAssetTypeID", typeof(int));

            assets.ForEach(a =>
            {
                var row = assetTable.NewRow();
                row["ExecutionID"] = a.ExecutionID;
                row["RawObject"] = a.RawObject;
                row["SourceID"] = a.SourceID;
                row["ErrorMessages"] = a.ErrorMessages;
                row["SynchedAssetTypeID"] = a.SynchedAssetTypeID;
                assetTable.Rows.Add(row);
            });

            if (Database.Connection.State != System.Data.ConnectionState.Open)
                Database.Connection.Open();

            Database.Connection.Execute("DROP TABLE IF EXISTS #IntegrationAssetTable");
            using (var trans = (Database.Connection as SqlConnection).BeginTransaction())
            {
                try
                {
                    Database.Connection.Execute(@"
                        create table #IntegrationAssetTable (
	                        ExecutionID bigint NOT NULL,
	                        SynchedAssetTypeID int NOT NULL,
	                        SourceID varchar(100) NOT NULL,
	                        RawObject nvarchar(max) NULL,
	                        ErrorMessages nvarchar(max) NULL
                        )", transaction: trans);

                    Database.Connection.Execute(@"CREATE CLUSTERED INDEX IX_TempIntegrationAssetTable ON #IntegrationAssetTable ( ExecutionID ASC, SynchedAssetTypeID, SourceID ASC )", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(
                        (SqlConnection)Database.Connection,
                        SqlBulkCopyOptions.Default,
                        trans);

                    assetBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetBulkCopy.DestinationTableName = "#IntegrationAssetTable";
                    assetBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    assetBulkCopy.ColumnMappings.Add("RawObject", "RawObject");
                    assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                    assetBulkCopy.ColumnMappings.Add("SynchedAssetTypeID", "SynchedAssetTypeID");
                    assetBulkCopy.ColumnMappings.Add("ErrorMessages", "ErrorMessages");

                    assetBulkCopy.WriteToServer(assetTable);

                    Database.Connection.Execute($@"
merge into  [integration].[ExecutionAsset] T
using       (
            select      ExecutionID,
                        SynchedAssetTypeID,
                        SourceID,
                        RawObject,
                        ErrorMessages
            from        #IntegrationAssetTable
            ) S
on          (
                S.ExecutionID = T.ExecutionID and 
                S.SourceID = T.SourceID and 
                S.SynchedAssetTypeID = T.SynchedAssetTypeID
            )
when matched then
    update set
            T.RawObject = S.RawObject,
            T.ErrorMessages = S.ErrorMessages
when not matched by target then
    insert  (ExecutionID, SynchedAssetTypeID, SourceID, RawObject, ErrorMessages)
    values  (S.ExecutionID, S.SynchedAssetTypeID, S.SourceID, S.RawObject, S.ErrorMessages);", 
    transaction: trans, commandTimeout: 1200);

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                }
            }
        }

        public void ProcessUnresolvedRelationships()
        {
            Execute(@"exec integration.ProcessUnresolvedRelationships");
        }

        #endregion
    }
}
