using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using d360.core.entities;
using Microsoft.Azure.WebJobs.Host;
using Dapper;
using System.Linq;
using igx.functions.FusionRules.Models;

namespace igx.functions.FusionRules
{
    internal class FusionArtifactItemPromotionAction : FusionPromotionActionBase, IFusionRuleAction
    {
        public FusionArtifactItemPromotionAction(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule, int promoteToObjectID, string promoteToObject)
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
            //determine Parent Object Search Type
            LoadParentSearchOptions();

            if(ParentObjectSearchType == FusionRuleParentSearchTypes.Invalid)
            {                
                Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] contains an invalid Parent Search type.");

                return;
            }
            
            //create temp table for the fields
            await CreateFieldValuesTempTable(company);

            await CreatePromotedItemsTempTable(company);

            // get the key fields for the target type for the promotion
            var keyFields = await GetKeyFields(company);

            // make sure the promotion has a mapping for all key fields if it doesnt throw an error
            foreach (var keyField in keyFields)
            {
                //check that the promote step has a mapping for this key field if not throw an error
                if(!Step.Mappings.Any(x=>x.TargetFieldName == keyField))
                {
                    Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Is missing key field mapping for[{keyField}].");

                    return;
                }
            }

            // determine parents and put them in temp table
            await MapItemParents(company, itemsToPromote);

            // figure out which artifacts already exist and put them in the items to promote table
            await DetermineExisting(company, keyFields);

            // add new records for any items that havent already been promoted to the artifact table
            await CreateNewArtifacts(company, Rule.ID, Step.ID);

            Stats.PromotedArtifacts = await GetNewItemCount(company);
                        
#if DEBUG
            await PrintTempTableContents(company, Log, "promotedItems");
#endif

            await PerformPostPromote(company, PromoteToObjectID, PromoteToObject);            
        }

        private async Task MapItemParents(SqlConnection company, List<int> itemsToPromote)
        {
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#promotionParents') IS NOT NULL
			                                DROP TABLE #promotionParents;

		                                create table #promotionParents (                                            			                                
			                                ObjectID int,
                                            ParentID int
		                                );
                                ");

            // determine item parents

            Log.Info($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType}");

            switch (ParentObjectSearchType)
            {
                case FusionRuleParentSearchTypes.Direct:
                    Log.Info($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}");
                    if (ParentObjectID > 0)
                    {
                        await company.ExecuteAsync(@"
                            MERGE
	                            INTO    #promotionParents d
	                            USING   (
			                            select distinct
									        ftemp.ID as ObjectID,
									        @ParentSearchObjectID as ParentID
								        from
									        #fields ftemp
                                        where
                                            ftemp.id in @items    
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID) ;                       
                        ", new { items = itemsToPromote, ParentSearchObjectID = ParentObjectID });
                    }
                    break;
                case FusionRuleParentSearchTypes.FusionOwner:
                    Log.Info($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}");
                    if (ParentObjectID > 0)
                    {
                        await company.ExecuteAsync(@"
                            MERGE
	                            INTO    #promotionParents d
	                            USING   (
			                            select distinct
									        ftemp.ID as ObjectID,
									        @ParentSearchObjectID as ParentID
								        from
									        #fields ftemp
                                        where
                                            ftemp.id in @items    
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID) ;                       
                        ", new { items = itemsToPromote, ParentSearchObjectID = ParentObjectID });
                    }
                    break;
                case FusionRuleParentSearchTypes.ResultFromStep:
                    
                    if(ParentObjectID <= 0)
                    {
                        Log.Error($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}.  No Parent step specified!");

                        return;
                    }

                    await company.ExecuteAsync(@"
                            MERGE
	                            INTO    #promotionParents d
	                            USING   (
			                            select distinct
									        rp.AttributeID as ObjectID,
									        rp.ObjectID as ParentID
								        from
									        [fusion].[RulePromotion] rp
                                        where
                                            rp.RuleID = @RuleID and
                                            rp.RuleStepID = @RuleStepID and
                                            rp.AttributeID in @items                                            
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID);                        
                        ", new { items = itemsToPromote, RuleID = Rule.ID, RuleStepID = ParentObjectID });
                    break;                
            }

#if DEBUG
            await PrintTempTableContents(company, Log, "promotionParents");
#endif

        }

        private async Task CreateNewArtifacts(SqlConnection company, int ruleId, int ruleStepId)
        {
            // merge
            var sql = @"MERGE
	                    INTO    Artifact d
	                    USING   (
			                    select distinct
									ftemp.ID,
									ftemp.ObjectType,
                                    pp.ParentID as ParentID
								from
									#fields ftemp
                                    left join #promotionParents pp on (pp.ObjectID = ftemp.ID)
                                where
                                    not exists(select 1 from #promotedItems tmp where tmp.attributeID = ftemp.ID) and not exists(select 1 from [fusion].rulepromotion where ruleid = @ruleid and rulestepid = @rulestepid and attributeid = ftemp.ID)									
			                    ) S
	                    ON      (1 != 1)
	                    WHEN NOT MATCHED THEN
	                        INSERT  (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy, Visible, ParentID)
	                        VALUES  (@promoteToId, getutcdate(), 0, getutcdate(), 0, 1, S.ParentID)                        
                        output  S.ID, S.ObjectType, inserted.ID, @targetType into #promotedItems;";
            await company.ExecuteAsync(sql, new { promoteToId = PromoteToObjectID, targetType = "Artifact", rulestepid = ruleStepId, ruleid = ruleId });
        }
    }
}