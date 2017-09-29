using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using d360.core.entities;
using Microsoft.Azure.WebJobs.Host;
using igx.functions.FusionRules.Models;
using System;
using Dapper;

namespace igx.functions.FusionRules
{
    internal class FusionRuleFusionFind : FusionFindActionBase, IFusionRuleAction
    {
        public FusionRuleFusionFind(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;

            LoadCommonFindSettings();
        }

        public int FindFilterFieldID { get; set; }

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (Step.Settings.ContainsKey("FilterField"))
            {                
                if (int.TryParse(Step.Settings["FilterField"], out int filterField))
                    FindFilterFieldID = filterField;
            }

            if(Rule.ObjectType == "FusionQueryAttributeType")
            {
                await FindFusionQueryAttribute(itemsToPromote, company);
            }
            else if(Rule.ObjectType == "FusionAttributeType")
            {
                await FindFusionAttribute(itemsToPromote, company);
            }
            else
            {
                Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action Find Fusion Unknown Fusion find object type.");

                return;
            }
        }

        private async Task FindFusionAttribute(List<int> itemsToPromote, SqlConnection company)
        {
            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select distinct
                                fVal.ID as AttributeID,
							    fVal.ObjectType as AttributeType, 
                                'FusionAttribute' as ObjectType,
								fa.ID as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                        
						    from 
                                #fields fVal
                                inner join field f on (fVal.Id = f.objectId and fVal.objectType = f.objecttype)
                                inner join fusionattribute fa on(f.value = fa.sourceid or f.value = fa.textpath or f.value = fa.name)
                            where
                                fVal.id in @items
                                    and
                                fVal.SourceFieldTypeID = @FindFilterFieldID
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
            
            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, RuleStepID = Step.ID, FindFilterFieldID = FindFilterFieldID, items = itemsToPromote });
        }

        private async Task FindFusionQueryAttribute(List<int> itemsToPromote, SqlConnection company)
        {
            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select distinct
                                fVal.ID as AttributeID,
							    fVal.ObjectType as AttributeType, 
                                'FusionAttribute' as ObjectType,
								fa.ID as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                        
						    from 
                                #fields fVal
                                inner join field f on (fVal.objectId = f.objectId and fVal.objectType = f.objecttype)
                                inner join fusionqueryattribute fa on(fa.name = f.value)
                            where
                                fVal.id in @items
                                    and
                                fVal.SourceFieldTypeID = @FindFilterFieldID
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

            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, RuleStepID = Step.ID, FindFilterFieldID = FindFilterFieldID, items = itemsToPromote });
        }
    }
}