using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using d360.utils.company;
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
            List<FieldDetail> assetFields,
            List<FieldType> assetFieldTypes,
            AllocationDataModel measure,
            bool matchExtraneousConditions = false)
        {
            var metConditions = new MetConditionsModel();
            metConditions.ConditionMet = (measure.Conditions.Count == 0);

            if (measure.Conditions.Count > 0)
            {
                measure.Conditions.ForEach(c => {

                    if (!metConditions.ConditionMet || matchExtraneousConditions)
                    {
                        int conditionsMetCount = 0;

                        c.Items.ForEach(i =>
                        {
                            var assetField = assetFields.SingleOrDefault(f => f.FieldTypeID == i.ConditionFieldTypeID);
                            var fieldType = assetFieldTypes.SingleOrDefault(f => f.ID == i.ConditionFieldTypeID);
                            if (assetField != null && fieldType != null)
                            {
                                if (fieldType.Type == DataType.Lookup.ToString() && fieldType.AllowMultipleValues)
                                {
                                    var fieldValues = assetField.Value.Split(',');
                                    var conditionValues = i.Values.Select(o => o.Value).ToList();
                                    if (i.ConditionType == MetricConditionType.And)
                                    {
                                        if (i.Operator == "neq")
                                        {
                                            int conditionValueCountMet = conditionValues.Count;
                                            conditionValues.ForEach(cv =>
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
                                            conditionValues.ForEach(cv =>
                                            {
                                                if (fieldValues.Any(fv => fv == cv))
                                                {
                                                    conditionValueCountMet++;
                                                }
                                            });
                                            if (conditionValueCountMet == conditionValues.Count) // All condition values met by field.
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i.Operator == "neq")
                                        {
                                            // This NEQ logic is the same as above. Let's just get the logic right first before optimizing.
                                            int conditionValueCountMet = conditionValues.Count;
                                            conditionValues.ForEach(cv =>
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
                                            if (conditionValues.Intersect(fieldValues).Any())
                                            {
                                                conditionsMetCount++;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    switch (i.Operator)
                                    {
                                        case "eq":
                                            if (assetField.Value == i.Values[0].Value)
                                            {
                                                conditionsMetCount++;
                                            }
                                            break;
                                        case "neq":
                                            if (assetField.Value != i.Values[0].Value)
                                            {
                                                conditionsMetCount++;
                                            }
                                            break;
                                        case "gt":
                                        case "gte":
                                        case "lt":
                                        case "lte":
                                            dynamic conditionValue;
                                            dynamic fieldValue;
                                            if (fieldType.Type == DataType.Boolean.ToString())
                                            {
                                                conditionValue = bool.Parse(i.Values[0].Value);
                                                fieldValue = bool.Parse(assetField.Value);
                                            }
                                            else if (fieldType.Type == DataType.Date.ToString() || fieldType.Type == DataType.DateTime.ToString())
                                            {
                                                conditionValue = DateTime.Parse(i.Values[0].Value);
                                                fieldValue = DateTime.Parse(assetField.Value);
                                            }
                                            else if (fieldType.Type == DataType.Decimal.ToString())
                                            {
                                                conditionValue = decimal.Parse(i.Values[0].Value);
                                                fieldValue = decimal.Parse(assetField.Value);
                                            }
                                            else if (fieldType.Type == DataType.Number.ToString())
                                            {
                                                conditionValue = int.Parse(i.Values[0].Value);
                                                fieldValue = int.Parse(assetField.Value);
                                            }
                                            else
                                            {
                                                conditionValue = i.Values[0].Value;
                                                fieldValue = assetField.Value;
                                            }

                                            switch (i.Operator)
                                            {
                                                case "gt":
                                                    if (fieldValue > conditionValue)
                                                    {
                                                        conditionsMetCount++;
                                                    }
                                                    break;
                                                case "gte":
                                                    if (fieldValue >= conditionValue)
                                                    {
                                                        conditionsMetCount++;
                                                    }
                                                    break;
                                                case "lt":
                                                    if (fieldValue < conditionValue)
                                                    {
                                                        conditionsMetCount++;
                                                    }
                                                    break;
                                                case "lte":
                                                    if (fieldValue <= conditionValue)
                                                    {
                                                        conditionsMetCount++;
                                                    }
                                                    break;
                                            }
                                            break;
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
                                Position = c.Position
                            };
                            if (c.Weight > 0)
                            {
                                metCondition.Weight = c.Weight;
                            }
                            if (c.Threshold > 0)
                            {
                                metCondition.Threshold = c.Threshold;
                            }
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

                // Set the measure weight as the default.
                if (!metConditions.SelectedWeight.HasValue)
                {
                    metConditions.SelectedWeight = measure.Weight;
                }
                if (!metConditions.SelectedThreshold.HasValue)
                {
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

        internal decimal AdjustScoreItemWeights(
            List<AllocationDataModel> allMeasures, List<ScoreItem> items)
        {
            decimal scoreValue = 0;

            var rootUids = allMeasures.Where(o => !o.MetricParentAssetUid.HasValue).Select(o => o.MetricAssetUid).ToList();
            
            // Root-level measures
            foreach(var o in items.Where(o => rootUids.Contains(o.MetricAssetUid)))
            {
                o.AdjustedMaxWeight = o.RawMeasureWeight / 
                    items.Where(i => rootUids.Contains(i.MetricAssetUid)).Sum(i => i.RawMeasureWeight);

                decimal totalChildPassingWeights = 0;
                // Child-level measures.
                if (allMeasures.Any(c => c.MetricParentAssetUid == o.MetricAssetUid))
                {
                    var childMeasures = (from item in items 
                                        join m in allMeasures on item.MetricAssetUid equals m.MetricAssetUid
                                        where m.MetricParentAssetUid == o.MetricAssetUid
                                        select item).ToList();

                    foreach (var c in childMeasures)
                    {
                        c.AdjustedMaxWeight = c.RawMeasureWeight / childMeasures.Sum(i => i.RawMeasureWeight);
                        c.AdjustedWeight = c.Value ? c.AdjustedMaxWeight : 0;
                        totalChildPassingWeights += c.AdjustedWeight.Value;
                    }
                }

                var rootMeasure = allMeasures.Single(r => r.MetricAssetUid == o.MetricAssetUid);
                if (rootMeasure.IsGroup)
                {
                    o.AdjustedWeight = totalChildPassingWeights * (o.AdjustedMaxWeight ?? 0);
                }
                else
                {
                    if (o.OverrideAdjustmentPercentage.HasValue)
                    {
                        // Typically applies when deling with a DataQuality measure that is NOT threshold-based.
                        o.AdjustedWeight = (o.AdjustedMaxWeight ?? 0) * (decimal)o.OverrideAdjustmentPercentage.Value;
                    }
                    else
                    {
                        o.AdjustedWeight = o.Value ? (o.AdjustedMaxWeight ?? 0) : 0;
                    }
                }
                scoreValue += o.AdjustedWeight.Value;
            }

            return scoreValue;
        }

        internal SqlBulkCopy CreateBulkCopy(SqlConnection company, SqlTransaction trans, string tableName)
        {
            return new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, trans) {
                BatchSize = 5000,
                DestinationTableName = tableName,
                BulkCopyTimeout = 0
            };
        }
    }
}
