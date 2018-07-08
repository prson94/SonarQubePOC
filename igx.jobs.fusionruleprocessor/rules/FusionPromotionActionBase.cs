using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    public enum FusionRuleParentSearchTypes
    {
        Invalid = -1,
        Direct,
        FusionOwner,
        ResultFromStep,
    }

    public class FusionPromotionActionBase : FusionActionBase
    {
        public int PromoteToObjectID { get; set; }
        public string PromoteToObject { get; set; }
        public string ParentObject { get; set; }
        public int ParentObjectID { get; set; }
        public FusionRuleParentSearchTypes ParentObjectSearchType { get; set; }

        public int PromoteToParentChildIntersectTypeID { get; set; }

        protected async Task CreatePromotedItemsTempTable(SqlConnection company)
        {
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#promotedItems') IS NOT NULL
			                                DROP TABLE #promotedItems;

		                                create table #promotedItems (                                            
			                                AttributeID int, 
                                            AttributeType varchar(20),
			                                ObjectID int,
                                            ObjectType varchar(20)
		                                );
                                ");
        }

        protected async Task PerformPostPromote(SqlConnection company, int promoteToObjectID, string promoteToObject)
        {            
            var sql = @"insert into #fieldValues
                            select distinct
	                            p.ObjectType as ObjectType,
	                            p.ObjectId as ObjectID,
	                            ft.ID as FieldTypeID,
	                            f.[Value] as 'Value'
                            from
	                            #fields f
                                inner join #promotedItems p on (f.Id = p.AttributeId)
	                            inner join fieldtype ft on (f.TargetFieldName = ft.Name and ft.[Object] = @targetType and ft.[ObjectId] = @objectParentId)                            
                            where
	                            f.ObjectType = @objectParentType and f.RuleStepId = @stepId
                        ";

            await company.ExecuteAsync(sql, new { stepId = Step.ID, objectParentType = Rule.ObjectType.Replace("Type", ""), targetType = promoteToObject, objectParentId = promoteToObjectID }, commandTimeout: EXECUTION_TIMEOUT);

#if DEBUG
            await PrintTempTableContents(company, Log, "fieldvalues");
#endif

            Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}] Merging Fields Starting...");

            //merge the field values
            await MergeFieldValues(company);

            Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Target[{promoteToObject}] Target ID[{promoteToObjectID}] Merging Fields DONE!");

            // log the items we promoted
            await MergePromotionResults(company, Step, Rule);

            // update asset table updateon / updated by for existing items so it triggers audit
            await UpdateModifiedAssets(company);
        }

        private async Task MergeFieldValues(SqlConnection company)
        {
            await company.ExecuteAsync(@"
                merge	Field as T
			using	(
					select	f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from	#fieldValues f 
							inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
					) as S
			on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
			when	matched then
					update set T.Value = S.Value
			when	not matched then
					insert (ObjectType, ObjectID, FieldTypeID, Value) values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
            ", commandTimeout: EXECUTION_TIMEOUT);
        }

        private async Task UpdateModifiedAssets(SqlConnection company)
        {
            await company.ExecuteAsync(@"
                merge	Asset as T
			using	(
					select	distinct f.ObjectType as ObjectType,
							f.ObjectID as ObjectID							
					from	#fieldValues f 							
					) as S
			on		T.Object = S.ObjectType and T.ObjectID = S.ObjectID
			when	matched then
					update set T.UpdatedOn = getutcdate(), T.UpdatedBy = 0;
            ", commandTimeout: EXECUTION_TIMEOUT);
        }

        private async Task MergePromotionResults(SqlConnection company, FusionRuleStepModel step, FusionRule rule)
        {
            await company.ExecuteAsync(@"
                                MERGE	[fusion].[RulePromotion] AS T
					USING	(
							SELECT	AttributeID as AttributeID,
									AttributeType as AttributeType, 
									ObjectType as ObjectType, 
									ObjectID as ObjectID, 
									@RuleID as RuleID,
									@ObjectTypeID as PromotedObjectTypeID,
									@RuleStepID as RuleStepID
                            from #promotedItems 
							) as S
					ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType
							and T.ObjectType = S.ObjectType 
							and T.ObjectID = S.ObjectID
					WHEN	MATCHED THEN
							UPDATE SET	T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.UpdatedOn = getutcdate()
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());    
                        ", new { RuleID = rule.ID, RuleStepID = step.ID, ObjectTypeID = PromoteToObjectID }, commandTimeout: EXECUTION_TIMEOUT);
        }

        protected void LoadParentSearchOptions()
        {
            if (Step.Settings.ContainsKey("ParentObjectSearch"))
            {
                ParentObjectSearchType = (FusionRuleParentSearchTypes)Enum.Parse(typeof(FusionRuleParentSearchTypes), Step.Settings["ParentObjectSearch"]);
            }

            if (Step.Settings.ContainsKey("ParentObject"))
            {
                ParentObject = Step.Settings["ParentObject"];
            }

            if (Step.Settings.ContainsKey("ParentObjectID"))
            {
                ParentObjectID = int.Parse(Step.Settings["ParentObjectID"]);
            }
        }

        protected async Task CreateFieldValuesTempTable(SqlConnection company)
        {
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
			                                DROP TABLE #fieldValues;

		                                create table #fieldValues (
			                                ObjectType varchar(50), 
			                                ObjectID int, 
			                                FieldTypeID int, 
			                                Value nvarchar(max)
		                                );

		                                CREATE UNIQUE CLUSTERED INDEX PK_tempfieldValues ON #fieldValues ([ObjectType] ASC,[ObjectID] ASC,[FieldTypeID] ASC);
                                ", commandTimeout: EXECUTION_TIMEOUT);
        }

        protected async Task<IEnumerable<string>> GetKeyFields(SqlConnection company)
        {

            // get the key fields for the target type for the promotion
            return await company.QueryAsync<string>(@"select 
	                                                        Name
                                                        from 
	                                                        fieldtype 
                                                        where 
	                                                        [object] = @promoteToObjectType
		                                                        and 
	                                                        objectid = @promoteToObjectId
		                                                        and
	                                                        ispartofkey = 1", new { promoteToObjectType = PromoteToObject, promoteToObjectId = PromoteToObjectID });
        }

        protected async Task DetermineExisting(SqlConnection company, IEnumerable<string> keyFields)
        {

            // if the promote to object has a parent we need to consider that as well
            var sql = string.Empty;

            if (PromoteToParentChildIntersectTypeID <= 0)
            {
                sql = @"insert into #promotedItems 
                            select distinct
	                            ph.ObjectID,
                                cast(@objectType as varchar(20)),
                                ad.[objectID],
                                ad.[object]
                            from	
                                #promotionKeyHash ph
                                inner join assetdetail ad on ph.keyhash = ad.keyhash";

                await company.ExecuteAsync(sql, new { objectType = Rule.ObjectType.Replace("Type", "") }, commandTimeout: EXECUTION_TIMEOUT);

                return;
            }


                sql = @"insert into #promotedItems 
                             select distinct
	                            ph.ObjectID,
                                cast(@objectType as varchar(20)),
                                ad.[objectID],
                                ad.[object]
                            from	
                                #promotionKeyHash ph
                                inner join assetdetail ad on ph.keyhash = ad.keyhash
                                inner join #promotionParents p on (ph.ObjectID = p.ObjectID)
                                inner join [intersect] i on (i.IntersectTypeID = @it and i.objectid = ad.[objectID] and i.object = ad.[object] and i.subjectid = p.ParentID and i.subject = ad.object)";


            await company.ExecuteAsync(sql, new { objectType = Rule.ObjectType.Replace("Type",""), it = PromoteToParentChildIntersectTypeID }, commandTimeout: EXECUTION_TIMEOUT);

            return;
        }

        protected async Task<int> GetNewItemCount(SqlConnection company)
        {
            return await (company.ExecuteScalarAsync<int>("select count(1) from #promotedItems;"));
        }


        protected async Task ValidatePromotionItems(SqlConnection company, List<int> itemsToPromote, IEnumerable<string> keyFields)
        {
            // generate key field hashes for each of the items and store them in a temp table

            // look for duplicates
            await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#promotionKeyHash') IS NOT NULL
			                                DROP TABLE #promotionKeyHash;

		                                create table #promotionKeyHash (                                            			                                
			                                ObjectID int,
                                            KeyHash varchar(250)
		                                );
                                ");

            // determine item parents
            await company.ExecuteAsync(@"insert into #promotionKeyHash
		                        select		ObjectID,
					                        CONVERT(
						                        varchar(32), 
						                        SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
						                        2) as FieldHash
		                        from		(
			                        select	top 100000000	FV.TargetFieldTypeID as FieldTypeID,
						                        coalesce(FV.Value, '') as Value,
						                        FV.ID as ObjectID
			                        from		#fields FV
			                        where		FV.TargetFieldTypeID in (select id from fieldtype where [object] = @obj and [objectid] = @objId and name in @keyFields)
			                        order by	FV.ID,FV.TargetFieldTypeID
		                        ) A group by A.ObjectID", new { keyFields, obj = PromoteToObject, objId = PromoteToObjectID });

#if DEBUG
            await PrintTempTableContents(company, Log, "promotionKeyHash");
#endif

            // check the counts by hash
            //var duplicateHashes = await company.QueryAsync(@"select KeyHash, count(ObjectID) from #promotionKeyHash group by KeyHash having count(ObjectID) > 1");

            //remove items with same key that are not the first row partitioned by keyhash.
            await company.ExecuteAsync(@"delete from #fields where id in(select objectid from(
	                                                                        select KeyHash,ObjectID,rn =Row_number() over(partition by KeyHash order by ObjectID) from #promotionKeyHash where keyhash in(
		                                                                        select KeyHash from #promotionKeyHash group by KeyHash having count(ObjectID) > 1)
	                                                                            ) a where a.rn > 1
                                                            )");

#if DEBUG
            await PrintTempTableContents(company, Log, "fields");
#endif


        }
    }
}
