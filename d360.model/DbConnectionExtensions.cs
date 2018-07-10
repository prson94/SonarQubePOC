using d360.core.entities;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace d360.model
{
    public static class DbConnectionExtensions
    {

        /// <summary>
        /// Re-process responsibility rules. By default this will re-process ALL rules unless passing a specific rule ID.
        /// </summary>
        /// <param name="cnn">The SQL connection object</param>
        /// <param name="ruleID">Optionall pass a specific rule by its ID.</param>
        public static void ProcessResponsibilityRelationRules(this SqlConnection cnn, int? ruleID = null)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultFixed);

            using (SqlTransaction trans = cnn.BeginTransaction())
            {
                try
                {
                    #region Create temporary tables

                    cnn.Execute(@"
    drop table if exists #resp;
    create table #resp (
        RuleID int, ResponsibilityTypeID int, 
        AssetID bigint, AssetTypeID int, 
        SecurityAsset char(1), SecurityAssetID int, Context nvarchar(max),
        ApplyToType bit, PermissionsBitMask int, IsVisible bit,
        Overridden bit, OverrideID bigint null
    );
    CREATE CLUSTERED INDEX CIX_Tempresp ON #resp (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, ApplyToType, Overridden, OverrideID);
    ", transaction: trans);

                    cnn.Execute(@"
    DROP TABLE IF EXISTS #ResponsibilityTypeRelationTypeItem;
    create table #ResponsibilityTypeRelationTypeItem (
    RuleID int not null, 
    ResponsibilityTypeID int not null, 
    [SecurityAsset] char(1) not null, 
    [SecurityAssetID] int not null
    );
    CREATE CLUSTERED INDEX CIX_TempResponsibilityTypeRelationTypeItem ON #ResponsibilityTypeRelationTypeItem (RuleID);
    ", transaction: trans);

                    cnn.Execute(@"
    DROP TABLE IF EXISTS #ResponsibilityTypeRelationItem;
    create table #ResponsibilityTypeRelationItem (
    RuleID int not null, 
    ResponsibilityTypeID int not null, 
    AssetID bigint not null, 
    [SecurityAsset] char(1) not null, 
    [SecurityAssetID] int not null
    );
    CREATE CLUSTERED INDEX CIX_TempResponsibilityTypeRelationItem ON #ResponsibilityTypeRelationItem (RuleID, AssetID);
    ", transaction: trans);

                    #endregion

                    List<ResponsibilityTypeRelationRule> rules = null;
                    if (ruleID.HasValue)
                    {
                        rules = cnn.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value }, transaction: trans).ToList();
                    }
                    else
                    {
                        rules = cnn.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule", transaction: trans).ToList();
                    }

                    rules.ForEach(rule =>
                    {
                        rule.SetDefinitionFromRaw();

                        if (rule.ApplyToType)
                        {
                            var results = cnn.GetProcessedResponsibilityRuleTypeResults(rule, trans).ToList();

                            #region Bulk insert the rows above.

                            using (var bulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans))
                            {
                                bulkCopy.BatchSize = results.Count;
                                bulkCopy.DestinationTableName = "#ResponsibilityTypeRelationTypeItem";
                                bulkCopy.BulkCopyTimeout = 3600;

                                var table = new System.Data.DataTable();

                                #region Create column mappings

                                var columnName = "RuleID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "ResponsibilityTypeID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "SecurityAsset";
                                table.Columns.Add(columnName, typeof(string));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "SecurityAssetID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                #endregion

                                foreach (var item in results)
                                {
                                    var row = table.NewRow();

                                    row["RuleID"] = item.RuleID;
                                    row["ResponsibilityTypeID"] = item.ResponsibilityTypeID;
                                    row["SecurityAsset"] = item.SecurityAsset;
                                    row["SecurityAssetID"] = item.SecurityAssetID;

                                    table.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(table);
                            }

                            #endregion

                            results = null;
                        }
                        else
                        {
                            var results = cnn.GetProcessedResponsibilityRuleResults(rule, trans).ToList();

                            #region Bulk insert the rows above.

                            using (var bulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans))
                            {
                                bulkCopy.BatchSize = results.Count;
                                bulkCopy.DestinationTableName = "#ResponsibilityTypeRelationItem";
                                bulkCopy.BulkCopyTimeout = 3600;

                                var table = new System.Data.DataTable();

                                #region Create column mappings

                                var columnName = "RuleID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "ResponsibilityTypeID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "AssetID";
                                table.Columns.Add(columnName, typeof(long));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "SecurityAsset";
                                table.Columns.Add(columnName, typeof(string));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "SecurityAssetID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                #endregion

                                foreach (var item in results)
                                {
                                    var row = table.NewRow();

                                    row["RuleID"] = item.RuleID;
                                    row["ResponsibilityTypeID"] = item.ResponsibilityTypeID;
                                    row["AssetID"] = item.AssetID;
                                    row["SecurityAsset"] = item.SecurityAsset;
                                    row["SecurityAssetID"] = item.SecurityAssetID;

                                    table.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(table);
                            }

                            #endregion

                            results = null;
                        }
                    });

                    #region Insert item assignments into #resp table.

                            cnn.Execute(@"
    insert into #resp
	    select	R.ID as RuleID,
			    R.ResponsibilityTypeID,
			    A.ID as AssetID,
			    A.AssetTypeID,
			    I.SecurityAsset,
			    I.SecurityAssetID,
			    R.Context,
			    R.ApplyToType,
			    REL.PermissionsBitMask,
			    R.IsVisible, 
			    cast(0 as bit) as Overridden, 
			    0 as OverrideID 
	    from	Asset A
			    inner join AssetType T on T.ID = A.AssetTypeID
			    inner join ResponsibilityTypeRelationRule R on R.ApplyToType = 0 and R.Object = T.Object and R.ObjectID = T.ObjectID
			    inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID
			    inner join #ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID", transaction: trans, commandTimeout: 7200);

                    #endregion

                    #region Update override columns

                    cnn.Execute(@"
    update	T
    set		T.Overridden = 1,
		    T.OverrideID = S.ID
    from	#resp T
		    inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID", transaction: trans, commandTimeout: 7200);

                    #endregion

                    #region Insert type assignments into #resp table.

                    cnn.Execute(@"
    insert into #resp
	    select	R.ID as RuleID,
			    R.ResponsibilityTypeID,
			    0 as AssetID,
			    T.ID as AssetTypeID,
			    I.SecurityAsset,
			    I.SecurityAssetID,
			    R.Context,
			    R.ApplyToType,
			    REL.PermissionsBitMask,
			    R.IsVisible,
			    cast(0 as bit) as Overridden,
			    0 as OverrideID 
	    from	AssetType T
			    inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
			    inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID
			    inner join #ResponsibilityTypeRelationTypeItem I on I.RuleID = R.ID and R.ApplyToType = 1", transaction: trans, commandTimeout: 7200);

                    #endregion

                    #region Merge final results into ResponsibilityTypeRelationRuleResult table

                    cnn.Execute(@"
    merge	ResponsibilityTypeRelationRuleResult as T
    using	(
		    select	distinct
				    *
		    from	#resp
		    ) as S
    on		(
		    S.RuleID = T.RuleID
		    and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
		    and S.AssetID = T.AssetID 
		    and S.AssetTypeID = T.AssetTypeID 
		    and S.SecurityAsset = T.SecurityAsset 
		    and S.SecurityAssetID = T.SecurityAssetID 
		    and S.ApplyToType = T.ApplyToType
		    and S.Overridden = T.Overridden
		    and S.OverrideID = T.OverrideID
		    )
    when	not matched by source" + ((ruleID.HasValue) ? $" and T.RuleID = {ruleID.Value}" : "") + @" then
		    delete
    when	matched and (
					    T.Context <> S.Context 
					    or (T.Context is null and S.Context is not null)
					    or (T.Context is not null and S.Context is null)
					    or T.PermissionsBitMask <> S.PermissionsBitMask 
					    or T.IsVisible <> S.IsVisible
					    ) then
    update	set
		    T.Context = S.Context,
		    T.PermissionsBitMask = S.PermissionsBitMask,
		    T.IsVisible = S.IsVisible
    when	not matched by target then
    insert	(RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, PermissionsBitMask, IsVisible, Overridden, OverrideID)
    values	(S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.AssetTypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, S.ApplyToType, S.PermissionsBitMask, S.IsVisible, S.Overridden, S.OverrideID);", 
                    transaction: trans, commandTimeout: 7200);

                    #endregion

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public static void RemoveRelationRuleResultsByRule(this DbConnection cnn, int ruleID)
        {
            cnn.Execute("delete ResponsibilityTypeRelationRuleResult where RuleID <> 0 and RuleID = ruleID", commandTimeout: 7200);
        }

        public static void ClearInvalidRelationRuleResults(this DbConnection cnn)
        {
            cnn.Execute("delete ResponsibilityTypeRelationRuleResult where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
            cnn.Execute("delete ResponsibilityTypeRelationRuleResult where RuleID = 0 and OverrideID not in (select ID from ResponsibilityTypeRelationOverrideItem)", commandTimeout: 7200);
            cnn.Execute("update ResponsibilityTypeRelationRuleResult set Overridden = 0, OverrideID = null where RuleID <> 0 and Overridden = 1 and OverrideID not in (select ID from ResponsibilityTypeRelationOverrideItem)", commandTimeout: 7200);
        }


        #region Responsibility Rule Generation

        private static string GetWhenResultsSql(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string whenSql = "";

            whenSql = $@"
select	A.ID as AssetID, utility.GetAssetDisplayValueWrapper(A.ID) as Name 
from	Asset A 
		inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID} ";
            var fCount = 1;
            var rCount = 1;
            if (rule.StructuredDefinition != null && rule.StructuredDefinition.When != null)
            {
                rule.StructuredDefinition.When.ForEach(w => {
                    if (w.CheckType == "F")
                    {
                        if (w.FieldTypeID > 0)
                        {
                            whenSql += $@"inner join FieldDetail F{fCount} on F{fCount}.Object = A.Object and F{fCount}.ObjectID = A.ObjectID and F{fCount}.FieldTypeID = {w.FieldTypeID} and F{fCount}.Value = '{w.Value}' ";
                        }
                        else
                        {
                            //something else here, static field
                        }
                        fCount++;
                    }
                    if (w.CheckType == "R")
                    {
                        whenSql += $@"inner join [Intersect] I{rCount} on 
        I{rCount}.IntersectTypeID = {w.IntersectTypeID} and 
        ( 
        (I{rCount}.Subject = A.Object and I{rCount}.SubjectID = A.ObjectID and I{rCount}.Object = '{w.TargetObject}' and I{rCount}.ObjectID = {w.TargetObjectID}) OR 
        (I{rCount}.Object = A.Object and I{rCount}.ObjectID = A.ObjectID and I{rCount}.Subject = '{w.TargetObject}' and I{rCount}.SubjectID = {w.TargetObjectID}) 
        ) ";
                        rCount++;
                    }
                });
            }

            return whenSql;
        }

        public static IEnumerable<ObjectResult> GetWhenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, SqlTransaction trans = null)
        {
            string sql = cnn.GetWhenResultsSql(rule);
            return cnn.Query<ObjectResult>(sql, transaction: trans, commandTimeout: 7200);
        }

        private static string GetThenResultsSql(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string thenSql = "";

            int tCount = 1;
            string whenSuffix = "";
            string obj = "";
            string uniqueIdField = "ID";

            if ((rule.StructuredDefinition != null) && (rule.StructuredDefinition.Then != null))
            {
                if (rule.StructuredDefinition.Then.Object == "OrganizationType")
                {
                    obj = "Organization";

                    thenSql = $@"
select	'O' as SecurityAsset,
        O.ID as SecurityAssetID,
		O.Name
from	Organization O ";
                }

                if (rule.StructuredDefinition.Then.Object == "GroupType")
                {
                    obj = "Group";

                    thenSql = $@"
select	'G' as SecurityAsset,
        O.ID as SecurityAssetID,
        O.Name
from	[Group] O ";
                }

                if (rule.StructuredDefinition.Then.Object == "ResourceType")
                {
                    obj = "Resource";
                    uniqueIdField = "ResourceID";

                    thenSql = $@"
select	'R' as SecurityAsset,
        O.ResourceID as SecurityAssetID,
		O.FirstName + ' ' + O.LastName as Name
from	reporting.Global_Resource O ";
                }

                if (rule.StructuredDefinition.Then.Conditions != null)
                {
                    rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                    {
                        if (rc.FieldTypeID > 0)
                        {
                            thenSql += $"inner join FieldDetail F{tCount} on F{tCount}.Object = '{obj}' and F{tCount}.ObjectID = O.{uniqueIdField} and F{tCount}.FieldTypeID = {rc.FieldTypeID} and F{tCount}.Value = '{rc.Value}' ";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(rc.FieldTypeName))
                            {
                                if (rc.FieldTypeName == "Name")
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.{uniqueIdField} = {rc.Value}";
                                }
                                else
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.{rc.FieldTypeName} = '{rc.Value}'";
                                }
                            }
                        }

                        tCount++;
                    });
                }

                if (obj == "Resource")
                {
                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.Status = 'Active'";
                }
            }

            thenSql += whenSuffix;

            return thenSql;
        }

        public static IEnumerable<SecurityResult> GetThenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, SqlTransaction trans = null)
        {
            string sql = cnn.GetThenResultsSql(rule);
            return (string.IsNullOrEmpty(sql)) ?
                new List<SecurityResult>().AsEnumerable() :
                cnn.Query<SecurityResult>(sql, transaction: trans, commandTimeout: 7200);
        }

        public static int UpdateFieldMove(this DbConnection cnn, FieldType toField,FieldType fromField ,int currentResourceID)
        {
            string updateSql = $" Update fieldtype set ColumnOrder ={toField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={toField.ID};";
            if (fromField!=null)
            updateSql += $" Update fieldtype set ColumnOrder ={fromField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={fromField.ID};";
            return cnn.Execute(updateSql);
        }
        public static IEnumerable<EndResult> GetProcessedResponsibilityRuleResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            if (rule.StructuredDefinition == null)
            {
                rule.SetDefinitionFromRaw();
            }

            var oResults = cnn.GetWhenResults(rule, trans);
            var sResults = cnn.GetThenResults(rule, trans);

            return 
                from o in oResults
                join s in sResults on 1 equals 1
                select new EndResult
                {
                    RuleID = rule.ID,
                    ResponsibilityTypeID = rule.ResponsibilityTypeID,
                    AssetID = o.AssetID,
                    SecurityAsset = s.SecurityAsset,
                    SecurityAssetID = s.SecurityAssetID
                };
        }

        /// <summary>
        /// Process and save results for a single rule.
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="rule"></param>
        public static void ProcessAndSaveResponsibilityRuleResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, bool useTransaction = true)
        {
            var results = cnn.GetProcessedResponsibilityRuleResults(rule).ToList();
            ((SqlConnection)cnn).SaveResponsibilityRuleResults(results, useTransaction, rule.ID);
        }

        public static IEnumerable<EndTypeResult> GetProcessedResponsibilityRuleTypeResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, SqlTransaction trans = null)
        {
            if (rule.StructuredDefinition == null)
            {
                rule.SetDefinitionFromRaw();
            }

            var sResults = cnn.GetThenResults(rule, trans);

            return
                from s in sResults
                select new EndTypeResult
                {
                    RuleID = rule.ID,
                    ResponsibilityTypeID = rule.ResponsibilityTypeID,
                    SecurityAsset = s.SecurityAsset,
                    SecurityAssetID = s.SecurityAssetID
                };
        }

        /// <summary>
        /// Process and save type results for a single rule.
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="rule"></param>
        public static void ProcessAndSaveResponsibilityRuleTypeResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, bool useTransaction = true)
        {
            var results = cnn.GetProcessedResponsibilityRuleTypeResults(rule).ToList();
            //((SqlConnection)cnn).SaveResponsibilityRuleTypeResults(results, useTransaction, rule.ID);
        }

        /// <summary>
        /// Save results to temp table via bulk insert
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public static void SaveResponsibilityRuleResults(this SqlConnection cnn, List<EndResult> results, bool useTransaction = true, int? ruleID = null)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.Open();

            SqlTransaction trans = null;
            if (useTransaction)
                trans = cnn.BeginTransaction();

            cnn.Execute("DROP TABLE IF EXISTS #ResponsibilityTypeRelationItem", transaction: trans);

            cnn.Execute(@"
set nocount on 
create table #ResponsibilityTypeRelationItem (
RuleID int not null, 
ResponsibilityTypeID int not null, 
AssetID bigint not null, 
[SecurityAsset] char(1) not null, 
[SecurityAssetID] int not null
)
set nocount off", commandTimeout: 3600, transaction: trans);

            #region Bulk insert the rows above.

            using (var bulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans))
            {
                bulkCopy.BatchSize = results.Count;
                bulkCopy.DestinationTableName = "#ResponsibilityTypeRelationItem";
                bulkCopy.BulkCopyTimeout = 3600;

                var table = new System.Data.DataTable();

                #region Create column mappings

                var columnName = "RuleID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "ResponsibilityTypeID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "AssetID";
                table.Columns.Add(columnName, typeof(long));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "SecurityAsset";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "SecurityAssetID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                #endregion

                foreach (var item in results)
                {
                    var row = table.NewRow();

                    row["RuleID"] = item.RuleID;
                    row["ResponsibilityTypeID"] = item.ResponsibilityTypeID;
                    row["AssetID"] = item.AssetID;
                    row["SecurityAsset"] = item.SecurityAsset;
                    row["SecurityAssetID"] = item.SecurityAssetID;

                    table.Rows.Add(row);
                }

                bulkCopy.WriteToServer(table);
            }

            #endregion

            #region  Merge the raw data you compiled above into the item table. These are rule results.

            if (ruleID.HasValue)
            {
                cnn.Execute(@"
merge   ResponsibilityTypeRelationItem as T 
using   ( 
    select  *
    from    #ResponsibilityTypeRelationItem
    ) as S 
    on  (
        T.RuleID = S.RuleID 
        and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
        and T.[AssetID] = S.[AssetID] 
        and T.[SecurityAsset] = S.[SecurityAsset] 
        and T.[SecurityAssetID] = S.[SecurityAssetID] 
        )
when    not matched by source and T.RuleID > 0 and T.RuleID = @r then 
    delete
when    not matched by target then 
    insert (RuleID, ResponsibilityTypeID, [AssetID], SecurityAsset, SecurityAssetID) 
    values (S.RuleID, S.ResponsibilityTypeID, S.[AssetID], S.SecurityAsset, S.SecurityAssetID);", new { r = ruleID.Value },
commandTimeout: 3600, transaction: trans);
            }
            else
            {
                cnn.Execute(@"
merge   ResponsibilityTypeRelationItem as T 
using   ( 
    select  *
    from    #ResponsibilityTypeRelationItem
    ) as S 
    on  (
        T.RuleID = S.RuleID 
        and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
        and T.[AssetID] = S.[AssetID] 
        and T.[SecurityAsset] = S.[SecurityAsset] 
        and T.[SecurityAssetID] = S.[SecurityAssetID] 
        )
when    not matched by source and T.RuleID > 0 then 
    delete
when    not matched by target then 
    insert (RuleID, ResponsibilityTypeID, [AssetID], SecurityAsset, SecurityAssetID) 
    values (S.RuleID, S.ResponsibilityTypeID, S.[AssetID], S.SecurityAsset, S.SecurityAssetID);",
commandTimeout: 3600, transaction: trans);
            }

            #endregion

            if (useTransaction)
                trans.Commit();
        }

        #endregion
    }
}
