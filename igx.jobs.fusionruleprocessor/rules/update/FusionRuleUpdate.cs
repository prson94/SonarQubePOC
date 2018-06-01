using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Data;

namespace igx.jobs.fusionruleprocessor
{
    public class FusionRuleUpdate : FusionActionBase, IFusionRuleAction
    {
        public FusionRuleUpdate(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;
        }

        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (!Step.Settings.ContainsKey("SubjectID"))
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Update step doesnt have a From Rule StepID setting specified.");

                return;
            }

            int.TryParse(Step.Settings["SubjectID"], out int fromRuleStepID);

            using (var transaction = company.BeginTransaction())
            {
                //get the item that this fusion was promoted to
                // update its values with the values of the fusionattribute / fusionqueryattribute
                await LoadItemsForUpdate(fromRuleStepID, itemsToPromote, company, transaction);

                await PerformUpdate(company, transaction);

                // log the items affected
                await UpdateRulePromotionItems(company, transaction);

                transaction.Commit();
            }
        }

        private async Task UpdateRulePromotionItems(SqlConnection company, SqlTransaction transaction)
        {

            var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select
		                            upd.AttributeID,
		                            upd.AttributeType,
                                    upd.ObjectType,
		                            upd.ObjectID,
		                            @RuleID as RuleID,
		                            0 as PromotedObjectTypeID,
		                            @RuleStepID as RuleStepID
                            from	#updateItems upd
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

            await company.ExecuteAsync(sql, new { RuleID = Rule.ID, RuleStepID = Step.ID }, transaction: transaction);
        }

        private async Task PerformUpdate(SqlConnection company, SqlTransaction transaction)
        {
#if DEBUG
            await PrintTempTableContents(company, Log, "updateItems", transaction);
#endif
            //delete any items getting updated more than one time

            await company.ExecuteAsync(@"delete from #updateItems where AttributeID in(select AttributeID from(
	                                                                        select ObjectID,AttributeID, rn =Row_number() over(partition by ObjectID order by AttributeID) from #updateItems ) a where a.rn > 1
                                                            )", transaction:transaction);

#if DEBUG
            await PrintTempTableContents(company, Log, "updateItems", transaction);
#endif

            await company.ExecuteAsync(@"
                    MERGE	[field] AS T
					USING	(
                            select distinct
                                fSource.value as value,
                                fSource.TargetFieldTypeID as FieldTypeID,
                                upd.ObjectType  as ObjectType,
                                upd.ObjectID as ObjectID,
                                a.id as AssetID
                           from #fields fSource
                           join #updateItems upd on (fSource.ID =upd.AttributeID and fSource.ObjectType = @updAttributeType)
                            inner join fieldtype ft on (fSource.TargetFieldTypeID = ft.id)
                            inner join asset a on (a.object = upd.objectType and a.objectid = upd.objectid)
                        ) as S
                    ON		T.FieldTypeID = S.FieldTypeID
							and T.ObjectType = S.ObjectType
							and T.ObjectID = S.ObjectID							
					WHEN	MATCHED THEN
							UPDATE SET	T.Value = S.Value, 										
										T.UpdatedBy = 0
					WHEN NOT MATCHED BY TARGET THEN
							INSERT (AssetID, ObjectType, ObjectID, FieldTypeID, Value, UpdatedBy) 
							VALUES (S.AssetID, S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value, 0);                    
                    ", new { updAttributeType = Rule.ObjectType.Replace("Type", "") }, transaction: transaction);


        }

        private async Task LoadItemsForUpdate(int fromStepID, List<int> itemsToPromote, SqlConnection company, SqlTransaction transaction)
        {
                await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#items') IS NOT NULL
			                                DROP TABLE #items;

		                                create table #items (                                            			                                
			                                ID int not null PRIMARY KEY
		                                );
                                ", transaction: transaction);

                using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, transaction))
                {
                    bulkCopy.BatchSize = itemsToPromote.Count;
                    bulkCopy.DestinationTableName = "#items";
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

                //create temp table with the fusion items and the found items
                await company.ExecuteAsync(@"
                    IF OBJECT_ID('tempdb..#updateItems') IS NOT NULL DROP TABLE #updateItems

                    create table #updateItems (		                        
                        ObjectID int,
                        ObjectType varchar(20),
			            AttributeID int,
			            AttributeType varchar(20)
		            );",transaction:transaction);


                await company.ExecuteAsync(@"
                        insert into #updateItems
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
                            AttributeID in (select distinct id from #items)
                ", new { RuleID = Rule.ID, AttributeType = Rule.ObjectType.Replace("Type", ""), RuleStepID = fromStepID }, transaction:transaction);
            
        }
    }
}

