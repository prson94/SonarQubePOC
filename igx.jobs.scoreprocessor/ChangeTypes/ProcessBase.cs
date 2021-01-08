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

                o.AdjustedMaxWeight = Math.Round(o.AdjustedMaxWeight ?? 0, 3, MidpointRounding.AwayFromZero);

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
                        c.AdjustedMaxWeight = Math.Round(c.AdjustedMaxWeight ?? 0, 3, MidpointRounding.AwayFromZero);
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
                o.AdjustedWeight = Math.Round(o.AdjustedWeight.Value, 3, MidpointRounding.AwayFromZero);
                scoreValue += o.AdjustedWeight.Value;
            }

            if (scoreValue > 1) scoreValue = 1; // Catch if the score is more than 100%, for whatever reason. 

            return Math.Round(scoreValue, 3, MidpointRounding.AwayFromZero);
        }

        internal SqlBulkCopy CreateBulkCopy(SqlConnection company, SqlTransaction trans, string tableName)
        {
            return new SqlBulkCopy(company, SqlBulkCopyOptions.TableLock, trans) {
                BatchSize = 5000,
                DestinationTableName = tableName,
                BulkCopyTimeout = 0
            };
        }

        internal DataQualityMeasureQueryModel BuildDataQualityMeasureQueryModel(Guid incomingAllocationUid, Guid assetVersionRollupPathUid)
        {
            var dqQueryDetail = new DataQualityMeasureQueryModel
            {
                AssetVersionRollupPathUid = assetVersionRollupPathUid
            };

            var dqCompany = GetEnvironmentConnection();

            if (dqCompany.State != ConnectionState.Open)
                dqCompany.Open();

            var dqQueryDetails = dqCompany.QueryMultiple(
                "metrics.BuildDataQualityMeasureQuery @incomingAllocationUid, @assetVersionRollupPathUid",
                new { incomingAllocationUid, assetVersionRollupPathUid }
                );
            var resultSqlQueryStatements = dqQueryDetails.Read<string>();
            dqQueryDetail.FilterMatchType = dqQueryDetails.Read<MetricMatchType>().Single();
            var resultFilters = dqQueryDetails.Read<DataQualityMeasureQueryFilterModel>();

            dqQueryDetail.Sql = string.Join("", resultSqlQueryStatements);
            dqQueryDetail.Filters = resultFilters.ToList();

            if (dqQueryDetail.Filters.Count > 0)
            {

                dqQueryDetail.Filters.ForEach(f =>
                {
                    var listFieldQuery = $" in (select FD.AssetID from FieldDetail FD inner join FieldLookupValue LV on LV.FieldTypeID = FD.FieldTypeID and LV.Value = FD.Value and FD.AssetTypeID = {f.AssetTypeID} and FD.FieldTypeID = {f.FieldTypeID} and ";
                    var nonListFieldQuery = $" in (select AssetID from FieldDetail where AssetTypeID = {f.AssetTypeID} and FieldTypeID = {f.FieldTypeID} and ";

                    f.WhereQuery += ((f.Type == "Lookup") ? listFieldQuery : nonListFieldQuery);
                    var queryColumn = ((f.Type == "Lookup") ? "LV.AssetUid" : "FormattedValue");

                    var paramName = $"@P{f.AssetTypeID}_{f.FieldTypeID}";
                    var dbTypeToCastTo = "";
                    switch (f.Type)
                    {
                        case "Date":
                        case "DateTime":
                            dbTypeToCastTo = "datetime";
                            DateTime dt;
                            if (DateTime.TryParse(f.Value, out dt))
                            {
                                f.Parameter = new SqlParameter(paramName, dt);
                            }
                            break;
                        case "Decimal":
                            dbTypeToCastTo = "decimal";
                            decimal dc;
                            if (decimal.TryParse(f.Value, out dc))
                            {
                                f.Parameter = new SqlParameter(paramName, dc);
                            }
                            break;
                        case "Number":
                            dbTypeToCastTo = "bigint";
                            long lg;
                            if (long.TryParse(f.Value, out lg))
                            {
                                f.Parameter = new SqlParameter(paramName, lg);
                            }
                            break;
                        default:
                            if (!string.IsNullOrEmpty(f.Value))
                            {
                                f.Parameter = new SqlParameter(paramName, f.Value);
                            }
                            break;
                    }

                    switch (f.Operator)
                    {
                        case Operator.After:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) > {paramName}";
                            break;
                        case Operator.Before:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) < {paramName}";
                            break;
                        //case Operator.Between:
                        //    break;
                        case Operator.Contains:
                            queryColumn = $"{queryColumn} like '%' + {paramName} + '%'";
                            break;
                        case Operator.EndsWith:
                            queryColumn = $"{queryColumn} like '%' + {paramName}";
                            break;
                        case Operator.Equals:
                            queryColumn = (string.IsNullOrEmpty(dbTypeToCastTo)) ?
                                $"{queryColumn} = {paramName}" :
                                $"try_cast({queryColumn} as {dbTypeToCastTo}) = {paramName}";
                            break;
                        case Operator.GreaterThan:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) > {paramName}";
                            break;
                        case Operator.GreaterThanOrEquals:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) >= {paramName}";
                            break;
                        //case Operator.In:
                        //    break;
                        case Operator.IsFalse:
                            queryColumn = $"coalesce(try_cast({queryColumn} as bit), 1) = 0";
                            break;
                        case Operator.IsTrue:
                            queryColumn = $"coalesce(try_cast({queryColumn} as bit), 0) = 1";
                            break;
                        case Operator.LessThan:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) < {paramName}";
                            break;
                        case Operator.LessThanOrEquals:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) <= {paramName}";
                            break;
                        case Operator.NotContains:
                            queryColumn = $"{queryColumn} not like '%' + {paramName} + '%'";
                            break;
                        case Operator.NotEquals:
                            queryColumn = (string.IsNullOrEmpty(dbTypeToCastTo)) ?
                                $"{queryColumn} <> {paramName}" :
                                $"try_cast({queryColumn} as {dbTypeToCastTo}) <> {paramName}";
                            break;
                        //case Operator.NotIn:
                        //    break;
                        case Operator.NotPopulated:
                            queryColumn = $"{queryColumn} is null";
                            break;
                        case Operator.OnOrAfter:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) >= {paramName}";
                            break;
                        case Operator.OnOrBefore:
                            queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) <= {paramName}";
                            break;
                        case Operator.Populated:
                            queryColumn = $"{queryColumn} is not null";
                            break;
                        case Operator.StartsWith:
                            queryColumn = $"{queryColumn} like {paramName} + '%'";
                            break;
                    }

                    f.WhereQuery += queryColumn + ")";
                });

                var filterSql = " and (" + string.Join(
                    dqQueryDetail.FilterMatchType == MetricMatchType.Any ? " or " : " and ",
                    dqQueryDetail.Filters.Select(f => f.WhereQuery)
                    ) + ") ";

                dqQueryDetail.Sql += filterSql;
            }

            dqCompany.Close();
            dqCompany.Dispose();

            return dqQueryDetail;
        }

        internal List<DataQualityMeasureQueryResultModel> GetDataQualityMeasureQueryResultModels(DataQualityMeasureQueryModel query, Guid assetUid, DateTime minDate, DateTime? maxDate)
        {
            var args = new DynamicParameters();
            args.Add("@AssetUid", assetUid, DbType.Guid);
            args.Add("@MinimumEffectiveDate", minDate, DbType.Date);
            args.Add("@MaximumEffectiveDate", maxDate ?? DateTime.UtcNow, DbType.Date);
            foreach (var p in query.Filters.Where(p => p.Parameter != null).Select(p => p.Parameter))
            {
                args.Add(p.ParameterName, p.Value, p.DbType);
            }

            var dqCompany = GetEnvironmentConnection();

            if (dqCompany.State != ConnectionState.Open)
                dqCompany.Open();

            var list = dqCompany.Query<DataQualityMeasureQueryResultModel>(query.Sql, args).ToList();

            dqCompany.Close();
            dqCompany.Dispose();

            return list;
        }
    }
}
