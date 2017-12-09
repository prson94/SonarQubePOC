using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.function.Fusion.rules
{
    public class FusionReferenceItemPromotionAction : FusionPromotionActionBase, IFusionRuleAction
    {
        public FusionReferenceItemPromotionAction(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule, int promoteToObjectID, string promoteToObject)
        {
            Step = step;
            CompanyId = companyId;
            Rule = rule;
            Log = log;
            PromoteToObjectID = promoteToObjectID;
            PromoteToObject = promoteToObject;
        }
        

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {            
            //create temp table for the fields
            await CreateFieldValuesTempTable(company);

            //create temp table for the items that we promote and what there new ids are
            await CreatePromotedItemsTempTable(company);


            // for ref items make sure there is a target field for Code
            var codeTarget = Step.Mappings.FirstOrDefault(x => x.TargetFieldName == "Code" && x.RuleStepID == Step.ID);

            if (codeTarget == null)
            {
                Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Doesnt contain a mapping for Code field.");

                return;
            }


            // merge the fields temp table based on the code field to the reference item table
            // if not matched insert

            await company.ExecuteAsync(@"
                    MERGE
	                    INTO    ReferenceItem d
	                    USING   (
			                    SELECT	f.ID, f.Value, f.ObjectType
			                    FROM	#fields f
                                    INNER Join
                                        (SELECT MIN(RowId) as RowId, Value
                                         FROM #fields 
                                         WHERE TargetFieldName = 'Code' and id in @ids
                                         GROUP BY Value) as f2 ON f.RowId = f2.RowId                                    
			                    ) S
	                    ON      (d.ReferenceItemTypeID = @promoteToId and d.Code = S.Value)
	                    WHEN NOT MATCHED THEN
	                        INSERT  (ReferenceItemTypeID, Code, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
	                        VALUES  (@promoteToId, S.Value, getutcdate(), 0, getutcdate(), 0)
                        WHEN MATCHED THEN
                            UPDATE SET UpdatedOn = getutcdate(), UpdatedBy = 0
                        output  S.ID, S.ObjectType, inserted.ID, @targetType into #promotedItems;
                ", new { promoteToId = PromoteToObjectID, ids = itemsToPromote, targetType = PromoteToObject.Replace("Type", "") });
            
#if DEBUG
            await PrintTempTableContents(company, Log, "promotedItems");
#endif

            await PerformPostPromote(company, PromoteToObjectID, PromoteToObject);
        }
    }
}
