using d360.core.entities;
using d360.core.enums;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    internal class FusionRuleRelate : FusionActionBase, IFusionRuleAction
    {
        public int IntersectTypeID { get; set; }

        public int ObjectID { get; set; }
        public int SubjectID { get; set; }
        public string ObjectType { get; set; }
        public string SubjectType { get; set; }
        public FusionRuleRelateSearchType ObjectSearchType { get; set; }
        public FusionRuleRelateSearchType SubjectSearchType { get; set; }

        public FusionRuleRelate(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;
        }
        
        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (!LoadRelateSettings())
            {
                return;
            }

            await CreateRelationTempTable(company);
                        
            foreach (var item in itemsToPromote)
            {
                //get the subject object
                var subjectTarget = await GetObjectInfo(company, SubjectSearchType, SubjectType, SubjectID, item, Rule.ObjectType);
                //get the object, object
                var objectTarget = await GetObjectInfo(company, ObjectSearchType, ObjectType, ObjectID, item, Rule.ObjectType);

                if(subjectTarget == null || objectTarget == null)
                {
                    Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] relate cannot resolve either the source or target object.");

                    return;
                }

                //relate them
                await CreateRelationship(company, subjectTarget, objectTarget, IntersectTypeID, item, Rule.ObjectType.Replace("Type",""));
            }

#if DEBUG
            await PrintTempTableContents(company, Log, "createdIntersects");
#endif

            //save the generated intersect id / intersect type as the promotion result
            await SaveResults(company);
        }

        private async Task CreateRelationTempTable(SqlConnection company)
        {
            await company.ExecuteAsync(@"
                    IF OBJECT_ID('tempdb..#createdIntersects') IS NOT NULL DROP TABLE #createdIntersects

                    create table #createdIntersects (		                        
                        ObjectID int,
                        ObjectType varchar(20),
			            AttributeID int,
			            AttributeType varchar(20)
		            );");

        }

        private async Task SaveResults(SqlConnection company)
        {        
            var sql = @"
                    MERGE[fusion].[RulePromotion] AS T

                    USING(
                            select
                                    I.AttributeID,
                                    I.AttributeType,
                                    I.ObjectType as ObjectType,
                                    I.ObjectID,
                                    @RuleID as RuleID,
                                    0 as PromotedObjectTypeID,
                                    @RuleStepID as RuleStepID
                            from #createdIntersects I      
                        ) as S
                    ON T.RuleID = S.RuleID

                            and T.RuleStepID = S.RuleStepID

                            and T.AttributeID = S.AttributeID

                            and T.AttributeType = S.AttributeType

                    WHEN MATCHED THEN
                         UPDATE SET T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.ObjectType = S.ObjectType,
										T.ObjectID = S.ObjectID,
										T.UpdatedOn = getutcdate()

                    WHEN NOT MATCHED BY TARGET THEN

                            INSERT(AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn)

                            VALUES(S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate())
                    WHEN NOT MATCHED BY SOURCE AND T.RuleID = @RuleID and T.RuleStepID = @RuleStepID
                        THEN DELETE;
            ";

            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, RuleStepID = Step.ID });
        
        }

        private async Task CreateRelationship(SqlConnection company,dynamic subjectTarget, dynamic objectTarget, int intersectTypeID, int attributeID, string attributeType)
        {
            await company.ExecuteAsync(@"
                    begin
                        declare @intersectID int;
                    
                        exec [fusion].RelateAction @subjectType, @subjectId, @objectType, @objectId, @intersectTypeId, @intersectId output;
                        insert into #createdIntersects values(@intersectID, 'Intersect', @attributeID, @attributeType);
                        
                    end
                ", new { subjectType = subjectTarget.ObjectType, subjectId = subjectTarget.ObjectID, objectType = objectTarget.ObjectType, objectId = objectTarget.ObjectID, intersectTypeId = intersectTypeID, attributeID = attributeID, attributeType = attributeType });
        }

        private async Task<dynamic> GetObjectInfo(SqlConnection company, FusionRuleRelateSearchType searchType, string objectType, int objectID, int attributeID, string attributeType)
        {
            switch (searchType)
            {
                case FusionRuleRelateSearchType.Direct:
                    return new { ObjectType = objectType, ObjectID = objectID };                    
                case FusionRuleRelateSearchType.FusionOwner:
                    return new { ObjectType = "Artifact", objectID };
                case FusionRuleRelateSearchType.ResultFromStep:
                    if(string.Compare(objectType,"Step",true) != 0)
                    {
                        Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] ResultFromStep search doesnt have step specified as the object type.");

                        return null;
                    }

                    return (await company.QueryAsync(@"
                        select 
                            ObjectType,
                            ObjectID
                        from
                            [fusion].rulepromotion
                        where
                            RuleID = @RuleID
                                and
                            RuleStepID = @RuleStepID
                                and
                            AttributeID = @AttributeID
                                and
                            AttributeType = @AttributeType
                    ", new { RuleID = Rule.ID, RuleStepID = objectID, AttributeID = attributeID, AttributeType = attributeType.Replace("Type","")})).FirstOrDefault();
                case FusionRuleRelateSearchType.Self:
                    return new { ObjectType = attributeType.Replace("Type", ""), ObjectID = attributeID };                
            }

            Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] invalid search type specified.");

            return null;
        }

        private bool LoadRelateSettings()
        {
            if (!Step.Settings.ContainsKey("IntersectType"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] relate step doesnt have an intersect type id specified.");

                return false;
            }

            if (int.TryParse(Step.Settings["IntersectType"], out int intersectTypeID))
                IntersectTypeID = intersectTypeID;
                       
            if (Step.Settings.ContainsKey("ObjectID") && int.TryParse(Step.Settings["ObjectID"], out int objectID))
                ObjectID = objectID;

            if (!Step.Settings.ContainsKey("Object"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have an object specified.");

                return false;
            }

            ObjectType = Step.Settings["Object"];

            if (!Step.Settings.ContainsKey("ObjectSearch"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have an object search type specified.");

                return false;
            }

            ObjectSearchType = (FusionRuleRelateSearchType)Enum.Parse(typeof(FusionRuleRelateSearchType), Step.Settings["ObjectSearch"], true);

            if (!Step.Settings.ContainsKey("SubjectSearch"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have a subject search type specified.");

                return false;
            }

            SubjectSearchType = (FusionRuleRelateSearchType)Enum.Parse(typeof(FusionRuleRelateSearchType), Step.Settings["SubjectSearch"], true);

            if (!Step.Settings.ContainsKey("Subject"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have a subject specified.");

                return false;
            }

            SubjectType = Step.Settings["Subject"];

            if (Step.Settings.ContainsKey("SubjectID") && int.TryParse(Step.Settings["SubjectID"], out int subjectID))
                SubjectID = subjectID;

            return true;
        }
    }
}