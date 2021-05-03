using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.extensions.storage;
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
        public AzureStorageProvider Storage { get; set; }

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

        protected void deleteExecution(SqlConnection Db, ScoreExecution executionRecord)
        {
            Db.Execute(@"delete metrics.Execution where Uid = @Uid", executionRecord);
        }

        protected void updateExecution(SqlConnection Db, ScoreExecution executionRecord)
        {
            Db.Execute(@"update metrics.Execution 
set     PercentComplete = @PercentComplete,
        Failures = @Failures, 
        ErrorMessage = @ErrorMessage,
        StartedOn = @StartedOn,
        CompletedOn = @CompletedOn, 
        ProcessingStartedOn = @ProcessingStartedOn,
        Processing = @Processing,
        TriggeredByExecutionUid = @TriggeredByExecutionUid,
        TriggeredByMeasureUid = @TriggeredByMeasureUid
where   Uid = @Uid", executionRecord);
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
