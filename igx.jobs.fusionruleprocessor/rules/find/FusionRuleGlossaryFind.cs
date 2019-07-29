using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    internal class FusionRuleGlossaryFind : FusionFindActionBase, IFusionRuleAction
    {
        public int FindFilterFieldID { get; set; }
        public int FindTargetFieldID { get; set; }

        public FusionRuleGlossaryFind(FusionRuleStepModel step, TextWriter log, int companyId, FusionRule rule)
        {
            Step = step;
            Log = log;
            CompanyId = companyId;
            Rule = rule;

            LoadCommonFindSettings();
        }
        
        public async Task Execute(List<int> itemsToPromote, SqlConnection company)
        {
            if (Step.Settings.ContainsKey("FilterField"))
            {
                if (int.TryParse(Step.Settings["FilterField"], out int findFieldID))
                    FindFilterFieldID = findFieldID;
            }

            if(Step.Settings.ContainsKey("TargetField"))
            {
                if (int.TryParse(Step.Settings["TargetField"], out int targetFieldID))
                    FindTargetFieldID = targetFieldID;
            }
            
            if (FindSearchObject == "ArtifactType")
            {
                await FindArtifact(itemsToPromote, company);
            }
            else if(FindSearchObject == "TaxonomyType")
            {
                await FindTaxonomy(itemsToPromote, company);
            }
        }

        private async Task FindTaxonomy(List<int> itemsToPromote, SqlConnection company)
        {
            //ui only supports find taxonomy by name field need to look at this before implementing.
        }

        private async Task FindArtifact(List<int> itemsToPromote, SqlConnection company)
        {
            if(FindTargetFieldID <= 0)
            {
                Log.WriteLine($"Company ID[{CompanyId}] Rule ID[{Rule.ID}] Step ID[{Step.ID}] Action Find Glossary No target field to match on specified.");

                return;
            }

            using (var transaction = company.BeginTransaction())
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
                
                var sql = @"
                    MERGE	[fusion].[RulePromotion] AS T
					USING	(
                            select distinct
                                fVal.ID as AttributeID,
							    fVal.ObjectType as AttributeType, 
                                'Artifact' as ObjectType,
								a.ObjectID as ObjectID,
                                @RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID                        
						    from	Asset a
                                    inner join AssetType T on t.ID = a.AssetTypeID
                                    inner join field f on(a.ID = f.ObjectID and f.Objecttype = 'Artifact' and f.fieldtypeid = @FindTargetField)
                                    inner join #fields fVal on(fVal.ObjectType = @AttributeType and fVal.ID in (select distinct id from #items) and f.FormattedValue = fVal.Value and fVal.TargetFieldTypeID = f.fieldtypeid)
                            where	t.ObjectID = @FindSearchObjectID and a.[Object] = 'Artifact'								                                    
                        ) as S
                    ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType	
                            and T.ObjectID = S.ObjectID
                            and T.ObjectType = S.ObjectType
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

                await company.ExecuteAsync(sql, new { RuleID = Rule.ID, FindSearchObjectID = FindSearchObjectID, AttributeType = Rule.ObjectType.Replace("Type", ""), RuleStepID = Step.ID, FindTargetField = FindTargetFieldID }, transaction: transaction);

                transaction.Commit();
            }
        }
    }
}