using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Data;

namespace igx.jobs.fusionruleprocessor
{
    internal class FusionArtifactItemPromotionAction : FusionPromotionActionBase, IFusionRuleAction
    {
        public FusionArtifactItemPromotionAction(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule, int promoteToObjectID, string promoteToObject)
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

            if (ParentObjectSearchType == FusionRuleParentSearchTypes.Invalid)
            {                
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] contains an invalid Parent Search type.");

                return;
            }

            PromoteToParentChildIntersectTypeID = await GetPromotionParentChildRelationshipID(company);

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
                    Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Is missing key field mapping for[{keyField}].");

                    return;
                }
            }

            // validate that the input items doesnt have duplicated key fields
            await ValidatePromotionItems(company, itemsToPromote, keyFields);

            // determine parents and put them in temp table
            await MapItemParents(company, itemsToPromote);

            // figure out which artifacts already exist and put them in the items to promote table
            await DetermineExisting(company, keyFields);
                        
            // copy n items to current promotion job table
            // do this until we get all the rows from promotedItems table
            var itemsToPromoteCount = itemsToPromote.Count;
            var transactionSize = PromotionChunkSize;
            var totalTransactions = (itemsToPromoteCount / transactionSize) + (itemsToPromoteCount % transactionSize > 0 ? 1 : 0);

            for (var i = 0; i < totalTransactions; i++)
            {
                using (var transaction = company.BeginTransaction())
                {
                    var startIndex = i * transactionSize + 1;                    
                    var endIndex = startIndex + transactionSize - 1;
                    if (endIndex > itemsToPromoteCount) endIndex = itemsToPromoteCount;

                    //create a sublist of items from items to promote for specified index range
                    var subList = itemsToPromote.GetRange(startIndex-1, endIndex - startIndex + 1);

                    await CreateNewArtifacts(company, Rule.ID, Step.ID, transaction, subList);
                           
                    // add intersects for parents from promotionParents table
                    await CreateParentIntersects(company, Rule.ID, Step.ID, transaction, subList);

                    Stats.PromotedArtifacts += await GetNewItemCount(company, subList, transaction);

                    await PerformPostPromote(company, PromoteToObjectID, PromoteToObject, subList, transaction);

                    transaction.Commit();
                }
            }

#if DEBUG
            await PrintTempTableContents(company, Log, "promotedItems");
#endif

        }

        private async Task<int> GetPromotionParentChildRelationshipID(SqlConnection company)
        {
            var sql = "select it.ID from intersecttype it inner join [predicate] pid on pid.id = it.predicateid  where [subject] = 'ArtifactType' and [object] = 'ArtifactType' and pid.type = 3 and it.objectid = @id";

            return (await company.QueryFirstOrDefaultAsync<int>(sql, new { id = PromoteToObjectID }));
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

            using (var transaction = company.BeginTransaction())
            {

                await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#itemsToPromote') IS NOT NULL
			                                DROP TABLE #itemsToPromote;

		                                create table #itemsToPromote (                                            			                                
			                                ID int not null
		                                );
                                ", transaction: transaction);

                using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, transaction))
                {
                    bulkCopy.BatchSize = itemsToPromote.Count;
                    bulkCopy.DestinationTableName = "#itemsToPromote";
                    bulkCopy.BulkCopyTimeout = 300;

                    var table = new DataTable();
                    var columnName = "ID";
                    table.Columns.Add(columnName, typeof(int));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    foreach (var item in itemsToPromote)
                    {
                        var row = table.NewRow();

                        row["ID"] = item;

                        table.Rows.Add(row);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }


                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType}");

                switch (ParentObjectSearchType)
                {
                    case FusionRuleParentSearchTypes.Direct:
                        Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}");
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
                                            ftemp.id in (select distinct id from #itemsToPromote)    
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID) ;                       
                        ", new { ParentSearchObjectID = ParentObjectID }, transaction:transaction,  commandTimeout: FusionActionBase.ExecutionTimeout);
                        }
                        break;
                    case FusionRuleParentSearchTypes.FusionOwner:
                        Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}");
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
                                            ftemp.id in  (select distinct id from #itemsToPromote)    
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID) ;                       
                        ", new { ParentSearchObjectID = ParentObjectID }, transaction: transaction, commandTimeout: FusionActionBase.ExecutionTimeout);
                        }
                        break;
                    case FusionRuleParentSearchTypes.ResultFromStep:

                        if (ParentObjectID <= 0)
                        {
                            Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action[{Step.Action}] Parent search is {ParentObjectSearchType} Parent ID is {ParentObjectID}.  No Parent step specified!");

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
                                            rp.AttributeID in (select distinct id from #itemsToPromote)                                          
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (ObjectID, ParentID)
	                                VALUES  (S.ObjectID, S.ParentID);                        
                        ", new { RuleID = Rule.ID, RuleStepID = ParentObjectID }, transaction: transaction, commandTimeout: FusionActionBase.ExecutionTimeout);
                        break;
                }

                if((ParentObjectID > 0)  && (ParentObjectSearchType == FusionRuleParentSearchTypes.Direct || ParentObjectSearchType == FusionRuleParentSearchTypes.FusionOwner || ParentObjectSearchType == FusionRuleParentSearchTypes.ResultFromStep))
                {
                    //delete any items that we didnt fine a parent for
                    await company.ExecuteAsync(@"delete from #fields where not exists(select 1 from  #promotionParents p where p.ObjectID = id )", transaction:transaction);

                }

#if DEBUG
                await PrintTempTableContents(company, Log, "promotionParents",transaction);
#endif
                transaction.Commit();
            }

        }

        private async Task CreateParentIntersects(SqlConnection company, int ruleId, int stepId, SqlTransaction transaction, List<int> itemsToPromote)
        {
            //get the intersecttype for this object
            var intersectTypeId = (await company.QueryAsync<int>("select id from intersecttypedetail where subject = 'ArtifactType' and [object] = 'ArtifactType' and [objectid] = @id and PredicateType = 3",
                        new { id = PromoteToObjectID }, transaction: transaction)).FirstOrDefault();

            if (intersectTypeId <= 0) return; // no valid parent intersect type

            // merge
            var sql = @"MERGE	[intersect] AS T
					USING	(
                        select
                                I.ID								
                                ,pp.ParentID
                                ,pI.ObjectID
						from	#promotionParents pp
                                inner join #promotedItems pI on (pp.objectid = pI.attributeid)
                                left outer join [intersect] I on(pp.ParentID = I.SubjectID and pI.ObjectID = I.ObjectID and I.IntersectTypeID = @IntersectTypeID)																			
                        where pI.attributeID in @items
                        ) as S
                    ON		T.ID = S.ID
					WHEN	NOT MATCHED THEN
							INSERT (IntersectTypeID,[Subject], SubjectID, [Object], ObjectID, State,CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Deleted, Visible) 
							VALUES (@IntersectTypeID, 'Artifact', S.ParentID, 'Artifact', S.ObjectID, 1,0,getutcdate(), 0,getutcdate(),0,1);";

            await company.ExecuteAsync(sql, new { IntersectTypeID = intersectTypeId, items = itemsToPromote }, commandTimeout: FusionActionBase.ExecutionTimeout, transaction: transaction);
        }

        private async Task CreateNewArtifacts(SqlConnection company, int ruleId, int ruleStepId, SqlTransaction transaction, List<int> itemsToPromote)
        {
            // merge
            var sql = @"MERGE
	                    INTO    Artifact d
	                    USING   (
			                    select distinct
									ftemp.ID,
									ftemp.ObjectType
								from
									#fields ftemp                                    
                                where
                                    not exists(select 1 from #promotedItems tmp where tmp.attributeID = ftemp.ID) 
                                        and
                                    ftemp.ID in @items
			                    ) S
	                    ON      (1 != 1)
	                    WHEN NOT MATCHED THEN
	                        INSERT  (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
	                        VALUES  (@promoteToId, getutcdate(), 0, getutcdate(), 0)                        
                        output  S.ID, S.ObjectType, inserted.ID, @targetType into #promotedItems;";
            await company.ExecuteAsync(sql, new { promoteToId = PromoteToObjectID, targetType = "Artifact", rulestepid = ruleStepId, ruleid = ruleId, items = itemsToPromote }, commandTimeout: FusionActionBase.ExecutionTimeout, transaction: transaction);
        }
    }
}