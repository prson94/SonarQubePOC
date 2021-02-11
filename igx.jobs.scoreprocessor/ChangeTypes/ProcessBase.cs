using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using d360.utils.company;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

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
                measure.Conditions.ForEach(c => {

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
                            if (fieldValue != null && fieldType != null)
                            {
                                if (fieldType.Type == DataType.Lookup.ToString() && fieldType.AllowMultipleValues)
                                {
                                    var fieldValues = fieldValue.Values.Split(',');
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
            var all = new List<AllocationDataModel>(allMeasures);

            var measuresThatAreNotPresent = all.Where(m => !m.IsGroup && !items.Any(i => i.MetricAssetUid == m.MetricAssetUid)).Select(m => m.MetricAssetUid);
            all.RemoveAll(m => measuresThatAreNotPresent.Contains(m.MetricAssetUid));

            var groupsWithoutAChild = all
                .Where(g => !g.MetricParentAssetUid.HasValue  && g.IsGroup  && !all.Any(c => c.MetricParentAssetUid == g.MetricAssetUid))
                .Select(g => g.MetricAssetUid)
                .Distinct()
                .ToList();
            all.RemoveAll(m => groupsWithoutAChild.Contains(m.MetricAssetUid));

            decimal scoreValue = 0;

            var rootUids = all.Where(o => !o.MetricParentAssetUid.HasValue).Select(o => o.MetricAssetUid).ToList();
            
            // Root-level measures
            foreach(var o in items.Where(o => rootUids.Contains(o.MetricAssetUid)))
            {
                o.AdjustedMaxWeight = o.RawMeasureWeight / 
                    items.Where(i => rootUids.Contains(i.MetricAssetUid)).Sum(i => i.RawMeasureWeight);

                o.AdjustedMaxWeight = Math.Round(o.AdjustedMaxWeight ?? 0, 6, MidpointRounding.AwayFromZero);

                decimal totalChildPassingWeights = 0;
                // Child-level measures.
                if (all.Any(c => c.MetricParentAssetUid == o.MetricAssetUid))
                {
                    var childMeasures = (from item in items 
                                        join m in all on item.MetricAssetUid equals m.MetricAssetUid
                                        where m.MetricParentAssetUid == o.MetricAssetUid
                                        select item).ToList();

                    foreach (var c in childMeasures)
                    {
                        c.AdjustedMaxWeight = c.RawMeasureWeight / childMeasures.Sum(i => i.RawMeasureWeight);
                        c.AdjustedMaxWeight = Math.Round(c.AdjustedMaxWeight ?? 0, 6, MidpointRounding.AwayFromZero);
                        if (c.DecimalValue.HasValue)
                        {
                            // Typically applies when deling with a DataQuality measure that is NOT threshold-based.
                            c.AdjustedWeight = (c.AdjustedMaxWeight ?? 0) * (decimal)c.DecimalValue.Value;
                        }
                        else 
                        {
                            c.AdjustedWeight = c.Value ? c.AdjustedMaxWeight : 0;
                        }

                        totalChildPassingWeights += c.AdjustedWeight.Value;
                    }
                }

                var rootMeasure = all.Single(r => r.MetricAssetUid == o.MetricAssetUid);
                if (rootMeasure.IsGroup)
                {
                    o.AdjustedWeight = totalChildPassingWeights * (o.AdjustedMaxWeight ?? 0);
                }
                else
                {
                    if (o.DecimalValue.HasValue)
                    {
                        // Typically applies when deling with a DataQuality measure that is NOT threshold-based.
                        o.AdjustedWeight = (o.AdjustedMaxWeight ?? 0) * (decimal)o.DecimalValue.Value;
                    }
                    else
                    {
                        o.AdjustedWeight = o.Value ? (o.AdjustedMaxWeight ?? 0) : 0;
                        o.DecimalValue = (float)o.AdjustedWeight;
                    }
                }
                o.AdjustedWeight = Math.Round(o.AdjustedWeight.Value, 6, MidpointRounding.AwayFromZero);
                scoreValue += o.AdjustedWeight.Value;
            }

            if (scoreValue > 1) scoreValue = 1; // Catch if the score is more than 100%, for whatever reason. 

            return Math.Round(scoreValue, 6, MidpointRounding.AwayFromZero);
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
