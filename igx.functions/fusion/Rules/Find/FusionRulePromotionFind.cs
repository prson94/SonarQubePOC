using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.functions.fusion.Rules
{
    internal class FusionRulePromotionFind : FusionFindActionBase, IFusionRuleAction
    {
        public string FindTargetField { get; set; }

        public FusionRulePromotionFind(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;

            LoadCommonFindSettings();
        }
        
        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (Step.Settings.ContainsKey("TargetField"))
            {
                FindTargetField = Step.Settings["TargetField"];
            }

            /// THis never worked right should read a value from the UI
            var previousStepID = -1;

            if (!string.IsNullOrEmpty(FindTargetField))
            {
                await company.ExecuteAsync(@"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                        select
                                TA.ID as AttributeID,
							    R.AttributeType as AttributeType, 
                                R.ObjectType,
								R.ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                        
						from	[fusion].[RulePromotion] R
						join	FusionAttribute SA on SA.ID = R.AttributeID
						join	Field SF on SF.ObjectType = @AttributeType 
								and SF.ObjectID = SA.ID 
								and SF.FieldTypeID = @FindFilterField
						join	FusionAttribute TA on TA.ID in @items
						join	Field TF on TF.ObjectType = @AttributeType
								and TF.ObjectID = TA.ID 
								and TF.FieldTypeID = @FindTargetField
						where	R.RuleStepID = @PromotionRuleStepID 
								and SF.Value = TF.Value
								and R.AttributeType = @AttributeType
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
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());
                    ", new { PromotionRuleStepID = previousStepID, RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, items = itemsToPromote, AttributeType = Rule.ObjectType.Replace("Type", "Type"), RuleStepID = Step.ID });
            }
            else
            {
                await company.ExecuteAsync(@"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                        select
                                AttributeID as AttributeID,
							    AttributeType as AttributeType, 
                                ObjectType,
								ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                              
						    from	[fusion].[RulePromotion]
						    join	FusionAttribute A on A.ID in @items
						    join	FusionAttribute AP on AP.ID = A.ParentID
						    where	RuleStepID = @PromotionRuleStepID
								and AttributeID = AP.ID and AttributeType = @AttributeType
                        ) as S
                    ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType
							--and T.ObjectType = S.ObjectType 
							--and T.ObjectID = S.ObjectID
					WHEN	MATCHED THEN
							UPDATE SET	T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.ObjectType = S.ObjectType,
										T.ObjectID = S.ObjectID,
										T.UpdatedOn = getutcdate()
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());
                    ", new { PromotionRuleStepID = previousStepID, RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, items = itemsToPromote, AttributeType = Rule.ObjectType.Replace("Type", "Type"), RuleStepID = Step.ID });
            }
        }
    }
}