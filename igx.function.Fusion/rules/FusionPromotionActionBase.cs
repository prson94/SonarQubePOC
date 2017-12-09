using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.function.Fusion.rules
{
    public enum FusionRuleParentSearchTypes
    {
        Invalid = -1,
        Direct,
        FusionOwner,
        ResultFromStep,
    }

    public class FusionPromotionActionBase : FusionActionBase
    {
        public int PromoteToObjectID { get; set; }
        public string PromoteToObject { get; set; }
        public string ParentObject { get; set; }
        public int ParentObjectID { get; set; }
        public FusionRuleParentSearchTypes ParentObjectSearchType { get; set; }

        protected async Task CreatePromotedItemsTempTable(SqlConnection company)
        {
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#promotedItems') IS NOT NULL
			                                DROP TABLE #promotedItems;

		                                create table #promotedItems (                                            
			                                AttributeID int, 
                                            AttributeType varchar(20),
			                                ObjectID int,
                                            ObjectType varchar(20)
		                                );
                                ");
        }

        protected async Task PerformPostPromote(SqlConnection company, int promoteToObjectID, string promoteToObject)
        {
            var sql = @"insert into #fieldValues
                            select distinct
	                            p.ObjectType as ObjectType,
	                            p.ObjectId as ObjectID,
	                            ft.ID as FieldTypeID,
	                            f.[Value] as 'Value'
                            from
	                            #fields f
                                inner join #promotedItems p on (f.Id = p.AttributeId)
	                            inner join fieldtype ft on (f.TargetFieldName = ft.Name and ft.[Object] = @targetType and ft.[ObjectId] = @objectParentId)                            
                            where
	                            f.ObjectType = @objectParentType
                        ";

            await company.ExecuteAsync(sql, new { objectParentType = Rule.ObjectType.Replace("Type", ""), targetType = promoteToObject, objectParentId = promoteToObjectID }, commandTimeout: EXECUTION_TIMEOUT);

#if DEBUG
            await PrintTempTableContents(company, Log, "fieldvalues");
#endif

            Log.Info($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}] Merging Fields Starting...");

            //merge the field values
            await MergeFieldValues(company);

            Log.Info($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}] Merging Fields DONE!");

            // log the items we promoted
            await MergePromotionResults(company, Step, Rule);
        }

        private async Task MergeFieldValues(SqlConnection company)
        {
            await company.ExecuteAsync(@"
                merge	Field as T
			using	(
					select	f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from	#fieldValues f 
							inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
					) as S
			on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
			when	matched then
					update set T.Value = S.Value
			when	not matched then
					insert (ObjectType, ObjectID, FieldTypeID, Value) values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
            ", commandTimeout: EXECUTION_TIMEOUT);
        }

        private async Task MergePromotionResults(SqlConnection company, FusionRuleStepModel step, FusionRule rule)
        {
            await company.ExecuteAsync(@"
                                MERGE	[fusion].[RulePromotion] AS T
					USING	(
							SELECT	AttributeID as AttributeID,
									AttributeType as AttributeType, 
									ObjectType as ObjectType, 
									ObjectID as ObjectID, 
									@RuleID as RuleID,
									@ObjectTypeID as PromotedObjectTypeID,
									@RuleStepID as RuleStepID
                            from #promotedItems 
							) as S
					ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType
							and T.ObjectType = S.ObjectType 
							and T.ObjectID = S.ObjectID
					WHEN	MATCHED THEN
							UPDATE SET	T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.UpdatedOn = getutcdate()
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());    
                        ", new { RuleID = rule.ID, RuleStepID = step.ID, ObjectTypeID = PromoteToObjectID }, commandTimeout: EXECUTION_TIMEOUT);
        }

        protected void LoadParentSearchOptions()
        {
            if (Step.Settings.ContainsKey("ParentObjectSearch"))
            {
                ParentObjectSearchType = (FusionRuleParentSearchTypes)Enum.Parse(typeof(FusionRuleParentSearchTypes), Step.Settings["ParentObjectSearch"]);
            }

            if (Step.Settings.ContainsKey("ParentObject"))
            {
                ParentObject = Step.Settings["ParentObject"];
            }

            if (Step.Settings.ContainsKey("ParentObjectID"))
            {
                ParentObjectID = int.Parse(Step.Settings["ParentObjectID"]);
            }
        }

        protected async Task CreateFieldValuesTempTable(SqlConnection company)
        {
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
			                                DROP TABLE #fieldValues;

		                                create table #fieldValues (
			                                ObjectType varchar(50), 
			                                ObjectID int, 
			                                FieldTypeID int, 
			                                Value nvarchar(max)
		                                );

		                                CREATE UNIQUE CLUSTERED INDEX PK_tempfieldValues ON #fieldValues ([ObjectType] ASC,[ObjectID] ASC,[FieldTypeID] ASC);
                                ", commandTimeout: EXECUTION_TIMEOUT);
        }

        protected async Task<IEnumerable<string>> GetKeyFields(SqlConnection company)
        {

            // get the key fields for the target type for the promotion
            return await company.QueryAsync<string>(@"select 
	                                                        Name
                                                        from 
	                                                        fieldtype 
                                                        where 
	                                                        [object] = @promoteToObjectType
		                                                        and 
	                                                        objectid = @promoteToObjectId
		                                                        and
	                                                        ispartofkey = 1", new { promoteToObjectType = PromoteToObject, promoteToObjectId = PromoteToObjectID });
        }

        protected async Task DetermineExisting(SqlConnection company, IEnumerable<string> keyFields)
        {
            var sql = @"insert into #promotedItems 
                            select
	                            ftemp.ID,
                                ftemp.ObjectType,
                                f.[objectID],
                                f.[objectType]
                            from	
	                            #fields ftemp
	                            inner join field f on (ftemp.TargetFieldTypeID = f.FieldTypeID and ftemp.Value = f.FormattedValue)
                            where
	                            ftemp.TargetFieldName in @keyFields;";

            await company.ExecuteAsync(sql, new { keyFields = keyFields }, commandTimeout: EXECUTION_TIMEOUT);
        }

        protected async Task<int> GetNewItemCount(SqlConnection company)
        {
            return await (company.ExecuteScalarAsync<int>("select count(1) from #promotedItems;"));
        }
    }
}
