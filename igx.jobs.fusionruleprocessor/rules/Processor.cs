using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
{
    public static class Processor
    {
        const string functionName = "Fusion_ProcessRules";
        private static DateTime lastPromotionRun = DateTime.MinValue;

        
        public static int? ExecuteQueryTimeout { get; private set; }

        public static async Task Process(int companyId, TextWriter log)
        {

            using (var company = CompanyConnectionUtils.GetCompanyConnection(companyId))
            {
                int ruleLogId = 0;
                IEnumerable<d360.core.entities.FusionRule> fusionRulesToRun = null;
                List<FusionRuleStepStatistics> stats = new List<FusionRuleStepStatistics>();

                try
                {
                    company.OpenWithRetry(RetryPolicy.DefaultFixed);

                    lastPromotionRun = await GetFusionRulesLastRun(company);

                    log.WriteLine($"Company ID[{companyId}] Fusion rules were last run {lastPromotionRun}");

                    if (await HasPendingFusionRules(company, companyId, log))
                    {
                        return;
                    }


                    //determine which fusion rules should be run
                    fusionRulesToRun = await GetFusionRulesToProcess(company, companyId, log);

                    if (!fusionRulesToRun.Any())
                    {
                        // load the steps for the rules
                        log.WriteLine($"Company ID[{companyId}] has no fusion rules to run.");

                        return;
                    }

                    IEnumerable<FusionRuleStepModel> fusionRuleSteps = await GetFusionRuleSteps(fusionRulesToRun, company, companyId, log);

                    if (!fusionRuleSteps.Any())
                    {
                        log.WriteLine($"Company ID[{companyId}] fusion rules contain no step data.  This is not valid cannot run fusion rules with no steps.");

                        return;
                    }

                    //add a record to the fusion.rulelog table so that these rules doent run another instance while it is running
                    ruleLogId = await CreateFusionRuleLogRecord(company);

                    log.WriteLine($"Company ID[{companyId}] Created Fusion Rule Log ID [{ruleLogId}]");

                    foreach (var rule in fusionRulesToRun)
                    {
                        //print the details of this fusion rule
                        log.WriteLine($"Company ID[{companyId}] running fusion rule [{rule.Description}] rule ID [{rule.ID}], fusion ID [{rule.FusionID}], object type [{rule.ObjectType}], object id [{rule.ObjectID}]");


                        // get the filter items
                        var items = await GetRuleItems(rule.ID, company, companyId, log);

                        // if nothing to run for we are done
                        if (!items.Any())
                        {
                            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] contains no items skipping rule.");

                            continue;
                        }

                        log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Rule will run for {items.Count} items");

                        // load the items for these rules into a temp table
                        await CreateFusionRuleFieldsTempTable(rule, items, company, companyId, log);

#if DEBUG
                        await FusionActionBase.PrintTempTableContents(company, log, "fields");
#endif

                        //get the steps for this rule
                        var steps = fusionRuleSteps.Where(x => x.RuleID == rule.ID);

                        if (!steps.Any())
                        {
                            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Rule contains no steps");

                            continue;
                        }

                        log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Rule contains [{steps.Count()}] steps");

                        foreach (var step in steps)
                        {
                            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Starting...");

                            FusionRuleType action = (FusionRuleType)Enum.Parse(typeof(FusionRuleType), step.Action, true);

                            IFusionRuleAction ruleAction = FusionActionFactory.CreateAction(action, step, log, companyId, rule);

                            if (ruleAction == null) return;

                            await ruleAction.Execute(items, company);

                            stats.Add(ruleAction.Stats);

                            log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Step ID[{step.ID}] Action[{step.Action}] Completed...");
                        }
                    }
                    // update the rule log table 
                    // log the statistics for this fusion rule run things like how many promotions etc

                    // also send app insights message.


                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, companyId);
                }
                finally
                {
                    if(ruleLogId > 0) await CreateRuleExecutionLogRecord(company, fusionRulesToRun, stats, ruleLogId);
                }
            }
        }

        private static async Task<int> CreateFusionRuleLogRecord(SqlConnection company)
        {
            return (await company.QueryAsync<int>(@"
                    begin
                        insert into [fusion].[RuleLog] ( DateStarted ) values ( CURRENT_TIMESTAMP)
		                select cast(SCOPE_IDENTITY() as int);
                    end
                ")).Single();
        }

        private static async Task CreateRuleExecutionLogRecord(SqlConnection company, IEnumerable<FusionRule> fusionRulesToRun, List<FusionRuleStepStatistics> stepStats, int ruleLogId)
        {
            if(ruleLogId <= 0)
            {
                throw new Exception("Error Invalid rule log id cannot properly update execution log.");
            }
            int promotedArtifacts = 0;
            int promotedTaxonomies = 0;
            int rulesToRun = fusionRulesToRun != null ? fusionRulesToRun.Count() : 0;

            foreach (var stat in stepStats)
            {
                promotedArtifacts += stat.PromotedArtifacts;
                promotedTaxonomies += stat.PromotedTaxonomies;
            }

            await company.ExecuteAsync(@"update 
                                            fusion.[rulelog] 
                                        set DateCompleted = @end, PromotedTaxonomies = @promoTaxonomy, PromotedArtifacts = @promoArtifacts, TotalNewPromotions = @promoTotal, NumberOfRules = @ruleCount 
                                        where id = @ruleLogId                
                ", new { ruleCount = rulesToRun, end = DateTime.UtcNow, promoArtifacts = promotedArtifacts, promoTaxonomy = promotedTaxonomies, promoTotal = promotedArtifacts + promotedTaxonomies, ruleLogId = ruleLogId });
        }
        
        private static async Task CreateFusionRuleFieldsTempTable(FusionRule rule, List<int> items, SqlConnection company, int companyId, TextWriter log)
        {
            
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
                    bulkCopy.BatchSize = items.Count;
                    bulkCopy.DestinationTableName = "#items";
                    bulkCopy.BulkCopyTimeout = 300;

                    var table = new DataTable();
                    var columnName = "ID";
                    table.Columns.Add(columnName, typeof(int));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);
                    
                    foreach (var item in items)
                    {
                        var row = table.NewRow();

                        row["ID"] = item;                        

                        table.Rows.Add(row);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }

                //create new temp table
                await company.ExecuteAsync(@"
                    IF OBJECT_ID('tempdb..#fields') IS NOT NULL DROP TABLE #fields

                    create table #fields (		
                        RowID int not null identity(1,1) primary key,
                        ID int,
                        ObjectType varchar(20),
			            RuleID int,
			            RuleStepID int,
			            SourceFieldName nvarchar(250), 
			            SourceFieldTypeID int, 
			            TargetFieldName nvarchar(250), 
			            TargetFieldTypeID int, 
			            Value nvarchar(max)
		            );
	
		            CREATE NONCLUSTERED INDEX [CIX_TempPromoFields] ON #fields ( RuleID ASC );", commandTimeout: ExecuteQueryTimeout, transaction:transaction);


                log.WriteLine($"Company ID[{companyId}] Rule ID[{rule.ID}] Loading field values");

                if (rule.ObjectType == "FusionAttributeType")
                {
                    await company.ExecuteAsync(@"insert into #fields
			            select	FA.ID,
                                'FusionAttribute',
                                RS.RuleID,
					            M.RuleStepID,
					            M.SourceFieldName,
					            M.SourceFieldTypeID,
					            M.TargetFieldName,
					            M.TargetFieldTypeID,
					            case 
						            when M.SourceFieldName = 'ID' then cast(FA.ID as nvarchar)
						            when M.SourceFieldName = 'Name' then FA.Name
						            when M.SourceFieldName = 'TextPath' then FA.TextPath
						            when M.IsConstantValue = 1 then M.ConstantValue
					            end				
			            from	[fusion].[RuleStepMapping] M
					            inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName in ('ID', 'Name', 'TextPath') OR M.IsConstantValue = 1)					            
					            inner join FusionAttribute FA on FA.ID in (select id from #items) and FA.Deleted = 0
                        where 
                                RS.RuleID = @id
            ", new { id = rule.ID }, transaction:transaction, commandTimeout: FusionActionBase.EXECUTION_TIMEOUT);

                    await company.ExecuteAsync(@"insert into #fields
			            select  FA.ID,
                                'FusionAttribute',
                                RS.RuleID,
					            M.RuleStepID,
					            M.SourceFieldName,
					            M.SourceFieldTypeID,
					            M.TargetFieldName,
					            M.TargetFieldTypeID,
					            F.FormattedValue
			            from	[fusion].[RuleStepMapping] M
					            inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName not in ('ID', 'Name', 'TextPath') AND M.IsConstantValue = 0)					            
					            inner  join FusionAttribute FA on FA.ID in (select id from #items)
					            inner join FieldType FT on FT.ID = M.SourceFieldTypeID
					            inner join Field F on F.FieldTypeID = FT.ID and F.ObjectType = 'FusionAttribute' and (F.ObjectID = FA.ID OR F.ObjectID = FA.ParentID)
                        where 
                                RS.RuleID = @id
            ", new { id = rule.ID }, transaction:transaction, commandTimeout: FusionActionBase.EXECUTION_TIMEOUT);
                }
                else if (rule.ObjectType == "FusionQueryAttributeType")
                {

                    await company.ExecuteAsync(@"insert into #fields
			                    select  FA.ID,                                        
                                        'FusionQueryAttribute',
                                        RS.RuleID,
					                    M.RuleStepID,
					                    M.SourceFieldName,
					                    M.SourceFieldTypeID,
					                    M.TargetFieldName,
					                    M.TargetFieldTypeID,
					                    case 
						                    when M.SourceFieldName = 'ID' then cast(FA.ID as nvarchar)
						                    when M.IsConstantValue = 1 then M.ConstantValue
                                        end
                                from[fusion].[RuleStepMapping] M
                                    inner join[fusion].[RuleStep] RS on M.RuleStepID = RS.ID and(M.SourceFieldName = 'ID' OR M.IsConstantValue = 1)
                                    inner join FusionQueryAttribute FA on FA.ID in (select id from #items) and FA.Deleted = 0
                                where RS.RuleID = @id", new { id = rule.ID }, transaction:transaction, commandTimeout: FusionActionBase.EXECUTION_TIMEOUT);

                    await company.ExecuteAsync(@"insert into #fields
			                    select FA.ID,                                        
                                        'FusionQueryAttribute',
                                        RS.RuleID,
					                    M.RuleStepID,
					                    M.SourceFieldName,
					                    M.SourceFieldTypeID,
					                    M.TargetFieldName,
					                    M.TargetFieldTypeID,
					                    F.FormattedValue
                                from[fusion].[RuleStepMapping] M
                                    inner join[fusion].[RuleStep] RS on M.RuleStepID = RS.ID and(M.SourceFieldName<> 'ID' AND M.IsConstantValue = 0)                                        
                                        inner join FusionQueryAttribute FA on FA.ID in (select id from #items) and FA.Deleted = 0
					                    inner join Field F on F.ObjectType = 'FusionQueryAttribute' and F.ObjectID = FA.ID
                                        inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name                                        
                                where RS.RuleID = @id", new { id = rule.ID }, transaction:transaction, commandTimeout: FusionActionBase.EXECUTION_TIMEOUT);
                }

                transaction.Commit();
            }
        }

        private static async Task<IEnumerable<FusionRuleStepModel>> GetFusionRuleSteps(IEnumerable<FusionRule> fusionRulesToRun, SqlConnection company, int companyId, TextWriter log)
        {            
            // get all steps for the rules that need to be ran.
            var sql = @"select id, ruleid, step, action, description from fusion.rulestep where ruleid in @ids order by step;";

            var rules = fusionRulesToRun.Select(x => x.ID);

            var res = await company.QueryAsync<FusionRuleStepModel>(sql, new { ids = rules });

            //get the fields for the steps

            var ruleStepIds = res.Select(x => x.ID);

            var ruleStepSettings = (await company.QueryAsync("select RuleStepId, Name, Value from fusion.rulestepsetting where rulestepid in @stepIds;", new { stepIds = ruleStepIds }));

            // take the rule settings and populate the steps
            foreach (var setting in ruleStepSettings)
            {
                var ruleStepId = setting.RuleStepId;
                var name = setting.Name;
                var value = setting.Value;

                //find the item from res
                var step = res.Where(x => x.ID == ruleStepId).FirstOrDefault();

                if(step != null)
                {
                    step.Settings[name] = value;
                }
            }


            // get the fields for the steps
            var stepMappings = await company.QueryAsync<FusionRuleStepMappingModel>("select RuleStepID, SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, IsConstantValue, ConstantValue from fusion.rulestepmapping where RuleStepID in @stepIds;", new { stepIds = ruleStepIds });

            foreach(var mapping in stepMappings)
            {
                //add the mapping to the step that it belongs to
                var step = res.Where(x => x.ID == mapping.RuleStepID).FirstOrDefault();

                if(step != null)
                {
                    step.Mappings.Add(mapping);
                }
            }

            return res;
        }

        private static async Task<List<int>> GetRuleItems(int ruleId, SqlConnection company, int companyId, TextWriter log)
        {
            var sql = @"select sql from fusion.rulefilter where ruleid = @id";

            var ruleFilters = await company.QueryAsync<string>(sql, new { id = ruleId});

            List<int> items = new List<int>();

            foreach (var ruleFilter in ruleFilters)
            {
                //execute the rule filter and add the items to the list of items we need to run for
                items.AddRange(await company.QueryAsync<int>(ruleFilter));
            }

            return items;
        }

        /// <summary>
        /// Get the fusion rules that need to be processed.  A rule needs to be run if:
        ///     1. The rule has changed since the last time it was run
        ///     2. Fusion Data has changed for the rule since the last time it was run
        /// </summary>
        /// <param name="company"></param>
        /// <param name="companyId"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<FusionRule>> GetFusionRulesToProcess(SqlConnection company, int companyId, TextWriter log)
        {
            var sql = @"select	distinct
					R.ID,
                    R.Description,
                    R.FusionID,
                    R.ObjectType,
                    R.ObjectID
			from	fusion.Execution E 
					inner join [fusion].[Rule] R on R.FusionID = E.FusionID
			where	R.[Enabled] = 1 
					and E.DateCompleted > @lastRun
					and (E.Adds + E.Updates + E.Deletes) > 0
		
		-- Get rules that have been modified or added since last run of rules engine.
		union
			select	R.ID,
                    R.Description,
                    R.FusionID,
                    R.ObjectType,
                    R.ObjectID
			from	fusion.[Rule] R
			where	(
					R.UpdatedOn > @lastRun 
					and R.[Enabled] = 1 					
					)";

            return await company.QueryAsync<FusionRule>(sql, new { lastRun = lastPromotionRun });
            
        }

        /// <summary>
        /// Gets the datetime that fusion rules were last run
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static async Task<DateTime> GetFusionRulesLastRun(SqlConnection company)
        {
            var sql = "select max(DateStarted) from fusion.RuleLog";

            var res =  (await company.QueryAsync<DateTime?>(sql)).FirstOrDefault();

            if (res.HasValue)
                return res.Value;

            return new DateTime(1990, 1,1);
        }

        /// <summary>
        /// Returns true if there are outstanding fusion rules
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static async Task<bool> HasPendingFusionRules(SqlConnection company, int companyId, TextWriter log)
        {
            //check if fusion rules are running and have started within last day.
            var stillRunningSql = "select 1 from fusion.RuleLog where DateCompleted is null and DateStarted > DATEADD(day,-1,CURRENT_TIMESTAMP)";

            var res = (await company.QueryAsync<int>(stillRunningSql)).FirstOrDefault();

            if(res == 1)
            {
                log.WriteLine($"Company ID [{companyId}] has outstanding fusion jobs.  Will not try to run while they are pending.");

                return true;
            }

            return false;   
        }

        
    }
}
