using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using d360.core.queue;
using d360.model;
using d360.utils.company;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public abstract class ProcessBase
    {
        public ScoreQueueInfo Info { get; set; }
        public ScoreExecution ExecutionRecord { get; set; }

        string companyConnectionString = null;

        internal SqlConnection GetEnvironmentConnection()
        {
            if (string.IsNullOrEmpty(companyConnectionString))
            {
                companyConnectionString = CompanyConnectionUtils.GetCompanyConnectionString(Info.CompanyID);
            }
            return new SqlConnection(companyConnectionString);
        }

        internal ICompanyContext GetCompanyContext()
        {
            // Create EF connection
            return JobDbContextCreator.CreateWebjobCompanyContext(this.Info.CompanyID, 0, "", true);
        }

        internal MetConditionsModel CheckMeasureConditions(
            List<AssetMeasuresProcessField> assetFields,
            List<FieldType> assetFieldTypes,
            AllocationDataModel measure,
            bool matchExtraneousConditions = false)
        {
            var metConditions = new MetConditionsModel();
            metConditions.ConditionMet = (measure.Conditions.Count == 0);

            if (measure.Conditions.Count > 0)
            {
                measure.Conditions.OrderBy(c => c.Position).ToList().ForEach(c => {

                    if (!metConditions.ConditionMet || matchExtraneousConditions)
                    {
                        int conditionsMetCount = 0;

                        if (c.Items == null)
                        {
                            c.Items = new List<AllocationDataModelConditionItem>();
                        }

                        c.Items.ForEach(i =>
                        {
                            var fieldType = assetFieldTypes.SingleOrDefault(f => f.ID == i.ConditionFieldTypeID);
                            var fieldValue = assetFields.FirstOrDefault(f => f.FieldTypeID == i.ConditionFieldTypeID);
                            if (fieldType != null)
                            {
                                if (fieldValue == null)
                                {
                                    fieldValue = new AssetMeasuresProcessField();
                                }
                                if (fieldType.Type == DataType.Lookup.ToString() && fieldType.AllowMultipleValues)
                                {
                                    var fieldValues = (fieldValue.Values ?? "").Split(',');
                                    if (i.ConditionType == MetricConditionType.And)
                                    {
                                        if (i.Operator == Operator.NotEquals)
                                        {
                                            int conditionValueCountMet = i.Values.Count;
                                            i.Values.ForEach(cv =>
                                            {
                                                if (!fieldValues.Any(fv => fv == cv))
                                                {
                                                    conditionValueCountMet--;
                                                }
                                            });
                                            if (conditionValueCountMet == 0) // No condition values met by field, which for "neq" is a good thing.
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                        else
                                        {
                                            int conditionValueCountMet = 0;
                                            i.Values.ForEach(cv =>
                                            {
                                                if (fieldValues.Any(fv => fv == cv))
                                                {
                                                    conditionValueCountMet++;
                                                }
                                            });
                                            if (conditionValueCountMet == i.Values.Count) // All condition values met by field.
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i.Operator == Operator.NotEquals)
                                        {
                                            // This NEQ logic is the same as above. Let's just get the logic right first before optimizing.
                                            int conditionValueCountMet = i.Values.Count;
                                            i.Values.ForEach(cv =>
                                            {
                                                if (!fieldValues.Any(fv => fv == cv))
                                                {
                                                    conditionValueCountMet--;
                                                }
                                            });
                                            if (conditionValueCountMet == 0) // No condition values met by field, which for "neq" is a good thing.
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                        else
                                        {
                                            if (i.Values.Intersect(fieldValues, new LowercaseStringEqualityComparer()).Any())
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (i.Operator.TestTwoValues(fieldType.Type, fieldType.AllowMultipleValues, i.Values, fieldValue.Values))
                                    {
                                        conditionsMetCount++;
                                    }
                                }
                            }
                        });

                        var met = false;
                        if (c.MatchType == MetricMatchType.All)
                        {
                            met = (conditionsMetCount == c.Items.Count);
                        }
                        else
                        {
                            met = (conditionsMetCount > 0);
                        }

                        if (met)
                        {
                            metConditions.ConditionMet = true;
                            var metCondition = new MetConditionModel
                            {
                                ConditionUid = c.ConditionUid,
                                Position = c.Position,
                                Weight = (c.Weight > 0) ? c.Weight : measure.Weight,
                                Threshold = (c.Threshold > 0) ? c.Threshold : measure.Threshold
                            };
                            metConditions.Conditions.Add(metCondition);
                        }
                    }

                });
            }

            if (metConditions.ConditionMet)
            {
                if (metConditions.Conditions.Count > 0)
                {
                    metConditions.SelectedWeight = metConditions.Conditions[0].Weight;
                    metConditions.SelectedThreshold = metConditions.Conditions[0].Threshold;
                }
                else 
                {
                    metConditions.SelectedWeight = measure.Weight;
                    metConditions.SelectedThreshold = measure.Threshold;
                }
            }
            else
            {
                if (!measure.MatchConditionsOnly)
                {
                    metConditions.ConditionMet = true;
                    metConditions.SelectedWeight = measure.Weight;
                    metConditions.SelectedThreshold = measure.Threshold;
                }
            }

            return metConditions;
        }

        internal SqlBulkCopy CreateBulkCopy(SqlConnection company, SqlTransaction trans, string tableName)
        {
            return new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, trans) {
                BatchSize = 5000,
                DestinationTableName = tableName,
                BulkCopyTimeout = 0
            };
        }

        protected void checkIfOtherRunningExecutions(SqlConnection company)
        {
            var ProcessingStartedOn = DateTime.UtcNow;

            // Clear orphaned executions.
            company.Execute(@"
update	metrics.execution 
set     ProcessingStartedOn = null,
        Processing = 0,
        Failures = Failures + 1,
        ErrorMessage = coalesce(ErrorMessage, '') + '; Cleared Processing flag due to orphaned execution'
where   Processing = 1 
        and CompletedOn is null 
        and Failures < 10
        and (
                ( UpdatedOn is not null and LoopSecondsElapsed is not null and @ProcessingStartedOn > dateadd(ss, LoopSecondsElapsed * 5, UpdatedOn) )
                or ( dateadd(dd, -1, ProcessingStartedOn) < @ProcessingStartedOn )
            )", new { ProcessingStartedOn }, commandTimeout: 600);

            var rowUpdated = company.Execute(@"
    update  metrics.Execution
    set     Processing = 1,
            ProcessingStartedOn = @ProcessingStartedOn
    where   Uid = @uid
            and not exists(select 1 from metrics.Execution where Uid <> @uid and Processing = 1 and CompletedOn is null and Failures < 10)", new { uid = Info.ExecutionUid, ProcessingStartedOn });

            if (rowUpdated <= 0)
            {
                throw new ScoresCurrentlyProcessingException();
            }
            ExecutionRecord.Processing = true;
            ExecutionRecord.ProcessingStartedOn = ProcessingStartedOn;
        }

        protected void deleteExecution(SqlConnection Db, ScoreExecution executionRecord)
        {
            Db.Execute(@"delete metrics.Execution where Uid = @Uid", executionRecord);
        }

        protected ScoreExecution getExecution(SqlConnection company)
        {
            var executionRecord = company.Query<ScoreExecution>("select * from metrics.Execution where Uid = @uid", new { uid = Info.ExecutionUid }).SingleOrDefault();

            if (executionRecord == null)
            {
                throw new ArgumentNullException("executionRecord", "Execution record must exist.");
            }

            if (executionRecord.Failures > 5)
            {
                company.Execute(@"
update  metrics.Execution 
set     Processing = 0, 
        ProcessingStartedOn = null, 
        CompletedOn = getutcdate(), 
        ErrorMessage = coalesce(ErrorMessage+'; ', '') + 'Too many failures' 
where   Uid = @uid", new { uid = Info.ExecutionUid });

                throw new ArgumentNullException("executionRecord", "Execution record has failed too many times.");
            }

            return executionRecord;
        }

        protected List<ScoreExecutionItem> getExecutionItems(SqlConnection company, int offset)
        {
            return company.Query<ScoreExecutionItem>($"select * from metrics.ExecutionItem where ExecutionID = @executionId and [State] = 0 and ChangeType = @ct order by RowNumber OFFSET {offset} ROWS FETCH NEXT 500 ROWS ONLY", new { executionId = ExecutionRecord.ID, ct = (int)Info.ChangeType }).ToList();
        }

        protected void updateExecutionMarkingItemsAsComplete(SqlConnection Db, ScoreExecution executionRecord)
        {
            Db.Execute(@"
update  metrics.ExecutionItem
set     State = 1
where   ExecutionID = @ID;

update  metrics.Execution 
set     PercentComplete = 1,
        Failures = @Failures, 
        ErrorMessage = @ErrorMessage,
        CompletedOn = getutcdate(), 
        ProcessingStartedOn = null,
        Processing = 0,
        UpdatedOn = getutcdate()
where   Uid = @Uid;", executionRecord, commandTimeout: 180);
        }

        protected void updateExecution(SqlConnection Db, ScoreExecution executionRecord)
        {
            Db.Execute(@"update T 
set     T.PercentComplete = Com.Completed / Tot.Total,
        T.Failures = @Failures, 
        T.ErrorMessage = iif(Com.Completed = Tot.Total, '', @ErrorMessage),
        T.StartedOn = @StartedOn,
        T.CompletedOn = @CompletedOn, 
        T.ProcessingStartedOn = @ProcessingStartedOn,
        T.Processing = @Processing,
        T.UpdatedOn = @UpdatedOn,
        T.LoopSecondsElapsed = @LoopSecondsElapsed,
        T.TriggeredByExecutionUid = @TriggeredByExecutionUid,
        T.TriggeredByMeasureUid = @TriggeredByMeasureUid
from    metrics.Execution T 
        cross apply (
            select count(1) as Total from metrics.ExecutionItem where ExecutionID = T.ID
        ) Tot
        cross apply (
            select count(1) as Completed from metrics.ExecutionItem where ExecutionID = T.ID and State <> 0
        ) Com
where   T.Uid = @Uid", executionRecord);
        }

        protected bool updateExecution(SqlConnection Db, ScoreExecution executionRecord, bool completed, Exception ex = null, bool shouldDeleteAfterCompletion = false)
        {
            bool closedExecution = false;

            try
            {
                // Reset on failure so it does not interfere with any other executing thread.
                if (executionRecord != null)
                {
                    executionRecord.Processing = false;
                    executionRecord.ProcessingStartedOn = null;
                    if (completed)
                    {
                        executionRecord.CompletedOn = DateTime.UtcNow;
                        closedExecution = true;
                    }
                    if (ex != null)
                    {
                        executionRecord.Failures += 1;
                        executionRecord.ErrorMessage += ex.GetFullExceptionData(false);
                    }

                    if (completed && shouldDeleteAfterCompletion)
                    {
                        deleteExecution(Db, executionRecord);
                    }
                    else
                    {
                        updateExecution(Db, executionRecord);
                    }
                }
            }
            catch
            {
                //do nothing.
            }
            
            return closedExecution;
        }
    }
}
