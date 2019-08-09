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
    }
}
