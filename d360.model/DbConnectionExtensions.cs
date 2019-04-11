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
                                var thenSql = cnn.GetThenResultsSql(rule, false, false);
                                var whenSql = cnn.GetWhenResultsSql(rule, false);

                                thenSql = string.Format(thenSql, "");

                                //merge into the asset table 
                                await cnn.ExecuteAsync($@"
                                    merge [dbo].[ResponsibilityRuleResultAsset] as T
			                                using	(
					                                    {whenSql}
					                                ) as S
			                                on		@ruleId = T.RuleID and S.AssetID = T.AssetID
			                                when	matched then
					                                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                                when	not matched by target then
					                                insert (RuleID, AssetID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetID,getutcdate(),0)
                                            when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
                                                    delete;
                                ", new { ruleId = rule.ID }, transaction: transaction);

                                //merge into the resource table
                                await cnn.ExecuteAsync($@"
                                    merge [dbo].[ResponsibilityRuleResultSecurityAsset] as T
			                                using	(
					                                    {thenSql}
					                                ) as S
			                                on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
			                                when	matched then
					                                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                                when	not matched by target then
					                                insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
                                            when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
                                                    delete;
                                ", new { ruleId = rule.ID, appliesToType = rule.ApplyToType}, transaction: transaction);
                            }
                            catch (Exception ex)
                            {
                                var ruleFailureLog = $"{rule.ID}: {ex.GetFullExceptionData()}. SQL was: {sqlToExecute}.\n";
                            }

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

        private static async Task ProcessTypeBasedResponsibilityRelationRules(SqlConnection cnn, int? ruleID, int timeout)
        {            
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

        private static async Task ProcessRuleForAssetType(SqlConnection cnn, ResponsibilityTypeRelationRule rule, int timeout, SqlTransaction transaction)
        {
            string sqlToExecute = "";
            try
            {                
                var thenSql = cnn.GetThenResultsSql(rule, false, false);
                thenSql = string.Format(thenSql, "");

                    //merge into the asset table 
                await cnn.ExecuteAsync(@"
                                    merge [dbo].[ResponsibilityRuleResultAsset] as T
			                using	(
					                select	T.ID as AssetTypeID,		
                                            R.ID as RuleID
					                from	AssetType T
							                inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID							                
						                where 
								                R.ID = @ruleId
					                ) as S
			                on		S.RuleID = T.RuleID and S.AssetTypeID = T.AssetTypeID
			                when	matched then
					                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                when	not matched by target then
					                insert (RuleID, AssetTypeID, UpdatedOn, UpdatedBy ) values (@ruleId,S.AssetTypeID,getutcdate(),0)
                            when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
                                    delete;
                ", new { ruleId = rule.ID }, transaction:transaction);

                    //merge into the resource table
                    await cnn.ExecuteAsync($@"
                                    merge [dbo].[ResponsibilityRuleResultSecurityAsset] as T
			                using	(
					                {thenSql}
					                ) as S
			                on		S.RuleID = T.RuleID and S.SecurityAsset = T.SecurityAsset and S.SecurityAssetID = T.SecurityAssetID
			                when	matched then
					                update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0
			                when	not matched by target then
					                insert (RuleID, SecurityAsset, SecurityAssetID ,UpdatedOn, UpdatedBy ) values (S.RuleID,S.SecurityAsset,S.SecurityAssetID,getutcdate(),0)
                            when NOT MATCHED BY SOURCE and T.RuleID = @ruleId THEN
                                    delete;
                ", new { ruleId = rule.ID }, transaction: transaction);

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
                            var thenFieldType = cnn.Query<FieldType>("select * from FieldType where ID = @FieldTypeID", new { rc.FieldTypeID }).SingleOrDefault();
                            thenSql += $"cross apply (select coalesce(FT.DefaultValue, F.Value) as [Value] from FieldType FT left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = '{obj}' and F.ObjectID = O.{uniqueIdField} ";
                            if (thenFieldType != null)
                            {
                                thenSql += (thenFieldType.AllowMultipleValues) ? 
                                    $"where FT.ID = {rc.FieldTypeID} and '{rc.Value}' in (select value from string_split(coalesce(F.Value, FT.DefaultValue),',')) ) FV{tCount}" :
                                    $"where FT.ID = {rc.FieldTypeID} and coalesce(F.Value, FT.DefaultValue) = '{rc.Value}' ) FV{tCount}";
                            }
                            else
                            {
                                thenSql += $"where FT.ID = {rc.FieldTypeID} and coalesce(F.Value, FT.DefaultValue) = '{rc.Value}' ) FV{tCount}";
                            }
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

        public static async Task RemoveRelationRuleResultsByRule(this DbConnection cnn, int ruleID)
        {            
            await (cnn.ExecuteAsync("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where RuleID = @givenRuleID", new { givenRuleID = ruleID }, commandTimeout: 7200));
            await (cnn.ExecuteAsync("delete [dbo].[ResponsibilityRuleResultAsset] where RuleID = @givenRuleID", new { givenRuleID = ruleID }, commandTimeout: 7200));
        }

        public static void ClearInvalidRelationRuleResults(this DbConnection cnn)
        {
            cnn.Execute("delete [dbo].[ResponsibilityRuleResultAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);            
            cnn.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where RuleID <> 0 and RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);            
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
