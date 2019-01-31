using d360.core;
using d360.core.entities;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace d360.model
{
    public static class DbConnectionExtensions
    {
        #region Responsibility Rule Generation

        /// <summary>
        /// Re-process responsibility rules. By default this will re-process ALL rules unless passing a specific rule ID.
        /// </summary>
        /// <param name="cnn">The SQL connection object</param>
        /// <param name="ruleID">Optionall pass a specific rule by its ID.</param>
        public static void ProcessResponsibilityRelationRules(this SqlConnection cnn, int? ruleID = null)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultFixed);

            #region Create temporary tables

            using (SqlTransaction trans = cnn.BeginTransaction())
            {
                try
                {

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
    CREATE CLUSTERED INDEX CIX_TempResponsibilityTypeRelationItem ON #ResponsibilityTypeRelationItem (RuleID, AssetID, SecurityAssetID);
    ", transaction: trans);

                    cnn.Execute(@"
    DROP TABLE IF EXISTS #ResponsibilityTypeConsideredRules;
    create table #ResponsibilityTypeConsideredRules (
    RuleID int not null
    );
    CREATE CLUSTERED INDEX CIX_TempResponsibilityTypeConsideredRules ON #ResponsibilityTypeConsideredRules (RuleID);
    ", transaction: trans);

                    trans.Commit();
                }
                catch
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception ex)
                    {

                        // This catch block will handle any errors that may have occurred
                        // on the server that would cause the rollback to fail, such as
                        // a closed connection.

                        Console.WriteLine("Rollback Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                    }
                    throw;
                }
            }

            #endregion

            List<ResponsibilityTypeRelationRule> rules = null;

            if (ruleID.HasValue)
            {
                rules = cnn.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value }).ToList();
            }
            else
            {
                rules = cnn.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule").ToList();
            }

            string ruleFailureLog = "";
            List<int> rulesRequiringRun = new List<int>();

            rules.ForEach(rule =>
            {
                    try
                    {
                        var shouldrunRule = cnn.Query<bool>("exec ResponsibilityRuleShouldRun @id", new { id = rule.ID }).Single();

                        if (shouldrunRule)
                        {
                            rulesRequiringRun.Add(rule.ID);

                            rule.SetDefinitionFromRaw();

                            string sqlToExecute = "";
                            if (rule.ApplyToType)
                            {
                                try
                                {
                                    cnn.Execute("truncate table #ResponsibilityTypeRelationTypeItem");

                                var thenSql = cnn.GetThenResultsSql(rule, false, false);
                                    sqlToExecute = $@"insert into #ResponsibilityTypeRelationTypeItem {string.Format(thenSql, "")}";
                                    int i = cnn.Execute(sqlToExecute, commandTimeout: 1200);

                                    #region Insert type assignments into #resp table.

                                    cnn.Execute("truncate table #resp");

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
			    inner join #ResponsibilityTypeRelationTypeItem I on I.RuleID = R.ID and R.ApplyToType = 1", commandTimeout: 7200);


                                    #endregion

                                    #region Merge final results into ResponsibilityTypeRelationRuleResult table
                                    
                                    cnn.Execute(@"
                                        delete	T
                                        from	ResponsibilityTypeRelationRuleResult T
                                        where	T.RuleID = @ruleId",new { ruleId = rule.ID }, commandTimeout: 7200);


                                    cnn.Execute(@"
                                        insert into ResponsibilityTypeRelationRuleResult (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, PermissionsBitMask, IsVisible, Overridden, OverrideID)
	                                        select	S.*
	                                        from	#resp S ", commandTimeout: 7200);


                                    #endregion

                                    
                                }
                                catch (Exception ex)
                                {
                                    ruleFailureLog += $"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n";
                                }
                            }
                            else
                            {
                                try
                                {
                                    var thenSql = cnn.GetThenResultsSql(rule, false, false, "A.AssetID");
                                    var whenSql = cnn.GetWhenResultsSql(rule, false);
                                    sqlToExecute = $"insert into #ResponsibilityTypeRelationItem {string.Format(thenSql, (string.IsNullOrEmpty(whenSql) ? "" : $"cross apply ({whenSql}) A"))}";
                                    cnn.Execute(sqlToExecute, commandTimeout: 1200);
                                }
                                catch (Exception ex)
                                {
                                    ruleFailureLog += $"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n";
                                }
                            }

                            cnn.Execute("update ResponsibilityTypeRelationRule set LastRunOn = @date where ID = @id", new { date = DateTime.UtcNow, id = rule.ID });
                        }

                    }
                    catch
                    {
                       
                        throw;
                    }
              
            });

            #region  Insert into temporary ruleID table to ignore.

            using (SqlTransaction trans = cnn.BeginTransaction())
            {
                try
                {


                    var ruleIdTable = new System.Data.DataTable();

                    ruleIdTable.Columns.Add("RuleID", typeof(int));

                    rulesRequiringRun.ForEach(rid =>
                    {
                        var row = ruleIdTable.NewRow();
                        row["RuleID"] = rid;
                        ruleIdTable.Rows.Add(row);
                    });

                    var ruleIdBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans);

                    ruleIdBulkCopy.BatchSize = ruleIdTable.Rows.Count;
                    ruleIdBulkCopy.DestinationTableName = "#ResponsibilityTypeConsideredRules";
                    ruleIdBulkCopy.BulkCopyTimeout = 3600;

                    ruleIdBulkCopy.ColumnMappings.Add("RuleID", "RuleID");

                    ruleIdBulkCopy.WriteToServer(ruleIdTable);

                    trans.Commit();
                }
                catch
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception ex)
                    {

                        // This catch block will handle any errors that may have occurred
                        // on the server that would cause the rollback to fail, such as
                        // a closed connection.

                        Console.WriteLine("Rollback Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                    }
                    throw;
                }
            }

            #endregion

            int position = 0;
            int pageSize = 10000;
            int recordCount = cnn.Query<int>(@"select count(*)
	                    from Asset A
			            inner join AssetType T on T.ID = A.AssetTypeID
			            inner join ResponsibilityTypeRelationRule R on R.ApplyToType = 0 and R.Object = T.Object and R.ObjectID = T.ObjectID
			            inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID
			            inner join #ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID").FirstOrDefault();


            while (position < recordCount)
            {
                #region Insert item assignments into #resp table.

                cnn.Execute("truncate table #resp");

                cnn.Execute($@"
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
			                            inner join #ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID
                                order by R.ID, A.ID, I.SecurityAssetID
                                offset {position} rows fetch next {pageSize} rows only",  commandTimeout: 7200);

                position += pageSize;

                #endregion

                #region Update override columns

                cnn.Execute(@"
                            update	T
                            set		T.Overridden = 1,
		                            T.OverrideID = S.ID
                            from	#resp T
		                            inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID", commandTimeout: 7200);

                #endregion

                #region Merge final results into ResponsibilityTypeRelationRuleResult table

                cnn.Execute(@"
delete	T
from	ResponsibilityTypeRelationRuleResult T
		left join #resp S on 
					S.RuleID = T.RuleID
					and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
					and S.AssetID = T.AssetID 
					and S.AssetTypeID = T.AssetTypeID 
					and S.SecurityAsset = T.SecurityAsset 
					and S.SecurityAssetID = T.SecurityAssetID 
					and S.ApplyToType = T.ApplyToType
					and S.Overridden = T.Overridden
					and S.OverrideID = T.OverrideID
where	S.RuleID is null
		and T.RuleID in (select RuleID from #ResponsibilityTypeConsideredRules)", commandTimeout: 7200);

                cnn.Execute(@"
update	T
set		T.Context = S.Context,
		T.PermissionsBitMask = S.PermissionsBitMask,
		T.IsVisible = S.IsVisible
from	ResponsibilityTypeRelationRuleResult T
		inner join #resp S on S.RuleID = T.RuleID
		    and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
		    and S.AssetID = T.AssetID 
		    and S.AssetTypeID = T.AssetTypeID 
		    and S.SecurityAsset = T.SecurityAsset 
		    and S.SecurityAssetID = T.SecurityAssetID 
		    and S.ApplyToType = T.ApplyToType
		    and S.Overridden = T.Overridden
		    and S.OverrideID = T.OverrideID", commandTimeout: 7200);

                cnn.Execute(@"
insert into ResponsibilityTypeRelationRuleResult (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, PermissionsBitMask, IsVisible, Overridden, OverrideID)
	select	S.*
	from	#resp S 
			left join ResponsibilityTypeRelationRuleResult T on 
						S.RuleID = T.RuleID
						and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
						and S.AssetID = T.AssetID 
						and S.AssetTypeID = T.AssetTypeID 
						and S.SecurityAsset = T.SecurityAsset 
						and S.SecurityAssetID = T.SecurityAssetID 
						and S.ApplyToType = T.ApplyToType
						and S.Overridden = T.Overridden
						and S.OverrideID = T.OverrideID
	where	T.RuleID is null", commandTimeout: 7200);





                #endregion

            }

            #region Insert type assignments into #resp table.

            cnn.Execute("truncate table #resp");

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
			    inner join #ResponsibilityTypeRelationTypeItem I on I.RuleID = R.ID and R.ApplyToType = 1", commandTimeout: 7200);


            #endregion

            #region Merge final results into ResponsibilityTypeRelationRuleResult table

            cnn.Execute(@"
delete	T
from	ResponsibilityTypeRelationRuleResult T
		left join #resp S on 
					S.RuleID = T.RuleID
					and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
					and S.AssetID = T.AssetID 
					and S.AssetTypeID = T.AssetTypeID 
					and S.SecurityAsset = T.SecurityAsset 
					and S.SecurityAssetID = T.SecurityAssetID 
					and S.ApplyToType = T.ApplyToType
					and S.Overridden = T.Overridden
					and S.OverrideID = T.OverrideID
where	S.RuleID is null
		and T.RuleID in (select RuleID from #ResponsibilityTypeConsideredRules)", commandTimeout: 7200);

            cnn.Execute(@"
update	T
set		--T.Context = S.Context,
		T.PermissionsBitMask = S.PermissionsBitMask,
		T.IsVisible = S.IsVisible
from	ResponsibilityTypeRelationRuleResult T
		inner join #resp S on S.RuleID = T.RuleID
		    and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
		    and S.AssetID = T.AssetID 
		    and S.AssetTypeID = T.AssetTypeID 
		    and S.SecurityAsset = T.SecurityAsset 
		    and S.SecurityAssetID = T.SecurityAssetID 
		    and S.ApplyToType = T.ApplyToType
		    and S.Overridden = T.Overridden
		    and S.OverrideID = T.OverrideID", commandTimeout: 7200);

            cnn.Execute(@"
insert into ResponsibilityTypeRelationRuleResult (RuleID, ResponsibilityTypeID, AssetID, AssetTypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, PermissionsBitMask, IsVisible, Overridden, OverrideID)
	select	S.*
	from	#resp S 
			left join ResponsibilityTypeRelationRuleResult T on 
						S.RuleID = T.RuleID
						and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
						and S.AssetID = T.AssetID 
						and S.AssetTypeID = T.AssetTypeID 
						and S.SecurityAsset = T.SecurityAsset 
						and S.SecurityAssetID = T.SecurityAssetID 
						and S.ApplyToType = T.ApplyToType
						and S.Overridden = T.Overridden
						and S.OverrideID = T.OverrideID
	where	T.RuleID is null", commandTimeout: 7200);





            #endregion

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

        private static string GetWhenResultsSql(this DbConnection cnn, ResponsibilityTypeRelationRule rule, bool includeName = true)
        {
            string whenSql = "";

            whenSql += "select	A.ID as AssetID ";
            if (includeName)
                whenSql += ", utility.GetAssetDisplayValueWrapper(A.ID) as Name ";

            whenSql += $"from Asset A inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID} ";

            var fCount = 1;
            var rCount = 1;
            if (rule.StructuredDefinition != null && rule.StructuredDefinition.When != null)
            {
                rule.StructuredDefinition.When.ForEach(w => {
                    if (w.CheckType == "F")
                    {
                        if (w.FieldTypeID > 0)
                        {

                            whenSql += $"cross apply (select coalesce(FT.DefaultValue, F.Value) as [Value] from FieldType FT left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = A.Object and F.ObjectID = A.ObjectID ";
                            whenSql += $"where FT.ID = {w.FieldTypeID} and coalesce(F.Value, FT.DefaultValue) = '{w.Value}' ) FV{fCount}";
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

        private static string GetThenResultsSql(this DbConnection cnn, ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, bool includeName = true, string assetIDColumn = "")
        {
            string thenSql = "";

            int tCount = 1;
            string whenSuffix = "";
            string obj = "";
            string uniqueIdField = "ID";

            if ((rule.StructuredDefinition != null) && (rule.StructuredDefinition.Then != null) && (rule.StructuredDefinition.Then.Object != null))
            {
                thenSql = $@"select {rule.ID} as RuleID, {rule.ResponsibilityTypeID} as ResponsibilityTypeID, {(string.IsNullOrEmpty(assetIDColumn) ? "" : assetIDColumn + ", ")}";

                if (rule.StructuredDefinition.Then.Object == "OrganizationType")
                {
                    obj = "Organization";
                    thenSql += $"'O' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")} from Organization O ";
                }

                if (rule.StructuredDefinition.Then.Object == "GroupType")
                {
                    obj = "Group";
                    thenSql += $"'G' as SecurityAsset, O.ID as SecurityAssetID{(includeName ? ", O.Name" : "")}  from	[Group] O ";
                }

                if (rule.StructuredDefinition.Then.Object == "ResourceType")
                {
                    obj = "Resource";
                    uniqueIdField = "ResourceID";
                    thenSql += $@"'R' as SecurityAsset, O.ResourceID as SecurityAssetID{(includeName ? ", O.FirstName + ' ' + O.LastName as Name" : "")} from reporting.Global_Resource O ";
                }

                if (rule.StructuredDefinition.Then.Conditions != null)
                {
                    rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                    {
                        if (rc.FieldTypeID > 0)
                        {
                            thenSql += $"cross apply (select coalesce(FT.DefaultValue, F.Value) as [Value] from FieldType FT left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = '{obj}' and F.ObjectID = O.{uniqueIdField} ";
                            thenSql += $"where FT.ID = {rc.FieldTypeID} and coalesce(F.Value, FT.DefaultValue) = '{rc.Value}' ) FV{tCount}";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(rc.FieldTypeName) && !string.IsNullOrEmpty(rc.Value))
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
                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.[State] = 1";
                    if (IsHideData3SixtyUsers)
                    {
                        whenSuffix += " and (O.Email not like '%@data3sixty.com' and O.Email not like '%@infogix.com')";
                    }
                }
            }

            if (!string.IsNullOrEmpty(thenSql) || !string.IsNullOrEmpty(whenSuffix))
                thenSql += " {0} " + whenSuffix;

            return thenSql;
        }

        public static IEnumerable<ObjectResult> GetWhenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, SqlTransaction trans = null)
        {
            string sql = cnn.GetWhenResultsSql(rule);
            return cnn.Query<ObjectResult>(sql, transaction: trans, commandTimeout: 7200);
        }

        public static IEnumerable<SecurityResult> GetThenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule, bool IsHideData3SixtyUsers, SqlTransaction trans = null)
        {
            string sql = cnn.GetThenResultsSql(rule, IsHideData3SixtyUsers);
            return (string.IsNullOrEmpty(sql)) ?
                new List<SecurityResult>().AsEnumerable() :
                cnn.Query<SecurityResult>(sql.Replace(" {0} ", ""), transaction: trans, commandTimeout: 7200);
        }

        #endregion

        public static int UpdateFieldMove(this DbConnection cnn, FieldType toField, FieldType fromField, int currentResourceID)
        {
            string updateSql = $" Update fieldtype set ColumnOrder ={toField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={toField.ID};";
            if (fromField != null)
                updateSql += $" Update fieldtype set ColumnOrder ={fromField.ColumnOrder},UpdatedBy = {currentResourceID} where Id={fromField.ID};";
            return cnn.Execute(updateSql);
        }

        public static int UpdateFieldMove(this DbConnection cnn, List<FieldType> fields, int currentResourceID)
        {
            StringBuilder updateSql = new StringBuilder();
            foreach (var f in fields)
            {
                updateSql.Append($" Update fieldtype set ColumnOrder ={f.ColumnOrder},UpdatedBy = {currentResourceID} where Id={f.ID};");
            }

            return cnn.Execute(updateSql.ToString());
        }
    }
}
