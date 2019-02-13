using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.extensions;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<IntegrationExecutionAssetType> IntegrationExecutionAssetTypes { get; set; }

        public DbSet<IntegrationSetting> IntegrationSettings { get; set; }

        public DbSet<IntegrationAssetType> IntegrationAssetTypes { get; set; }

        public DbSet<IntegrationAssetTypeFieldItem> IntegrationAssetTypeFieldItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItem> IntegrationAssetTypeRelationItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItemTarget> IntegrationAssetTypeRelationItemTargets { get; set; }

        public DbSet<IntegrationAssetTypeRoleItem> IntegrationAssetTypeRoleItems { get; set; }

        public DbSet<IntegrationUnresolvedRelationItem> IntegrationUnresolvedRelationItems { get; set; }

        #endregion
    }

    public static partial class ConnectionExtensions
    {
        #region API v1 logic

        public static List<dynamic> BulkOwnersImport(this SqlConnection cnn, int currentResourceID, BulkOwnerImport import)
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

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            using (var trans = cnn.BeginTransaction())
            {
                try
                {
                    cnn.Execute("DROP TABLE IF EXISTS #OwnershipTable", transaction: trans);
                    cnn.Execute("DROP TABLE IF EXISTS #OwnershipMergeTableResult", transaction: trans);
                    cnn.Execute("DROP TABLE IF EXISTS #UserTableResult", transaction: trans);

                    #region Asset Bulk Copy

                    cnn.Execute(@"
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

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempOwnershipTable ON #OwnershipTable ( SourceID ASC ) INCLUDE ( ItemNumber, RoleName )", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

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

                    cnn.Execute($@"create table #UserTableResult (ItemNumber int, ResourceID int, UserId nvarchar(1000) null, UserIdFieldName nvarchar(50) null);", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempUserTableResult ON #UserTableResult ( UserId ASC ) INCLUDE ( ItemNumber, UserIdFieldName )", transaction: trans);

                    cnn.Execute($@"
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

                    cnn.Execute($@"create table #OwnershipMergeTableResult (ID bigint, ItemNumber int, [Action] nvarchar(10));", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempOwnershipMergeTableResult ON #OwnershipMergeTableResult ( ItemNumber ASC ) INCLUDE ( ID )", transaction: trans);

                    cnn.Execute($@"
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
    ", new { @r = currentResourceID }, transaction: trans, commandTimeout: 1200);

                    retResults = cnn.Query<dynamic>("select * from #OwnershipTable", transaction: trans).ToList();

                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();                    
                }
            }

            #endregion

            return retResults;
        }

        public static async Task<List<AssetImportResult>> BulkAssetsImport(this SqlConnection cnn, int currentResourceID, SystemObjects ot, int otid, List<Dictionary<string, string>> import)
        {
            var results = new List<AssetImportResult>();

            var sType = ot.ToString();
            var assetTable = new System.Data.DataTable();
            var assetFieldTable = new System.Data.DataTable();

            assetTable.Columns.Add("ItemNumber", typeof(int));
            assetTable.Columns.Add("SourceID", typeof(string));
            assetTable.Columns.Add("Message", typeof(string));
            assetTable.Columns.Add("Success", typeof(bool));
            assetTable.Columns.Add("IntersectTypeID", typeof(int));
            assetTable.Columns.Add("ParentSourceID", typeof(string));
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

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            #region Parent predicate type choice.

            var predicateType = PredicateType.InterTypeHierarchy;

            if (ot == SystemObjects.PolicyType || ot == SystemObjects.TaxonomyType)
            {
                predicateType = PredicateType.IntraTypeHierarchy;
            }

            #endregion

            for (int i = 1; i <= import.Count; i++)
            {
                var model = import[i - 1];
                var result = new AssetImportResult { ItemNumber = i, Message = "", Success = true };

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
                    if (model.ContainsKey("ParentSourceID"))
                    {
                        row["ParentSourceID"] = model["ParentSourceID"].ToString();
                    }
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

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            using (var trans = cnn.BeginTransaction())
            {
                try
                {
                    await cnn.ExecuteAsync("DROP TABLE IF EXISTS #AssetTable", transaction: trans);
                    await cnn.ExecuteAsync("DROP TABLE IF EXISTS #AssetFieldTable", transaction: trans);
                    await cnn.ExecuteAsync("DROP TABLE IF EXISTS #ObjectMergeTableResult", transaction: trans);

                    #region Asset Bulk Copy

                    await cnn.ExecuteAsync(@"
    create table #AssetTable (
        ItemNumber int not null primary key,
        SourceID nvarchar(1000) null,
        Message nvarchar(2500) null,
        Success bit null,
        IntersectTypeID int null,        
        ParentSourceID nvarchar(1000) null,
        ParentID int null,
        Object varchar(50) null,
        ObjectID int null,
        Name nvarchar(250) null,
        OptionalID int null,
        IsNew bit null
    )", transaction: trans);

                    await cnn.ExecuteAsync(@"CREATE NONCLUSTERED INDEX IX_TempAssetTable_SourceID ON #AssetTable ( [SourceID] ASC ) INCLUDE ( ItemNumber )", transaction: trans);
                    await cnn.ExecuteAsync(@"CREATE NONCLUSTERED INDEX IX_TempAssetTable_ParentSourceID ON #AssetTable ( [Object] ASC, [ObjectID] ASC, [ParentSourceID] ASC )", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    assetBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetBulkCopy.DestinationTableName = "#AssetTable";
                    assetBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                    assetBulkCopy.ColumnMappings.Add("Message", "Message");
                    assetBulkCopy.ColumnMappings.Add("Success", "Success");
                    assetBulkCopy.ColumnMappings.Add("IntersectTypeID", "IntersectTypeID");
                    assetBulkCopy.ColumnMappings.Add("ParentSourceID", "ParentSourceID");
                    assetBulkCopy.ColumnMappings.Add("ParentID", "ParentID");
                    assetBulkCopy.ColumnMappings.Add("Object", "Object");
                    assetBulkCopy.ColumnMappings.Add("ObjectID", "ObjectID");
                    assetBulkCopy.ColumnMappings.Add("Name", "Name");               // For Fusion Data
                    assetBulkCopy.ColumnMappings.Add("OptionalID", "OptionalID");   // For Fusion Data
                    assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                    await assetBulkCopy.WriteToServerAsync(assetTable);

                    #endregion

                    #region Resolve Parents, if required.

                    cnn.Execute(@"
update  T
set     T.ParentID = S.ObjectID,
        T.IntersectTypeID = IT.ID
from    #AssetTable T
		inner join Asset S on S.SourceID = T.ParentSourceID
        inner join AssetType ST on ST.ID = S.AssetTypeID
        inner join IntersectType IT on IT.Subject = ST.Object and IT.SubjectID = ST.ObjectID and IT.Object = @ot and IT.ObjectID = @otid
        inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @pt", new { ot = sType, otid, pt = (int)predicateType }, transaction: trans);

                    #endregion

                    #region Asset Field Bulk Copy

                    await cnn.ExecuteAsync(@"
    create table #AssetFieldTable (
        ItemNumber int not null,
        FieldName nvarchar(250) not null,
        FieldValue nvarchar(max) null,
        FieldTypeID int null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetFieldTable ON #AssetFieldTable ( ItemNumber ASC ) INCLUDE ( FieldTypeID )", transaction: trans);

                    var assetFieldBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    assetFieldBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetFieldBulkCopy.DestinationTableName = "#AssetFieldTable";
                    assetFieldBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    assetBulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                    assetBulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                    assetFieldBulkCopy.WriteToServer(assetFieldTable);

                    #endregion

                    await cnn.ExecuteAsync($@"create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));", transaction: trans);
                    await cnn.ExecuteAsync(@"CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC )", transaction: trans);

                    var o = ot.ToString().Replace("Type", "");

                    switch (ot)
                    {
                        case SystemObjects.ArtifactType:
                            #region
                            cnn.Execute($@"
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
        insert  (ArtifactTypeID, SourceID, CreatedOn, UpdatedBy, UpdatedOn)
        values  (@id, S.SourceID, getutcdate(), @r, getutcdate())
    output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
    ", new { id = otid, @r = currentResourceID }, transaction: trans, commandTimeout: 1200);
                            break;
                        #endregion
                        case SystemObjects.FusionAttributeType:
                            #region
                            cnn.Execute($@"
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
                            cnn.Execute($@"
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
        insert  (PolicyTypeID, SourceID, UpdatedBy, UpdatedOn)
        values  (@id, S.SourceID, @r, getutcdate())
    output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
    ", new { id = otid, @r = currentResourceID }, transaction: trans, commandTimeout: 1200);
                            break;
                        #endregion
                        case SystemObjects.ReferenceItemType:
                            #region
                            await cnn.ExecuteAsync($@"
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
    ", new { id = otid, @r = currentResourceID }, transaction: trans, commandTimeout: 1200);
                            break;
                        #endregion
                        case SystemObjects.RuleType:
                            #region
                            cnn.Execute($@"
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
        insert  (RuleTypeID, SourceID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
        values  (@id, S.SourceID, @r, getutcdate(), @r, getutcdate())
    output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
    ", new { id = otid, @r = currentResourceID }, transaction: trans, commandTimeout: 1200);
                            break;
                        #endregion
                        case SystemObjects.TaxonomyType:
                            #region
                            cnn.Execute($@"
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
        insert  (TaxonomyTypeID, SourceID, UpdatedBy, UpdatedOn)
        values  (@id, S.SourceID, @r, getutcdate())
    output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;
    ", new { id = otid, @r = currentResourceID }, transaction: trans, commandTimeout: 1200);
                            break;
                            #endregion
                    }

                    cnn.Execute($@"
    update  T
    set     T.Object = @o,
            T.ObjectID = S.ID,
            T.IsNew = case when S.[Action] = 'INSERT' then 1 else 0 end,
            T.Success = case when S.ID is not null then 1 else 0 end
    from    #AssetTable T
            left join #ObjectMergeTableResult S on S.ItemNumber = T.ItemNumber;
    ", new { id = otid, @r = currentResourceID, o }, transaction: trans, commandTimeout: 1200);

                    #region Deal with parent relationship if required

                    await cnn.ExecuteAsync($@"
merge into  [Intersect] T
using       (
            select  IntersectTypeID,
                    Object as Subject, 
                    ParentID as SubjectID, 
                    Object as Object, 
                    ObjectID as ObjectID
            from    #AssetTable 
            where   ParentID is not null 
                    and ObjectID is not null 
                    and IntersectTypeID is not null
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
    values  (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, @r, @r);
", new { @r = currentResourceID }, transaction: trans, commandTimeout: 1200);

                    #endregion

                    #region Update the asset field temp table with the proper FieldTypeID

                    await cnn.ExecuteAsync(@"
    update  T
    set     T.FieldTypeID = S.ID
    from    #AssetFieldTable T
            inner join FieldType S on S.Object = @ot and S.ObjectID = @otid and S.Name = T.FieldName
    ", new { otid, ot = ot.ToString() }, transaction: trans, commandTimeout: 1200);

                    #endregion

                    #region Merge into the Field table

                    await cnn.ExecuteAsync(@"
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
                T.FormattedValue = S.FieldValue
    when not matched by target then
        insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
        values  (S.FieldTypeID, S.Object, S.ObjectID, S.FieldValue, S.FieldValue);
    ", new { id = otid }, transaction: trans, commandTimeout: 1200);
                    
                    await cnn.ExecuteAsync(@"
    merge into  Field T
    using       (
                select  
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
                        cross apply  [dbo].[FieldLookupValueByFieldTypeID](FT.ID) LV
				where LV.Text = F.FieldValue
                ) S
    on          (
                    T.FieldTypeID = S.FieldTypeID and 
                    T.ObjectType = S.Object and 
                    T.ObjectID = S.ObjectID 
                )
    when matched then
        update set
                T.Value = S.Value,
                T.FormattedValue = S.Value
    when not matched by target then
        insert  (FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
        values  (S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.Value);
    ", new { id = otid }, transaction: trans, commandTimeout: 1200);

                    #endregion

                    retResults = (await cnn.QueryAsync<DatabaseBulkAssetResult>("select ast.ItemNumber, ast.ObjectID, ast.SourceID, ast.Message, ast.Success, ast.IsNew, a.uid from #AssetTable ast inner join Asset a on ast.SourceID = a.SourceID", transaction: trans)).ToList();
                    trans.Commit();

                    results.ForEach(air =>
                    {
                        var dbr = retResults.SingleOrDefault(i => i.ItemNumber == air.ItemNumber);
                        if (dbr != null)
                        {                            
                            air.IsNew = dbr.IsNew;
                            air.Success = dbr.Success;
                            air.uid = dbr.uid;
                            air.Message = dbr.Success ? (dbr.IsNew ? "Created" : "Updated") : $"Failed: {dbr.Message}";
                        }
                    });
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }

            cnn.Close();

            return results;

            #endregion
        }

        public static List<dynamic> BulkRelationshipsImport(this SqlConnection cnn, int currentResourceID, List<RelationshipImportRequest> import)
        {
            var relationshipTable = new System.Data.DataTable();

            relationshipTable.Columns.Add("ItemNumber", typeof(int));
            relationshipTable.Columns.Add("SubjectSourceID", typeof(string));
            relationshipTable.Columns.Add("ObjectSourceID", typeof(string));
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
                row["IntersectTypeID"] = model.IntersectTypeID;

                relationshipTable.Rows.Add(row);
            }

            #endregion

            List<dynamic> retResults = null;

            #region

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            using (var trans = cnn.BeginTransaction())
            {
                try
                {
                    cnn.Execute("DROP TABLE IF EXISTS api.ExecutionRelationship", transaction: trans);
                    
                    #region Asset Bulk Copy

                    cnn.Execute(@"
    create table api.ExecutionRelationship (
        ItemNumber int not null,
        SubjectSourceID nvarchar(1000) null,
        ObjectSourceID nvarchar(1000) null,
        IntersectTypeID int null,
        Message nvarchar(2500) null,
        Success bit null,
        IntersectID int null,
        IsNew bit null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempRelationshipTable ON api.ExecutionRelationship ( IntersectTypeID ASC, SubjectSourceID ASC, ObjectSourceID ASC ) INCLUDE ( ItemNumber )", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    assetBulkCopy.BatchSize = relationshipTable.Rows.Count;
                    assetBulkCopy.DestinationTableName = "api.ExecutionRelationship";
                    assetBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("SubjectSourceID", "SubjectSourceID");
                    assetBulkCopy.ColumnMappings.Add("ObjectSourceID", "ObjectSourceID");
                    assetBulkCopy.ColumnMappings.Add("IntersectTypeID", "IntersectTypeID");
                    assetBulkCopy.ColumnMappings.Add("Message", "Message");
                    assetBulkCopy.ColumnMappings.Add("Success", "Success");
                    assetBulkCopy.ColumnMappings.Add("IntersectID", "IntersectID");
                    assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                    assetBulkCopy.WriteToServer(relationshipTable);

                    #endregion
                                        
                    #region Merge into Intersect

                    cnn.Execute($@"
insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
    (select  distinct
            IT.ID as IntersectTypeID,
		    S.Object as Subject,
		    S.ObjectID as SubjectID,
		    T.Object,
		    T.ObjectID,
            @r, @r
    from    api.ExecutionRelationship R
		    inner join Asset S on S.SourceID = R.SubjectSourceID
		    inner join AssetType ST on ST.ID = S.AssetTypeID

		    inner join Asset T on T.SourceID = R.ObjectSourceID
		    inner join AssetType TT on TT.ID = T.AssetTypeID

		    inner join IntersectTypeDetail	IT on IT.Subject = ST.Object and IT.SubjectID = ST.ObjectID and 
										    IT.Object = TT.Object and IT.ObjectID = TT.ObjectID and
										    IT.ID = R.IntersectTypeID
            left join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = S.Object and I.SubjectID = S.ObjectID and I.Object = T.Object and I.ObjectID = T.ObjectID
    where   I.ID is null)
    union
    (select  distinct
            IT.ID as IntersectTypeID,
		    S.Object as Subject,
		    S.ObjectID as SubjectID,
		    T.Object,
		    T.ObjectID,
            @r, @r
    from    api.ExecutionRelationship R
		    inner join AssetType S on S.Uid = R.SubjectSourceID		    

		    inner join Asset T on T.SourceID = R.ObjectSourceID
		    inner join AssetType TT on TT.ID = T.AssetTypeID

		    inner join IntersectTypeDetail	IT on IT.Subject = S.Object and IT.SubjectID = 0 and 
										    IT.Object = TT.Object and IT.ObjectID = TT.ObjectID and
										    IT.ID = R.IntersectTypeID
            left join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = S.Object and I.SubjectID = S.ObjectID and I.Object = T.Object and I.ObjectID = T.ObjectID
    where   I.ID is null)
    union
    (select  distinct
            IT.ID as IntersectTypeID,
		    S.Object as Subject,
		    S.ObjectID as SubjectID,
		    T.Object,
		    T.ObjectID,
            @r, @r
    from    api.ExecutionRelationship R
		    inner join Asset S on S.SourceID = R.SubjectSourceID
		    inner join AssetType ST on ST.ID = S.AssetTypeID

		    inner join AssetType T on T.Uid = R.ObjectSourceID		    

		    inner join IntersectTypeDetail	IT on IT.Subject = ST.Object and IT.SubjectID = ST.ObjectID and 
										    IT.Object = T.Object and IT.ObjectID = 0 and
										    IT.ID = R.IntersectTypeID
            left join [Intersect] I on I.IntersectTypeID = IT.ID and I.Subject = S.Object and I.SubjectID = S.ObjectID and I.Object = T.Object and I.ObjectID = T.ObjectID
    where   I.ID is null)
;

    update  T
    set     T.IntersectID = I.ID,
            T.Success = case when I.ID is null then cast(0 as bit) else cast(1 as bit) end
    from    api.ExecutionRelationship T
            left join Asset S on S.SourceID = T.SubjectSourceID
            left join Asset O on O.SourceID = T.ObjectSourceID
            left join [Intersect] I on T.IntersectTypeID = I.IntersectTypeID 
                    and I.Subject = S.Object 
                    and I.SubjectID = S.ObjectID 
                    and I.Object = O.Object 
                    and I.ObjectID = O.ObjectID;

    update  T
    set     T.IntersectID = I.ID,
            T.Success = case when I.ID is null then cast(0 as bit) else cast(1 as bit) end
    from    api.ExecutionRelationship T
            inner join Asset S on S.SourceID = T.SubjectSourceID
            inner join AssetType O on O.Uid = T.ObjectSourceID
            inner join [Intersect] I on T.IntersectTypeID = I.IntersectTypeID 
                    and I.Subject = S.Object 
                    and I.SubjectID = S.ObjectID 
                    and I.Object = O.Object 
                    and I.ObjectID = O.ObjectID

    update  T
    set     T.IntersectID = I.ID,
            T.Success = case when I.ID is null then cast(0 as bit) else cast(1 as bit) end
    from    api.ExecutionRelationship T
            inner join AssetType S on S.Uid = T.SubjectSourceID
            inner join Asset O on O.SourceID = T.ObjectSourceID
            inner join [Intersect] I on T.IntersectTypeID = I.IntersectTypeID 
                    and I.Subject = S.Object 
                    and I.SubjectID = S.ObjectID 
                    and I.Object = O.Object 
                    and I.ObjectID = O.ObjectID;


",
                new { @r = currentResourceID }, transaction: trans, commandTimeout: 1200);

                    #endregion

                    retResults = cnn.Query<dynamic>("select * from api.ExecutionRelationship", transaction: trans).ToList();

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }

            cnn.Close();

            #endregion

            return retResults;
        }

        #endregion API v1 logic

        #region API v2 logic

        internal class FieldTypeIdModel
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        static List<FieldType> GetEditableFieldTypesByAssetType(this SqlConnection cnn, AssetType at)
        {
            return cnn.Query<FieldType>("select * from FieldType where AssetTypeID = @atId and [Type] not in ('Hidden', 'Color', 'FusionLookup', 'Attribute', 'FilteredLookup', 'ComplexRelationLookup', 'OwnershipLookup', 'FieldFromRelationship', 'RefListRelationship')", new { atId = at.ID }).ToList();
        }

        static void SendWorkflowEvents(this SqlConnection cnn, 
            IQueueSource queue, string companyUrlPrefix, int currentCompanyID, int currentResourceID, 
            AssetType at, List<DatabaseBulkAssetResult> results)
        {
            try
            {
                var events = new List<EventInfo>();
                var objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), at.Object);
                foreach (var result in results)
                {
                    var changeType = result.IsNew ? ChangeType.Add : ChangeType.Update;

                    events.Add(new EventInfo
                    {
                        CompanyID = currentCompanyID,
                        DomainPrefix = companyUrlPrefix,
                        ResourceID = currentResourceID,
                        Action = changeType,
                        Object = new EventObjectInfo
                        {
                            Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), result.Object),
                            ObjectType = objectType,
                            ObjectID = result.ObjectID,
                            ObjectTypeID = at.ObjectID
                        }
                    });

                    if (events.Count > 50)
                    {
                        queue.CreateTopicMessages(events);
                        events.Clear();
                    }
                }

                if (events.Count > 0)
                {
                    queue.CreateTopicMessages(events);
                    events.Clear();
                }
            }
            catch (Exception)
            {

            }
        }

        public static List<DatabaseBulkAssetResult> UpsertAssets(this SqlConnection cnn,
            IQueueSource queue,
            string companyUrlPrefix,
            int currentCompanyID,
            int currentResourceID,
            AssetType at,
            IEnumerable<IAssetUpsert> import, 
            bool isInsert,
            int timeout = 3600)
        {
            #region Build data tables for bulk load.

            var assetTable = new System.Data.DataTable();
            assetTable.Columns.Add("ItemNumber", typeof(int));
            assetTable.Columns.Add("Message", typeof(string));
            assetTable.Columns.Add("Success", typeof(bool));
            assetTable.Columns.Add("Uid", typeof(Guid));
            assetTable.Columns.Add("ParentUid", typeof(Guid));

            var assetFieldTable = new System.Data.DataTable();
            assetFieldTable.Columns.Add("ItemNumber", typeof(int));
            assetFieldTable.Columns.Add("FieldName", typeof(string));
            assetFieldTable.Columns.Add("FieldValue", typeof(string));
            assetFieldTable.Columns.Add("FieldTypeID", typeof(int));

            #endregion

            var results = new List<DatabaseBulkAssetResult>();

            var fieldTypes = cnn.GetEditableFieldTypesByAssetType(at);
            var fieldTypeIDs = new HashSet<FieldTypeIdModel>();
            fieldTypes.ForEach(ft => {
                fieldTypeIDs.Add(new FieldTypeIdModel { ID = ft.ID, Name = ft.Name });
            });

            int dbLimit = 5000;
            int currentCount = 0;

            #region Generate data sets

            int i = 1;
            foreach(var model in import)
            {
                var row = assetTable.NewRow();

                row["ItemNumber"] = i;

                if (model.Uid != Guid.Empty && !isInsert)
                    row["Uid"] = model.Uid;

                if (model.ParentUid.HasValue && isInsert)
                    row["ParentUid"] = model.ParentUid.Value;

                var usedFields = new HashSet<string>();
                bool assetIsValid = true;

                #region Model-level validation

                if (isInsert)
                {
                    if (model.Uid != Guid.Empty)
                    {
                        row["Message"] += $"You may not provide a Uid for this asset when you are attempting to add it. ";
                        assetIsValid = false;
                    }
                }
                else
                {
                    if (model.Uid == Guid.Empty)
                    {
                        row["Message"] += $"You must provide a valid Uid for this asset when you are attempting to update it. ";
                        assetIsValid = false;
                    }

                    if (model.ParentUid != Guid.Empty && model.ParentUid.HasValue)
                    {
                        row["Message"] += $"You may not provide a Parent Uid for this asset when you are attempting to update it. ";
                        assetIsValid = false;
                    }
                }

                if (assetIsValid)
                {
                    if (at.Class == AssetTypeClass.Reference && !model.Fields.ContainsKey("Code"))
                    {
                        row["Message"] += $"You must provide a Code in order to ${(isInsert ? "add" : "update")} this reference list item. ";
                        assetIsValid = false;
                    }

                    if (at.Class == AssetTypeClass.FusionAttribute && (!model.Fields.ContainsKey("FusionID") || !model.Fields.ContainsKey("Name")))
                    {
                        if (!model.Fields.ContainsKey("FusionID"))
                        {
                            row["Message"] += $"You must provide a FusionID in order to ${(isInsert ? "add" : "update")} this technical asset. ";
                            assetIsValid = false;
                        }
                        if (!model.Fields.ContainsKey("Name"))
                        {
                            row["Message"] += $"You must provide a Name in order to ${(isInsert ? "add" : "update")} this technical asset. ";
                            assetIsValid = false;
                        }
                    }
                }

                #endregion

                if (assetIsValid)
                {
                    foreach (var k in model.Fields.Keys)
                    {
                        // Checks for duplicate fields and only add uniques.
                        if (!usedFields.Any(u => u == k) && assetIsValid)
                        {
                            usedFields.Add(k);

                            var fieldRow = assetFieldTable.NewRow();

                            fieldRow["ItemNumber"] = i;
                            fieldRow["FieldName"] = k.Trim();
                            fieldRow["FieldValue"] = (model.Fields[k] + "").Trim();
                            if (fieldTypeIDs.Any(ft => ft.Name == k))
                            {
                                fieldRow["FieldTypeID"] = fieldTypeIDs.Single(ft => ft.Name == k).ID;
                                assetFieldTable.Rows.Add(fieldRow);
                            }
                            else if (at.Class == AssetTypeClass.Reference && k == "Code")
                            {
                                assetFieldTable.Rows.Add(fieldRow);
                            }
                            else if (at.Class == AssetTypeClass.FusionAttribute && ((k == "Name") || (k == "FusionID")))
                            {
                                assetFieldTable.Rows.Add(fieldRow);
                            }
                            else
                            {
                                row["Message"] += $"Field [{k}] is not valid for this asset. ";
                                assetIsValid = false;
                            }
                        }
                    }
                }

                if (assetIsValid)
                {
                    assetTable.Rows.Add(row);
                    currentCount++;
                }
                else
                {
                    results.Add(new DatabaseBulkAssetResult { ItemNumber = i, Message = row["Message"].ToString(), Success = false, IsNew = false });
                }

                // If we reached limit, have the database process what we have so far.
                if (currentCount >= dbLimit)
                {
                    results.AddRange(
                        cnn.UpsertAssetsToDatabase(currentResourceID, isInsert, at, assetTable, assetFieldTable, timeout)
                    );
                    assetTable.Rows.Clear();
                    assetFieldTable.Rows.Clear();
                    currentCount = 0;
                }

                i++;
            }

            #endregion

            // Now deal with any remaining items.
            if (currentCount > 0)
            {
                results.AddRange(
                    cnn.UpsertAssetsToDatabase(currentResourceID, isInsert, at, assetTable, assetFieldTable, timeout)
                );
                currentCount = 0;
            }

            cnn.SendWorkflowEvents(queue, companyUrlPrefix, currentCompanyID, currentResourceID, at, results);

            return results;
        }

        static List<DatabaseBulkAssetResult> UpsertAssetsToDatabase(this SqlConnection cnn, 
            int currentResourceID, 
            bool isInsert,
            AssetType at,
            System.Data.DataTable assetTable,
            System.Data.DataTable assetFieldTable,
            int timeout = 3600)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            List<DatabaseBulkAssetResult> results = null;

            using (var trans = cnn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    cnn.Execute("DROP TABLE IF EXISTS #AssetTable", transaction: trans);
                    cnn.Execute("DROP TABLE IF EXISTS #AssetFieldTable", transaction: trans);

                    #region Asset Bulk Copy

                    cnn.Execute(@"
    create table #AssetTable (
        ItemNumber int not null,

        Uid uniqueidentifier null,
        AssetID bigint null,
        Object varchar(50) null,
        ObjectID int null,
        KeyHash varchar(50) null,

        ParentUid uniqueidentifier null,
        ParentObject varchar(50) null,
        ParentObjectID int null,

        [Message] nvarchar(2500) null,
        Success bit null,
        IsNew bit null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetTable_Uid ON #AssetTable ( [Uid] ASC ) INCLUDE ( ItemNumber )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetTable_ParentUid ON #AssetTable ( [ParentUid] ASC ) INCLUDE ( ItemNumber )", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    assetBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetBulkCopy.DestinationTableName = "#AssetTable";
                    assetBulkCopy.BulkCopyTimeout = timeout;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("Uid", "Uid");
                    assetBulkCopy.ColumnMappings.Add("ParentUid", "ParentUid");
                    assetBulkCopy.WriteToServer(assetTable);

                    #endregion

                    #region Asset Field Bulk Copy

                    cnn.Execute(@"
    create table #AssetFieldTable (
        ItemNumber int not null,
        FieldName nvarchar(250) not null,
        FieldValue nvarchar(max) null,
        FieldTypeID int null,
        LookupValue nvarchar(max) null,
        Ignore bit null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempAssetFieldTable ON #AssetFieldTable ( ItemNumber ASC, FieldTypeID ASC )", transaction: trans);
                    
                    var assetFieldBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    assetFieldBulkCopy.BatchSize = assetFieldTable.Rows.Count;
                    assetFieldBulkCopy.DestinationTableName = "#AssetFieldTable";
                    assetFieldBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    assetBulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                    assetBulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                    assetFieldBulkCopy.WriteToServer(assetFieldTable);

                    #endregion

                    cnn.Execute("exec asset.BulkUpsert @isInsert, @uid, @r", new { isInsert, at.uid, r = currentResourceID }, trans, timeout);

                    results = cnn.Query<DatabaseBulkAssetResult>("select * from #AssetTable", transaction: trans).ToList();
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }

            cnn.Close();

            return results;
        }
        
        #endregion API v2 logic
    }
}
