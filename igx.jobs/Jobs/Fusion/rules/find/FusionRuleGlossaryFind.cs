using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusion.rules
{
    internal class FusionRuleGlossaryFind : FusionFindActionBase, IFusionRuleAction
    {
        public int FindFilterFieldID { get; set; }
        public int FindTargetFieldID { get; set; }

        public FusionRuleGlossaryFind(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;

            LoadCommonFindSettings();
        }
        
        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (Step.Settings.ContainsKey("FilterField"))
            {
                if (int.TryParse(Step.Settings["FilterField"], out int findFieldID))
                    FindFilterFieldID = findFieldID;
            }

            if(Step.Settings.ContainsKey("TargetField"))
            {
                if (int.TryParse(Step.Settings["TargetField"], out int targetFieldID))
                    FindTargetFieldID = targetFieldID;
            }
            
            if (FindSearchObject == "ArtifactType")
            {
                await FindArtifact(itemsToPromote, company);
            }
            else if(FindSearchObject == "TaxonomyType")
            {
                await FindTaxonomy(itemsToPromote, company);
            }
        }

        private async Task FindTaxonomy(List<int> itemsToPromote, SqlConnection company)
        {
            //ui only supports find taxonomy by name field need to look at this before implementing.
        }

        private async Task FindArtifact(List<int> itemsToPromote, SqlConnection company)
        {
            if(FindTargetFieldID <= 0)
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action Find Glossary No target field to match on specified.");

                return;
            }
                        
            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select
                                fVal.ID as AttributeID,
							    fVal.ObjectType as AttributeType, 
                                'Artifact' as ObjectType,
								a.ID as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                        
						    from	Artifact a
                                    inner join field f on(a.ID = f.ObjectID and f.Objecttype = 'Artifact' and f.fieldtypeid = @FindTargetField)
                                    inner join #fields fVal on(fVal.ObjectType = @AttributeType and fVal.ID in @items and f.FormattedValue = fVal.Value and fVal.TargetFieldTypeID = f.fieldtypeid)
                            where	a.ArtifactTypeID = @FindSearchObjectID									                                    
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
            
            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, items = itemsToPromote, AttributeType = Rule.ObjectType.Replace("Type", ""), RuleStepID = Step.ID, FindTargetField = FindTargetFieldID });
        }
    }
}