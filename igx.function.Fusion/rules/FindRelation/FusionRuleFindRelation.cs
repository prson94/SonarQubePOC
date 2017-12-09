using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.function.Fusion.rules
{
    public class FusionRuleFindRelation : FusionActionBase, IFusionRuleAction
    {
        public FusionRuleFindRelation(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;
        }
        

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {               
            if (!Step.Settings.ContainsKey("IntersectType"))
            {
                Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Find Relation Step doesnt have an intersectTypeID specified.");

                return;
            }

            if (!Step.Settings.ContainsKey("Search"))
            {
                Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Find Relation Step doesnt have a search type specified.");

                return;
            }

            int.TryParse(Step.Settings["IntersectType"], out int intersectTypeID);
            string searchType = Step.Settings["Search"];


            await LoadItemsForFind(searchType, itemsToPromote, company);
            
            await PerformFindRelation(intersectTypeID, company);
        }

        private async Task PerformFindRelation(int intersectTypeID, SqlConnection company)
        {
            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            (select
		                            frel.AttributeID,
		                            frel.AttributeType,
                                    I.[Object] as ObjectType,
		                            I.ObjectID,
		                            @RuleID as RuleID,
		                            0 as PromotedObjectTypeID,
		                            @RuleStepID as RuleStepID
                            from	[Intersect] I
	                            inner join #findrelations frel on (I.Subject = frel.ObjectType and I.SubjectID = frel.ObjectID)
                            where	IntersectTypeID = @IntersectTypeID)
                            union
                            (select
		                            frel.AttributeID,
		                            frel.AttributeType,
                                    I.[Subject] as ObjectType,
		                            I.SubjectID,
		                            @RuleID as RuleID,
		                            0 as PromotedObjectTypeID,
		                            @RuleStepID as RuleStepID
                            from	[Intersect] I
	                            inner join #findrelations frel on (I.[Object] = frel.ObjectType and I.ObjectID = frel.ObjectID)
                            where	IntersectTypeID = @IntersectTypeID)
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

            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, IntersectTypeID = intersectTypeID, RuleStepID = Step.ID });
        }

        private async Task LoadItemsForFind(string searchType, List<int> itemsToPromote, SqlConnection company)
        {
            //create temp table with the fusion items and the found items
            await company.ExecuteAsync(@"
                    IF OBJECT_ID('tempdb..#findrelations') IS NOT NULL DROP TABLE #findrelations

                    create table #findrelations (		                        
                        ObjectID int,
                        ObjectType varchar(20),
			            AttributeID int,
			            AttributeType varchar(20)
		            );");

            if (string.Compare(searchType,"Self",true) == 0)
            {                
                foreach (var item in itemsToPromote)
                {
                    await company.ExecuteAsync(@"
                        insert into #findrelations
                            values(
                                @ObjectID,
                                @ObjectType,
                                @ObjectID,
                                @ObjectType);
                        ", new { ObjectType = Rule.ObjectType.Replace("Type",""), ObjectID = item});
                    
                }
            }
            else if(string.Compare(searchType, "ResultFromStep", true) == 0)
            {
                if (!Step.Settings.ContainsKey("ID"))
                {
                    Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Find Relation Step is of type ResultFromStep but doesnt specify a step id.");

                    return;
                }

                int.TryParse(Step.Settings["ID"], out int findSearchStep);

                await company.ExecuteAsync(@"
                        insert into #findrelations
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
                ", new { RuleID = Rule.ID, AttributeType = Rule.ObjectType, items = itemsToPromote, RuleStepID = findSearchStep });
            }            
        }
    }
}
