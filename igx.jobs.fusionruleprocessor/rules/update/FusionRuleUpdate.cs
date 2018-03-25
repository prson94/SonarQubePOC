using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    public class FusionRuleUpdate : FusionActionBase, IFusionRuleAction
    {
        public FusionRuleUpdate(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;
        }

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (!Step.Settings.ContainsKey("SubjectID"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have a From Rule StepID setting specified.");

                return;
            }

            int.TryParse(Step.Settings["SubjectID"], out int fromRuleStepID);

            //get the item that this fusion was promoted to
            // update its values with the values of the fusionattribute / fusionqueryattribute
            await LoadItemsForUpdate(fromRuleStepID, itemsToPromote, company);

            await PerformUpdate(company);

            // log the items affected
            await UpdateRulePromotionItems(company);
        }

        private async Task UpdateRulePromotionItems(SqlConnection company)
        {
            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select
		                            upd.AttributeID,
		                            upd.AttributeType,
                                    upd.ObjectType,
		                            upd.ObjectID,
		                            @RuleID as RuleID,
		                            0 as PromotedObjectTypeID,
		                            @RuleStepID as RuleStepID
                            from	#updateItems upd
                        ) as S
                    ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType							
					WHEN	MATCHED THEN
							UPDATE SET	T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.ObjectType = S.ObjectType,
										T.ObjectID = S.ObjectID,
										T.UpdatedOn = getutcdate()
					WHEN NOT MATCHED BY TARGET THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate())
                    WHEN NOT MATCHED BY SOURCE AND T.RuleID = @RuleID and T.RuleStepID = @RuleStepID
                        THEN DELETE; 
                    ";

            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, RuleStepID = Step.ID });
        }

        private async Task PerformUpdate(SqlConnection company)
        {
            // update the fields for the item
            await company.ExecuteAsync(@"
                        update fTarget
                        set fTarget.value = fSource.value
                        from field fTarget
                        join #fields fSource on (fSource.SourceFieldTypeID = fTarget.FieldTypeID)
                        join #updateItems upd on (fSource.ID =upd.AttributeID and fSource.ObjectType = @updAttributeType and fTarget.ObjectType = upd.ObjectType and fTarget.ObjectID = upd.ObjectID)                        
                    ", new { updAttributeType = Rule.ObjectType.Replace("Type", "") });
        }

        private async Task LoadItemsForUpdate(int fromStepID, List<int> itemsToPromote, SqlConnection company)
        {
            //create temp table with the fusion items and the found items
            await company.ExecuteAsync(@"
                    IF OBJECT_ID('tempdb..#updateItems') IS NOT NULL DROP TABLE #updateItems

                    create table #updateItems (		                        
                        ObjectID int,
                        ObjectType varchar(20),
			            AttributeID int,
			            AttributeType varchar(20)
		            );");


            await company.ExecuteAsync(@"
                        insert into #updateItems
                        select
                            ObjectID,
                            ObjectType,
                            AttributeID,
                            AttributeType
                        from
                            fusion.rulepromotion
                        where
                            RuleID = @RuleID
                                and
                            RuleStepID = @RuleStepID
                                and
                            AttributeType = @AttributeType
                                and
                            AttributeID in @items
                ", new { RuleID = Rule.ID, AttributeType = Rule.ObjectType, items = itemsToPromote, RuleStepID = fromStepID });
        }
    }
}

