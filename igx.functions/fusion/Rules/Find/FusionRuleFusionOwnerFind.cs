using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.functions.fusion.Rules
{
    internal class FusionRuleFusionOwnerFind : FusionFindActionBase, IFusionRuleAction
    {        
        public FusionRuleFusionOwnerFind(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;

            LoadCommonFindSettings();
        }


        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            using (var transaction = company.BeginTransaction())
            {
                await company.ExecuteAsync(@"IF OBJECT_ID('tempdb..#items') IS NOT NULL
			                                DROP TABLE #items;

		                                create table #items (                                            			                                
			                                AttributeID int not null,
                                            AttributeType varchar(20) not null
		                                );
                                ", transaction: transaction);
                
                using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, transaction))
                {
                    bulkCopy.BatchSize = itemsToPromote.Count;
                    bulkCopy.DestinationTableName = "#items";
                    bulkCopy.BulkCopyTimeout = 300;

                    var table = new DataTable();
                    var columnName = "AttributeID";
                    table.Columns.Add(columnName, typeof(int));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "AttributeType";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);
                    
                    foreach (var item in itemsToPromote)
                    {
                        var row = table.NewRow();

                        row["AttributeID"] = item;
                        row["AttributeType"] = Rule.ObjectType.Replace("Type","");
                        
                        table.Rows.Add(row);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }


                await company.ExecuteAsync(@"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                        select
                                AttributeID as AttributeID,
							    AttributeType as AttributeType, 
                                'Artifact' as ObjectType,
								@FindSearchObjectID as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID	
                        from #items
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
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());
                    ", new { RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, items = itemsToPromote, AttributeType = Rule.ObjectType.Replace("Type", "Type"), RuleStepID = Step.ID }, transaction);

                transaction.Commit();
            }
        }
    }
}