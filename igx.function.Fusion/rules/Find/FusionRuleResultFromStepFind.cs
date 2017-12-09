using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.function.Fusion.rules
{
    internal class FusionRuleResultFromStepFind : FusionFindActionBase, IFusionRuleAction
    {
        public FusionRuleResultFromStepFind(FusionRuleStepModel step, TraceWriter log, int companyId, FusionRule rule)
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
			                                ID int not null
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


                if (FindParent > 0)
                {
                    await company.QueryAsync(@"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                        select 
                                AttributeID as AttributeID,
							    AttributeType as AttributeType, 
                                co.parent as ObjectType,
								co.parentid as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						from	[fusion].[RulePromotion] rp
								inner join [cache].[objectdetails] co on(co.[object] = rp.objecttype and co.objectid = rp.objectid)
						where	rp.RuleID = @RuleID
								and rp.RuleStepID = @FindSearchObjectID
								and rp.AttributeID in (select id from #items)
								and rp.AttributeType = @AttributeType) as S
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
                    ", new { RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, AttributeType = Rule.ObjectType.Replace("Type", ""), RuleStepID = Step.ID }, transaction:transaction);
                }
                else
                {
                    await company.ExecuteAsync(@"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                        select
                                AttributeID as AttributeID,
							    AttributeType as AttributeType,
                                ObjectType,
								ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						from	[fusion].[RulePromotion]
						where	RuleID = @RuleID
								and RuleStepID = @FindSearchObjectID
								and AttributeID in (select id from #items)
								and AttributeType = @AttributeType
                        ) as S
                    ON	T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType							
					WHEN MATCHED THEN
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
                    ", new { RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, AttributeType = Rule.ObjectType.Replace("Type", ""), RuleStepID = Step.ID }, transaction:transaction);
                }
                transaction.Commit();
            }
        }
    }
}