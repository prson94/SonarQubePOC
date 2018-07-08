using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    internal class FusionTaxonomyItemPromotionAction : FusionPromotionActionBase, IFusionRuleAction
    {        
        public FusionTaxonomyItemPromotionAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule, int promoteToObjectID, string promoteToObject)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;
            PromoteToObjectID = promoteToObjectID;
            PromoteToObject = promoteToObject;
        }

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            //create temp table for the fields
            await CreateFieldValuesTempTable(company);

            await CreatePromotedItemsTempTable(company);

            var keyFields = await GetKeyFields(company);
            
            // make sure the promotion has a mapping for all key fields if it doesnt throw an error
            foreach (var keyField in keyFields)
            {
                //check that the promote step has a mapping for this key field if not throw an error
                if (!Step.Mappings.Any(x => x.TargetFieldName == keyField))
                {
                    Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Is missing key field mapping for[{keyField}].");

                    return;
                }
            }

            // validate that the input items doesnt have duplicated key fields
            await ValidatePromotionItems(company, itemsToPromote, keyFields);

            // figure out which taxonomy already exist and put them in the items to promote table
            await DetermineExisting(company, keyFields);

            // add new records for any items that havent already been promoted to the taxonomy table
            await CreateNewTaxonomies(company);

            Stats.PromotedTaxonomies = await GetNewItemCount(company);

#if DEBUG
            await PrintTempTableContents(company, Log, "promotedItems");
#endif

            //promote the items to specified location


#if DEBUG
            await PrintTempTableContents(company, Log, "promotedItems");
#endif

            await PerformPostPromote(company, PromoteToObjectID, PromoteToObject);
        }

        private async Task CreateNewTaxonomies(SqlConnection company)
        {
            // merge
            var sql = @"MERGE
	                    INTO    Taxonomy d
	                    USING   (
			                    select distinct
									ftemp.ID,
									ftemp.ObjectType
								from
									#fields ftemp
                                where
                                    not exists(select 1 from #promotedItems tmp where tmp.attributeID = ftemp.ID)									
			                    ) S
	                    ON      (1 != 1)
	                    WHEN NOT MATCHED THEN
	                        INSERT  (TaxonomyTypeID, UpdatedOn, UpdatedBy, Visible)
	                        VALUES  (@promoteToId, getutcdate(), 0, 1)                        
                        output  S.ID, S.ObjectType, inserted.ID, @targetType into #promotedItems;";
            await company.ExecuteAsync(sql, new { promoteToId = PromoteToObjectID, targetType = "Taxonomy" });
        }

    }
}