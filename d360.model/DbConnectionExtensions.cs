using d360.core.entities;
using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace d360.model
{
    public static class DbConnectionExtensions
    {
        public static IEnumerable<ObjectResult> GetWhenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string whenSql = "";

            #region WhenSql

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

            return cnn.Query<ObjectResult>(whenSql, commandTimeout: 7200);

            #endregion
        }

        public static IEnumerable<SecurityResult> GetThenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string thenSql = "";

            int tCount = 1;
            string whenSuffix = "";
            string obj = "";
            string uniqueIdField = "ID";

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

            thenSql += whenSuffix;

            return cnn.Query<SecurityResult>(thenSql, commandTimeout: 7200);
        }

        public static IEnumerable<EndResult> GetProcessedResponsibilityRuleResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            if (rule.StructuredDefinition == null)
            {
                rule.SetDefinitionFromRaw();
            }

            var oResults = cnn.GetWhenResults(rule);
            var sResults = cnn.GetThenResults(rule);

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

        public static IEnumerable<EndTypeResult> GetProcessedResponsibilityRuleTypeResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            if (rule.StructuredDefinition == null)
            {
                rule.SetDefinitionFromRaw();
            }

            var sResults = cnn.GetThenResults(rule);

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
            ((SqlConnection)cnn).SaveResponsibilityRuleTypeResults(results, useTransaction, rule.ID);
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

            //using (var trans = cnn.BeginTransaction())
            //{
                cnn.Execute(@"

IF OBJECT_ID('tempdb..#ResponsibilityTypeRelationItem') IS NOT NULL
			DROP TABLE #ResponsibilityTypeRelationItem;


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

            #region Merge the overrides into the item table. These are override items.

            //I think this is quite dangerous. Removing for now.
            //                cnn.Execute(@"
            //merge   ResponsibilityTypeRelationItem as T 
            //using   ( 
            //        select  *
            //        from    ResponsibilityTypeRelationOverrideItem
            //        ) as S 
            //        on  (
            //            T.RuleID = 0
            //            and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
            //            and T.[AssetID] = S.[AssetID] 
            //            and T.[SecurityAsset] = S.[SecurityAsset] 
            //            and T.[SecurityAssetID] = S.[SecurityAssetID] 
            //            )
            //when    not matched by source and T.RuleID = 0 then 
            //        delete
            //when    not matched by target then 
            //        insert (RuleID, ResponsibilityTypeID, [AssetID], SecurityAsset, SecurityAssetID, OverrideItemID) 
            //        values (0, S.ResponsibilityTypeID, S.[AssetID], S.SecurityAsset, S.SecurityAssetID, S.ID);",
            //commandTimeout: 3600, transaction: trans);

            #endregion

            #region Mark the overriden items generated from rules with overrides we loaded above.

            //I think this is quite dangerous. Removing for now.
            //                cnn.Execute(@"
            //update	T
            //set		T.Overriden = 1
            //from	ResponsibilityTypeRelationItem T
            //		inner join ResponsibilityTypeRelationItem S on S.RuleID = 0 and T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 0;",
            //commandTimeout: 3600, transaction: trans);

            #endregion

            if (useTransaction)
                trans.Commit();
            //}
        }

        /// <summary>
        /// Save type results to temp table via bulk insert
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public static void SaveResponsibilityRuleTypeResults(this SqlConnection cnn, List<EndTypeResult> results, bool useTransaction = true, int? ruleID = null)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.Open();

            SqlTransaction trans = null;
            if (useTransaction)
                trans = cnn.BeginTransaction();

            cnn.Execute(@"
set nocount on 
create table #ResponsibilityTypeRelationTypeItem (
RuleID int not null, 
ResponsibilityTypeID int not null, 
[SecurityAsset] char(1) not null, 
[SecurityAssetID] int not null
)
set nocount off", commandTimeout: 3600, transaction: trans);

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

            #region  Merge the raw data you compiled above into the item table. These are rule results.

            if (ruleID.HasValue)
            {
                cnn.Execute(@"
merge   ResponsibilityTypeRelationTypeItem as T 
using   ( 
        select  *
        from    #ResponsibilityTypeRelationTypeItem
        ) as S 
        on  (
            T.RuleID = S.RuleID 
            and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
            and T.[SecurityAsset] = S.[SecurityAsset] 
            and T.[SecurityAssetID] = S.[SecurityAssetID] 
            )
when    not matched by source and T.RuleID > 0 and T.RuleID = @r then 
        delete
when    not matched by target then 
        insert (RuleID, ResponsibilityTypeID, SecurityAsset, SecurityAssetID) 
        values (S.RuleID, S.ResponsibilityTypeID, S.SecurityAsset, S.SecurityAssetID);", new { r = ruleID.Value },
commandTimeout: 3600, transaction: trans);
            }
            else
            {
                cnn.Execute(@"
merge   ResponsibilityTypeRelationTypeItem as T 
using   ( 
        select  *
        from    #ResponsibilityTypeRelationTypeItem
        ) as S 
        on  (
            T.RuleID = S.RuleID 
            and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
            and T.[SecurityAsset] = S.[SecurityAsset] 
            and T.[SecurityAssetID] = S.[SecurityAssetID] 
            )
when    not matched by source and T.RuleID > 0 then 
        delete
when    not matched by target then 
        insert (RuleID, ResponsibilityTypeID, SecurityAsset, SecurityAssetID) 
        values (S.RuleID, S.ResponsibilityTypeID, S.SecurityAsset, S.SecurityAssetID);",
commandTimeout: 3600, transaction: trans);
            }

            #endregion

            if (useTransaction)
                trans.Commit();
        }
    }
}
