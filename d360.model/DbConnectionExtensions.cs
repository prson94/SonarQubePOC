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
using System.Threading.Tasks;

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
        public static async Task ProcessResponsibilityRelationRules(this SqlConnection cnn, int? ruleID = null, int timeout = 7200)
        {
            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultFixed);


            // Do two passes 1 pass for rules applying to type second pass for rules not applying to type
            // PASS 1 - Do rules that apply to types 
            await ProcessTypeBasedResponsibilityRelationRules(cnn, ruleID, timeout);

            // PASS 2 - Do rules that dont apply to types
            await ProcessAssetBasedResponsibilityRelationRules(cnn, ruleID, timeout);            
        }

        private static async Task ProcessAssetBasedResponsibilityRelationRules(SqlConnection cnn, int? ruleID, int timeout)
        {
            await CreateWorkAreaTables(cnn);

            IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesToRun(cnn, ruleID);

            List<int> rulesRequiringRun = new List<int>();

            foreach (var rule in rules)
            {
                try
                {
                    if (await ShouldRuleRun(cnn, rule.ID) && !rule.ApplyToType)
                    {
                        
                        rulesRequiringRun.Add(rule.ID);

                        rule.SetDefinitionFromRaw();

                        using (var transaction = cnn.BeginTransaction())
                        {

                            string sqlToExecute = "";

                            try
                            {
                                var thenSql = cnn.GetThenResultsSql(rule, false, false, "A.AssetID");
                                var whenSql = cnn.GetWhenResultsSql(rule, false);
                                sqlToExecute = $"insert into #ResponsibilityTypeRelationItem {string.Format(thenSql, (string.IsNullOrEmpty(whenSql) ? "" : $"cross apply ({whenSql}) A"))}";
                                await cnn.ExecuteAsync(sqlToExecute, commandTimeout: timeout, transaction:transaction);


                            }
                            catch (Exception ex)
                            {
                                var ruleFailureLog = $"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n";
                            }

                            // Mark the responsibility rule as having been built right now
                            // This is wrong because it may not have been built and the subsequent lines could fail...
                            await MarkResponsibilityRuleAsRan(cnn, rule.ID, transaction);
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }

            /*
            // Save the rules we are processing to a temp table so we can easily 
            await PopulateImpactedResponsibilityRulesTempTable(cnn, rulesRequiringRun, timeout);

            // For None Applies to type rules save the rule overrides
            await PerformResponsibilityRuleOverrideUpdates(cnn, timeout);

            // For Applies to type rules save assignments into responsibility temp table
           // await InsertRuleTypeAssignmentsIntoTempTable(cnn, timeout);

            // For all impacted rules delete/ update/ insert changes only applies to rules for entire type.
            await PerformFinalMerge(cnn, timeout);*/
        }

        private static async Task ProcessTypeBasedResponsibilityRelationRules(SqlConnection cnn, int? ruleID, int timeout)
        {
            await CreateWorkAreaTables(cnn);

            IEnumerable<ResponsibilityTypeRelationRule> rules = await GetRulesToRun(cnn, ruleID);

            List<int> rulesRequiringRun = new List<int>();

            foreach (var rule in rules)
            {
                try
                {
                    if (await ShouldRuleRun(cnn, rule.ID) && rule.ApplyToType)
                    {
                        rulesRequiringRun.Add(rule.ID);

                        rule.SetDefinitionFromRaw();

                        //create a transaction
                        using (var transaction = cnn.BeginTransaction())
                        {
                            await ProcessRuleForAssetType(cnn, rule, timeout, transaction);

                            // Mark the responsibility rule as having been built right now
                            // This is wrong because it may not have been built and the subsequent lines could fail...
                            await MarkResponsibilityRuleAsRan(cnn, rule.ID, transaction);

                            transaction.Commit();
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }


        #region Helper Methods

        /// <summary>
        /// Set a rule as having already been processed with the current date / time
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="ruleId"></param>
        /// <returns></returns>
        private static async Task MarkResponsibilityRuleAsRan(SqlConnection cnn, int ruleId, SqlTransaction transaction = null)
        {
            await cnn.ExecuteAsync("update ResponsibilityTypeRelationRule set LastRunOn = @date where ID = @id", new { date = DateTime.UtcNow, id = ruleId }, transaction: transaction);
        }

        /// <summary>
        /// Populate a temp table with the rules that have been changed with this run.
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="rulesRequiringRun"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private static async Task PopulateImpactedResponsibilityRulesTempTable(SqlConnection cnn, List<int> rulesRequiringRun, int timeout)
        {
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

                    await ruleIdBulkCopy.WriteToServerAsync(ruleIdTable);

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
        }


        /// <summary>
        /// Handles the changes to responsibility rules that are not on applies to type responsibility rules.  If there are no 
        /// rules that are not applies to type we should short circute this method.
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="timeout"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        private static async Task PerformResponsibilityRuleOverrideUpdates(SqlConnection cnn, int timeout, int pageSize = 10000000)
        {
            int position = 0;
            
            int recordCount = (await cnn.QueryAsync<int>(@"select count(*)
	                    from Asset A
			            inner join AssetType T on T.ID = A.AssetTypeID
			            inner join ResponsibilityTypeRelationRule R on R.ApplyToType = 0 and R.Object = T.Object and R.ObjectID = T.ObjectID
			            inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID
			            inner join #ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID")).FirstOrDefault();


            while (position < recordCount)
            {
                #region Insert item assignments into #resp table.

                await ClearResponsibilityTempTable(cnn);

                await cnn.ExecuteAsync($@"
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
                                offset {position} rows fetch next {pageSize} rows only", commandTimeout: timeout);

                position += pageSize;

                #endregion

                #region Update override columns

                await cnn.ExecuteAsync(@"
                            update	T
                            set		T.Overridden = 1,
		                            T.OverrideID = S.ID
                            from	#resp T
		                            inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID", commandTimeout: timeout);

                #endregion

                #region Merge final results into ResponsibilityTypeRelationRuleResult table

                await cnn.ExecuteAsync(@"
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
            		and T.RuleID in (select RuleID from #ResponsibilityTypeConsideredRules)", commandTimeout: timeout);

                await cnn.ExecuteAsync(@"
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
		                        and S.OverrideID = T.OverrideID", commandTimeout: timeout);

                await cnn.ExecuteAsync(@"
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
	                    where	T.RuleID is null", commandTimeout: timeout);

                #endregion

            }
        }

        private static async Task InsertRuleTypeAssignmentsIntoTempTable(SqlConnection cnn, int timeout)
        {
            await ClearResponsibilityTempTable(cnn);
            
            await cnn.ExecuteAsync(@"
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
			    inner join #ResponsibilityTypeRelationTypeItem I on I.RuleID = R.ID and R.ApplyToType = 1", commandTimeout: timeout);
        }

        private static async Task ClearResponsibilityTempTable(SqlConnection cnn)
        {
            await cnn.ExecuteAsync("truncate table #resp");
        }

        private static async Task PerformFinalMerge(SqlConnection cnn, int timeout)
        {
            await cnn.ExecuteAsync(@"
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
		        and T.RuleID in (select RuleID from #ResponsibilityTypeConsideredRules)", commandTimeout: timeout);

            await cnn.ExecuteAsync(@"
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
		                    and S.OverrideID = T.OverrideID", commandTimeout: timeout);

            await cnn.ExecuteAsync(@"
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
	                where	T.RuleID is null", commandTimeout: timeout);
        }

        private static async Task ProcessRuleForAssetType(SqlConnection cnn, ResponsibilityTypeRelationRule rule, int timeout, SqlTransaction transaction)
        {
            string sqlToExecute = "";
            try
            {
                await cnn.ExecuteAsync("truncate table #ResponsibilityTypeRelationTypeItem", transaction:transaction);

                var thenSql = cnn.GetThenResultsSql(rule, false, false);
                sqlToExecute = $@"insert into #ResponsibilityTypeRelationTypeItem {string.Format(thenSql, "")}";
                int i = await (cnn.ExecuteAsync(sqlToExecute, commandTimeout: timeout, transaction:transaction));
                
                    //merge into the asset table 
                    await cnn.ExecuteAsync(@"
                                    merge [dbo].[ResponsibilityRuleResultAsset] as T
			                using	(
					                select	R.ID as RuleID,
							                R.ResponsibilityTypeID,							
							                T.ID as AssetTypeID,							
							                REL.PermissionsBitMask							
					                from	AssetType T
							                inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
							                inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID							
						                where 
								                R.ID = @ruleId
					                ) as S
			                on		S.RuleID = T.RuleID and S.AssetTypeID = T.AssetTypeID
			                when	matched then
					                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                when	not matched by target then
					                insert (RuleID, AssetTypeID, PermissionsBitMask,UpdatedOn, UpdatedBy ) values (S.RuleID,S.AssetTypeID,S.PermissionsBitMask,getutcdate(),0)
                            when NOT MATCHED BY SOURCE THEN
                                    delete;
                ", new { ruleId = rule.ID }, transaction:transaction);

                    //merge into the resource table
                    await cnn.ExecuteAsync(@"
                                    merge [dbo].[ResponsibilityRuleResultResource] as T
			                using	(
					                select	R.ID as RuleID,							
							                I.SecurityAsset,
							                I.SecurityAssetID							
					                from	AssetType T
							                inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
							                inner join ResponsibilityTypeRelation REL on REL.ObjectType = T.Object and REL.ObjectID = T.ObjectID and REL.ResponsibilityTypeID = R.ResponsibilityTypeID
							                inner join #ResponsibilityTypeRelationTypeItem I on I.RuleID = R.ID and R.ApplyToType = 1
					                ) as S
			                on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
			                when	matched then
					                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                when	not matched by target then
					                insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
                            when NOT MATCHED BY SOURCE THEN
                                    delete;
                ", transaction: transaction);

            }
            catch (Exception ex)
            {
                var ruleFailureLog = $"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n";
            }
        }

        /// <summary>
        /// Does the current rule id need to be run?
        /// </summary>
        /// <param name="cnn">DB connection</param>
        /// <param name="ruleId">RUle ID to look at</param>
        /// <returns></returns>
        private static async Task<bool> ShouldRuleRun(SqlConnection cnn, int ruleId)
        {
            return await (cnn.QueryFirstAsync<bool>("exec ResponsibilityRuleShouldRun @id", new { id = ruleId }));
        }


        /// <summary>
        /// Create the temporary tables that are needed to process the responsibility rules
        /// </summary>
        /// <param name="cnn"></param>
        /// <returns></returns>
        private static async Task CreateWorkAreaTables(SqlConnection cnn)
        {
            using (SqlTransaction trans = cnn.BeginTransaction())
            {
                try
                {

                    await cnn.ExecuteAsync(@"
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

                    await cnn.ExecuteAsync(@"
                        DROP TABLE IF EXISTS #ResponsibilityTypeRelationTypeItem;
                        create table #ResponsibilityTypeRelationTypeItem (
                            RuleID int not null, 
                            ResponsibilityTypeID int not null, 
                            [SecurityAsset] char(1) not null, 
                            [SecurityAssetID] int not null
                        );
                        CREATE CLUSTERED INDEX CIX_TempResponsibilityTypeRelationTypeItem ON #ResponsibilityTypeRelationTypeItem (RuleID);
                        ", transaction: trans);

                    await cnn.ExecuteAsync(@"
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

                    await cnn.ExecuteAsync(@"
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
        }

        /// <summary>
        /// Load the Responsibility Rules that the rebuild process should run
        /// </summary>
        /// <param name="cnn">DB connection</param>
        /// <param name="ruleID">Specific responsibilty rule id to go after</param>
        /// <returns></returns>
        private static async Task<IEnumerable<ResponsibilityTypeRelationRule>> GetRulesToRun(SqlConnection cnn, int? ruleID)
        {
            if (ruleID.HasValue)
            {
                return (await cnn.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule where ID = @id", new { id = ruleID.Value }));
            }
            
            return  (await cnn.QueryAsync<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule"));            
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

        #endregion

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
