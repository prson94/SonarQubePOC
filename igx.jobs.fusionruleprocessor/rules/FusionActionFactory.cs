using d360.core.entities;
using d360.core.enums;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.IO;

namespace igx.jobs.fusionruleprocessor
{
    public static class FusionActionFactory
    {
        internal static IFusionRuleAction CreateAction(FusionRuleType action, FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            switch (action)
            {
                case FusionRuleType.Promote:
                    return FusionActionFactory.CreatePromotionAction(step, log, companyId, rule);                    
                case FusionRuleType.Find:
                    return FusionActionFactory.CreateFindAction(step, log, companyId, rule);
                case FusionRuleType.FindRelation:
                    return FusionActionFactory.CreateFindRelation(step, log, companyId, rule);
                case FusionRuleType.Relate:
                    return FusionActionFactory.CreateRelateAction(step, log, companyId, rule);                    
                case FusionRuleType.Update:
                    return FusionActionFactory.CreateUpdateAction(step, log, companyId, rule);            }

            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Not Supported...");

            return null;
        }

        private static IFusionRuleAction CreateUpdateAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            return new FusionRuleUpdate(step, log, companyId, rule);
        }

        private static IFusionRuleAction CreateFindRelation(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            return new FusionRuleFindRelation(step, log, companyId, rule);
        }

        internal static IFusionRuleAction CreateRelateAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            return new FusionRuleRelate(step, log, companyId, rule);
        }

        internal static IFusionRuleAction CreateFindAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            if (!step.Settings.ContainsKey("ObjectSearch"))
            {
                log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Doesnt contain find search type.");

                return null;
            }

            FusionRuleFindSearchType searchType = (FusionRuleFindSearchType)Enum.Parse(typeof(FusionRuleFindSearchType), step.Settings["ObjectSearch"],true);

            switch (searchType)
            {
                case FusionRuleFindSearchType.Fusion:
                    return new FusionRuleFusionFind(step, log, companyId, rule);
                case FusionRuleFindSearchType.FusionOwner:
                    return new FusionRuleFusionOwnerFind(step, log, companyId, rule);                    
                case FusionRuleFindSearchType.Glossary:
                    return new FusionRuleGlossaryFind(step, log, companyId, rule);
                case FusionRuleFindSearchType.ResultFromStep:
                    return new FusionRuleResultFromStepFind(step, log, companyId, rule);                    
                case FusionRuleFindSearchType.Promotion:
                    return new FusionRulePromotionFind(step, log, companyId, rule);                    
                default:
                    log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Find Action doesnt contain a valid search type.");
                    return null;
            }            
        }
        

        internal static IFusionRuleAction CreatePromotionAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            if (!step.Settings.ContainsKey("Object"))
            {
                log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Doesnt contain object setting.");

                return null;
            }

            if (!step.Settings.ContainsKey("ObjectID"))
            {
                log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Doesnt contain object id setting.");

                return null;
            }

            var promoteToObject = (step.Settings["Object"] ?? "");
            var promoteToObjectID = int.Parse(step.Settings["ObjectID"]);

            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}]");

            if (promoteToObject == "ReferenceItemType" || promoteToObject == "ReferenceItem")
                return new FusionReferenceItemPromotionAction(step, log, companyId, rule, promoteToObjectID, promoteToObject);
            else if (promoteToObject == "ArtifactType")
                return new FusionArtifactItemPromotionAction(step, log, companyId, rule, promoteToObjectID, promoteToObject);
            else if (promoteToObject == "TaxonomyType")
                return new FusionTaxonomyItemPromotionAction(step, log, companyId, rule, promoteToObjectID, promoteToObject);

            log.WriteLine($"INVALID PROMOTION TARGET - Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}]");

            return null;
        }
    }
}
