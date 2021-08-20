using d360.core.entities;
using d360.core.entities.Metric;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d360.core.enums;
using d360.model;
using System.Net;
using d360.core;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using System.Data;
using d360.model.DataAccessLayer.repositories;
using d360.core.queue;
using d360.core.exceptions;
using System.Runtime.CompilerServices;
using Microsoft.SqlServer.Server;
using System.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;
using d360.model.helpers;
using System.Globalization;

namespace d360.model.DataAccessLayer
{
    public class MetricsRepository : BaseRepository, IMetricsRepository
    {
        #region Properties/Ctor

        internal ICompanyContext Company;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;

        public MetricsRepository(ICompanyContext context, IQueueSource queueSource, IStorageProvider storageProvider) : base(context)
        {
            this.Company = context;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
        }

        #endregion

        #region Common Sql Segments

        string conditionGroupsJsonSql(string assetVersionAliasedUidColumn)
        {
            return $@"(
                    	select		C.Uid,
									C.Position,
									C.Threshold,
									C.Weight,
									C.MatchType,
									(
										select	CI.Uid,
												CI.ConditionType,
												CI.ConditionFieldTypeID,
                                                FT.Name as ConditionFieldTypeName,
												CI.ConditionIntersectTypeID,
                                                IT.Uid as ConditionIntersectTypeUid,
												CI.Operator,
												JSON_QUERY((SELECT CONCAT('[""',STRING_AGG(STRING_ESCAPE([Value],'JSON'), '"",""'),'""]') FROM metrics.AssetVersionConditionItemValue where	Uid = CI.Uid)) as [Values]
                                        from	metrics.AssetVersionConditionItem CI
                                                left join FieldType FT on FT.ID = CI.ConditionFieldTypeID
                                                left join IntersectType IT on IT.ID = CI.ConditionIntersectTypeID
										where	CI.AssetVersionConditionUid = C.Uid
										for json path
									) as ConditionItems
                    	from		metrics.AssetVersionCondition C
                    	where		C.AssetVersionUid = {assetVersionAliasedUidColumn}
						order by	C.Position
                    	for		json path
                    ) as ConditionGroups";
        }

        string dataQualityDefinitionSql(string assetVersionAliasedDefinitionColumn, string assetVersionAliasedUidColumn)
        {
            return $@"JSON_QUERY((
                        select	P.RollupPathUid as ResultPathUid,
		                        P.FilterMatchType,
                                JSON_VALUE({assetVersionAliasedDefinitionColumn}, '$.DataQuality.ResultOperation') as ResultOperation,
		                        (
		                        select	A.Uid as AssetTypeUid,
				                        F.Name as FieldTypeName,
				                        PF.Operator,
				                        JSON_QUERY((SELECT CONCAT('[""',STRING_AGG(STRING_ESCAPE([Value],'JSON'), '"",""'),'""]') FROM metrics.AssetVersionRollupPathFilterValue where	AssetVersionRollupPathFilterUid = PF.Uid)) as [Values]
                                from	metrics.AssetVersionRollupPathFilter PF
				                        inner join AssetType A on A.ID = PF.AssetTypeID
				                        inner join FieldType F on F.ID = PF.FieldTypeID
		                        where	PF.AssetVersionRollupPathUid = P.Uid
		                        for json path
		                        ) as Filters
                        from	metrics.AssetVersionRollupPath P
                        where	AssetVersionUid = {assetVersionAliasedUidColumn}
                        for json path, without_array_wrapper
                )) as DataQualityDefinition";
        }

        string hasResultsSql(string assetVersionAliasedUidColumn)
        {
            return $@"cast(IIF({assetVersionAliasedUidColumn} = ANY(select AssetVersionUid from metrics.ScoreItem), 1, 0) as bit) as HasResults";
        }

        #endregion

        #region Common Processing Of Measure Data

        void processConditionGroup(IConditionGroupMeasure m)
        {
            m.ConditionGroups.RemoveAll(g => g.ConditionItems == null || g.ConditionItems.Count == 0);
        }
        void processDefinition(IDefinitionMeasure m)
        {
            m.Definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(m.DefinitionJson ?? "{}");
            m.DefinitionJson = null;

            if (m.Definition != null && m.DataQualityDefinition != null)
            {
                if (m.Definition.DataQuality != null)
                {
                    m.Definition.DataQuality = m.DataQualityDefinition;
                }
            }
            m.DataQualityDefinition = null;
        }

        #endregion

        public void DeleteMetric(MetricAsset model)
        {
            var now = DateTime.UtcNow.Date;

            var currentAssetVersion = model.Versions.OrderByDescending(x => x.EffectiveDate).FirstOrDefault();
            currentAssetVersion.State = State.Deleted;
            currentAssetVersion.EffectiveEndDate = now.AddDays(-1);

            model.State = State.Deleted;
            model.UpdatedOn = now;
            var children = Company.Filter<MetricAsset>(x => x.ParentUid != null && x.ParentUid == model.Uid).ToList();
            if (children.Count > 0)
            {
                var childVersions = Company.Filter<MetricAssetVersion>(x => x.Asset.ParentUid != null && x.Asset.ParentUid == model.Uid && x.EffectiveEndDate == null).ToList();
                childVersions.ForEach(v =>
                {
                    v.EffectiveEndDate = now.AddDays(-1);
                });
                children.ForEach(c => c.State = State.Deleted);
            }

            var olderVersions = Company.MetricAssetVersions.Where(x => x.Uid == currentAssetVersion.Uid).ToList();
            if (olderVersions.Count > 0)
            {
                olderVersions.ForEach(x => x.State = State.Deleted);
            }

            Company.SaveChanges();
            Company.CreateMeasureRemovedNotificationExecution(currentAssetVersion);
        }

        public MetricAssetViewDetailModel GetMetricViewModelByUid(Guid uid, DateTime? effectiveDate)
        {
            var model = (
                        from a in Company.MetricAssets
                            .Include("Allocation")
                            .Include("Versions.RollupPaths.Filters.AssetType")
                            .Include("Versions.RollupPaths.Filters.FieldType")
                            .Include("Versions.RollupPaths.Filters.Values")
                            .Include("Versions.Conditions.Items.Values")
                            .Include("Versions.Conditions.Items.ConditionFieldType")
                            .Include("Versions.Conditions.Items.ConditionIntersectType")
                        from v in a.Versions
                        where a.Uid == uid
                        where (
                                (!effectiveDate.HasValue && v.EffectiveEndDate == null) ||
                                (effectiveDate.HasValue && v.EffectiveDate <= effectiveDate.Value && v.EffectiveEndDate >= effectiveDate.Value)
                              )
                        select new MetricAssetViewDetailModel
                        {
                            AllocationUid = a.AllocationUid,
                            ConditionGroups = v.Conditions.Select(g => new MetricAssetVersionConditionViewModel
                            {
                                ConditionItems = g.Items.Select(i => new MetricAssetVersionConditionItemViewModel
                                {
                                    ConditionFieldTypeID = i.ConditionFieldTypeID,
                                    ConditionFieldTypeName = (i.ConditionFieldType != null) ? i.ConditionFieldType.Name : null,
                                    ConditionIntersectTypeID = i.ConditionIntersectTypeID,
                                    ConditionIntersectTypeUid = (i.ConditionIntersectType != null) ? i.ConditionIntersectType.uid : new Nullable<Guid>(),
                                    ConditionType = i.ConditionType,
                                    Operator = i.Operator,
                                    Uid = i.Uid,
                                    Values = i.Values.Select(v => v.Value).ToList()
                                }).ToList(),
                                MatchType = g.MatchType,
                                Position = g.Position,
                                Threshold = g.Threshold,
                                Uid = g.Uid,
                                Weight = g.Weight
                            }).ToList(),
                            DefinitionJson = v.Definition,
                            RollupPaths = v.RollupPaths,
                            Versions = a.Versions.Select(v => new MetricAssetVersionViewModel
                            {
                                ConditionAndOr = v.ConditionAndOr,
                                Description = v.Description,
                                EffectiveDate = v.EffectiveDate,
                                EffectiveEndDate = v.EffectiveEndDate,
                                MatchConditionsOnly = v.MatchConditionsOnly,
                                Name = v.Name,
                                Threshold = v.Threshold,
                                Uid = v.Uid,
                                UpdateFrequency = v.UpdateFrequency,
                                Weight = v.Weight
                            }).ToList(),
                            MatchConditionsOnly = v.MatchConditionsOnly,
                            Description = v.Description,
                            EffectiveDate = v.EffectiveDate,
                            IsGroup = a.IsGroup,
                            Name = v.Name,
                            ParentUid = a.ParentUid,
                            Threshold = v.Threshold,
                            Uid = a.Uid,
                            Weight = v.Weight,
                            AssetTypeUid = a.Allocation.AssetTypeUid,
                            ScoreType = a.Allocation.ScoreType
                        }).FirstOrDefault();

            if (model != null)
            {
                model.Definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(model.DefinitionJson ?? "{}");

                if (model.RollupPaths != null && model.Definition.DataQuality != null)
                {
                    var rollupPath = model.RollupPaths.FirstOrDefault();
                    if (rollupPath != null)
                    {
                        model.Definition.DataQuality.FilterMatchType = rollupPath.FilterMatchType;
                        model.Definition.DataQuality.ResultPathUid = rollupPath.RollupPathUid;
                        if (rollupPath.Filters != null)
                        {
                            model.Definition.DataQuality.Filters = rollupPath.Filters.Select(f => new MetricAssetDefinitionDataQualityFilterViewModel
                            {
                                AssetTypeUid = f.AssetType.uid,
                                FieldTypeName = f.FieldType.Name,
                                Operator = f.Operator,
                                Values = f.Values.Select(v => v.Value).ToList()
                            }).ToList();
                        }
                    }

                }
            }

            return model;
        }

        public MetricAsset GetMetricByUid(Guid uid)
        {
            return Company.GetByUid<MetricAsset>(uid, i => i.Children);
        }

        public MetricAsset GetActiveMetric(Guid uid)
        {
            return Company.Filter<MetricAsset>(i => i.Uid == uid && i.State == State.Active).SingleOrDefault();
        }

        public WorkHttpStatus AddOrUpdateMetrics(MetricAssetEditModel model)
        {
            MetricAsset metricAsset = null;
            MetricAssetVersion metricAssetVersion = null;
            AssetType targetAssetType = null;
            bool isNew = true;
            bool changeWillEffectScore = false;

            var operatorInfos = Operator.After.GetAsList();

            var errorTitle = "Error updating measure";
            if (model == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You are have provided a null metric.");
            }

            isNew = (model.Uid == null || model.Uid == Guid.Empty);

            if (isNew)
            {
                errorTitle = "Error creating measure";
            }

            if (model.Uid != null && model.Uid != Guid.Empty)
            {
                isNew = false;
                metricAsset = Company.GetByUid<MetricAsset>(model.Uid, i => i.Allocation);
                if (metricAsset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, errorTitle, $"Metric with UID {model.Uid} does not exist.");
                }
                Guid assetTypeId = metricAsset.Allocation.AssetTypeUid;
                targetAssetType = Company.Filter<AssetType>(x => x.uid == assetTypeId).SingleOrDefault();
            }
            else
            {
                if (model.AllocationUid != null && model.AllocationUid != Guid.Empty)
                {
                    targetAssetType = (
                                      from al in Company.MetricAllocations
                                      join assettype in Company.AssetTypes on al.AssetTypeUid equals assettype.uid
                                      where al.Uid == model.AllocationUid
                                      select assettype
                                      ).SingleOrDefault();
                }
                else
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must provide a valid AllocationUid.");
                }
            }

            if (isNew)
            {
                changeWillEffectScore = true;
            }

            if (model.Definition != null)
            {
                var definitionJsonToCheck = "";
                try
                {
                    definitionJsonToCheck = model.Definition.AsJson();
                }
                catch
                {
                    model.Definition = new MetricAssetDefinitionViewModel();
                }
                if (definitionJsonToCheck.Length > 4000)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Definition must not exceed 4000 characters.");
                }
                definitionJsonToCheck = null;
            }
            else
            {
                model.Definition = new MetricAssetDefinitionViewModel();
            }

            if (model.Allocation.IsExternallyCalculated)
            {
                if (model.Definition != null)
                {
                    if (model.Definition.DataQuality != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "A measure belonging to an externally calculated score may not have a DataQuality definition.");
                    }
                    else if (model.Definition.Governance != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "A measure belonging to an externally calculated score may not have a Governance definition.");
                    }
                }
            }
            else
            {
                if (model.Weight <= 0 || model.Weight > 1)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Weight must be a value between 0 and 1");
                }
                else if (decimal.Round(model.Weight, 2) != model.Weight)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Weight can have a maximum of 2 decimal places.");
                }
            }

            Func<List<string>, string, string, int?, bool> checkValidValuesByDataType = delegate (List<string> values, string dataType, string lookupObject, int? lookupObjectID)
            {
                var validForType = true;
                StringBuilder sb = new StringBuilder();
                if (values != null)
                {
                    for (int ix = 0; ix < values.Count; ix++)
                    {
                        string v = values[ix];
                        sb.Clear();
                        sb.Append(lookupObject);

                        switch (dataType)
                        {
                            case "Date":
                            case "DateTime":
                                DateTime tempDate;
                                if (DateTime.TryParse(v, out tempDate))
                                {
                                    values[ix] = tempDate.ToUniversalTime().ToString("yyyy-MM-ddT00:00:00.0000Z");
                                }
                                else
                                {
                                    validForType = false;
                                }
                                break;
                            case "Decimal":
                                if (!decimal.TryParse(v, out _))
                                {
                                    validForType = false;
                                }
                                break;
                            case "Lookup":
                                Guid lookupUid;
                                int lookupId;
                                if (Guid.TryParse(v, out lookupUid) && lookupObjectID.HasValue)
                                {
                                    sb.Append("Type");
                                    string ot = sb.ToString();
                                    if (!Company.Filter<AssetDetail>(i => i.Type == ot && i.TypeID == lookupObjectID && i.uid == lookupUid).Any())
                                    {
                                        validForType = false;
                                    }
                                }
                                else if (int.TryParse(v, out lookupId) && lookupObjectID.HasValue)
                                {
                                    sb.Append("Type");
                                    string ot = sb.ToString();
                                    if (!Company.Filter<AssetDetail>(i => i.Type == ot && i.TypeID == lookupObjectID && i.ObjectID == lookupId).Any())
                                    {
                                        validForType = false;
                                    }
                                }
                                else
                                {
                                    validForType = false;
                                }
                                break;
                            case "Number":
                                if (!int.TryParse(v, out _))
                                {
                                    validForType = false;
                                }
                                break;
                            default:
                                continue;
                        }
                    }
                }

                return validForType;
            };

            Func<List<string>, OperatorInfo, string, string> checkValidValuesCount = delegate (List<string> values, OperatorInfo op, string checkType)
            {
                string error = null;

                var valueCount = values != null ? values.Count : 0;
                if (valueCount < op.MinimumValueCount || valueCount > op.MaximumValueCount)
                {
                    error = $"The operator used on your {checkType} does not support {valueCount} values. You must ";
                    if (op.MinimumValueCount == 0 && op.MaximumValueCount == 0)
                    {
                        error += "not provide any values for this operator.";
                    }
                    else if (op.MinimumValueCount == op.MaximumValueCount)
                    {
                        error += $"provide exactly {op.MaximumValueCount} value(s) for this operator.";
                    }
                    else
                    {
                        error += $"provide between {op.MinimumValueCount} and {op.MaximumValueCount} values for this operator.";
                    }
                }
                return error;
            };

            Func<Operator, MetricGovernanceCheckType, WorkHttpStatus> checkOperatorForGovernanceMeasure = delegate (Operator op, MetricGovernanceCheckType check)
            {
                WorkHttpStatus status = null;

                var checkOperatorInfo = op.GetAsInfo();
                if (checkOperatorInfo == null)
                {
                    status = new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The Governance {check.GetDisplayName()} does not support the selected operator.");
                }
                else
                {
                    if (!checkOperatorInfo.AllowedMeasureChecks.Any(t => t.ID == check))
                    {
                        status = new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The Governance {check.GetDisplayName()} does not support the selected operator.");
                    }
                }

                return status;
            };

            // Definition does not apply IF the measure is a group or it is externally calculated.
            if (!model.Allocation.IsExternallyCalculated && !model.IsGroup)
            {
                if (model.Definition == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Definition object property must not be empty.");
                }

                switch (model.Allocation.ScoreType)
                {
                    case ScoreType.DataQuality:
                        #region

                        var dq = model.Definition.DataQuality;
                        if (dq == null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must provide the DataQuality object property under Definition.");
                        }
                        if (model.Definition.Governance != null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not provide a Governance object property under Definition.");
                        }
                        if (model.Definition.DataQuality.ResultPathUid == Guid.Empty)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "ResultPathUid may not be an empty identifier.");
                        }
                        else
                        {
                            if (!Company.Query<bool>("select cast(iif(count(1)>0,1,0) as bit) from metrics.RollupPath where Uid = @ResultPathUid and AssetTypeID = @ID and [State] = 1", new { model.Definition.DataQuality.ResultPathUid, targetAssetType.ID }).Single())
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"ResultPathUid with the specified identifier {model.Definition.DataQuality.ResultPathUid} does not exist or does not correspond to the asset type you are scoring.");
                            }
                        }
                        if (model.Allocation.IsThresholdBased)
                        {
                            if (model.Threshold.HasValue)
                            {
                                if (model.Threshold <= 0 || model.Threshold > 1)
                                {
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Threshold must be a value between 0 and 1");
                                }
                            }
                            else
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Because the score definition is threshold-based, you must provide a valid Threshold between 0 and 1.");
                            }
                        }
                        else
                        {
                            if (model.Threshold.HasValue)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must not provide a value for the Threshold property since the measure you are adding does not belong to a threshold-based score definition.");
                            }
                        }
                        if (dq.Filters != null)
                        {
                            if (dq.Filters.Count > 0)
                            {
                                if (dq.Filters.Any(f => f.AssetTypeUid == Guid.Empty))
                                {
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must provide valid (non-empty) asset type identifiers when using Rule Result Filters.");
                                }

                                if (dq.Filters.Any(f => string.IsNullOrEmpty(f.FieldTypeName)))
                                {
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must provide valid (non-empty) field names (API Name of the field definition) when using Rule Result Filters.");
                                }

                                var dqFilterFieldTypes = Company.Query<dynamic>(@"
select  distinct
	A.ID as AssetTypeID, A.Uid as AssetTypeUid,
	F.ID as FieldTypeID, F.Name as FieldTypeName, F.Type,
	F.AllowMultipleValues, F.LookupObjectType, F.LookupObjectID
from	metrics.RollupPath P
	inner join metrics.RollupPathSegment S on S.RollupPathUid = P.Uid and P.Uid = @ResultPathUid
	inner join AssetType A on A.ID = S.AssetTypeID
	inner join FieldType F on F.AssetTypeID = S.AssetTypeID", new { dq.ResultPathUid }).ToList();

                                bool isSuccess = true;
                                var dqFilterErrorMessage = "";
                                dq.Filters.ForEach(f =>
                                {
                                    var dqFilterFieldType = dqFilterFieldTypes.SingleOrDefault(o => o.AssetTypeUid == f.AssetTypeUid && o.FieldTypeName == f.FieldTypeName);

                                    if (dqFilterFieldType != null)
                                    {
                                        f.AssetTypeID = dqFilterFieldType.AssetTypeID;
                                        f.FieldTypeID = dqFilterFieldType.FieldTypeID;

                                        var dqFilterOperatorInfo = operatorInfos.SingleOrDefault(o => o.ID == f.Operator);
                                        if (dqFilterOperatorInfo == null)
                                        {
                                            isSuccess = false;
                                            dqFilterErrorMessage += $"A DataQuality filter uses an invalid operator. ";
                                        }
                                        else
                                        {
                                            if (!dqFilterOperatorInfo.AllowedDataTypes.Any(dt => dt.Name == dqFilterFieldType.Type))
                                            {
                                                isSuccess = false;
                                                dqFilterErrorMessage += $"A DataQuality filter uses an operator ({dqFilterOperatorInfo.Name}) that is not valid for fields with a data type of {dqFilterFieldType.Type}. ";
                                            }
                                            var dqValueCountErrorMessage = checkValidValuesCount(f.Values, dqFilterOperatorInfo, "Data Quality Result Filter");
                                            if (!string.IsNullOrEmpty(dqValueCountErrorMessage))
                                            {
                                                isSuccess = false;
                                                dqFilterErrorMessage = dqValueCountErrorMessage;
                                            }
                                        }

                                        var dqValuesValidForType = checkValidValuesByDataType(f.Values, dqFilterFieldType.Type, dqFilterFieldType.LookupObjectType, dqFilterFieldType.LookupObjectID);
                                        if (!dqValuesValidForType)
                                        {
                                            isSuccess = false;
                                            dqFilterErrorMessage += $"One or more values used on your DataQuality filters are not supported by the data type ({dqFilterFieldType.Type}) of the referenced field. ";
                                        }
                                    }
                                    else
                                    {
                                        isSuccess = false;
                                        dqFilterErrorMessage += "You have referenced an invalid field as part of your DataQuality filters, either because it was not found or is using an improper operator.";
                                    }


                                });

                                if (!isSuccess)
                                {
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, dqFilterErrorMessage);
                                }
                            }
                        }

                        #endregion
                        break;
                    case ScoreType.Governance:
                        #region

                        var gov = model.Definition.Governance;
                        if (gov == null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You must provide the Governance object property under Definition.");
                        }
                        if (model.Definition.DataQuality != null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not provide a DataQuality object property under Definition.");
                        }
                        var checkObjectCorrespondsToCheckErrorMessage = gov.ValidateCheckObjectCorrespondsToCheck();
                        if (!string.IsNullOrEmpty(checkObjectCorrespondsToCheckErrorMessage))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, checkObjectCorrespondsToCheckErrorMessage);
                        }

                        if (gov.External != null)
                        {
                            if (!string.IsNullOrEmpty(gov.External.Instructions) && gov.External.Instructions.Length > 500)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, $"Error " + ((isNew) ? "adding" : "updating") + " metric", "Instructions for External check cannot be longer than 500 characters.");
                            }
                        }
                        else if (gov.Field != null)
                        {
                            var governanceCheckFieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == targetAssetType.ID && x.Name == gov.Field.FieldTypeName);
                            if (governanceCheckFieldType == null)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The field type name ({gov.Field.FieldTypeName}) used on your Governance Field Check does not exist on this asset type.");
                            }

                            var operatorCheckStatus = checkOperatorForGovernanceMeasure(gov.Field.Operator, gov.Check);
                            if (operatorCheckStatus != null)
                            {
                                return operatorCheckStatus;
                            }

                            var operatorInfo = gov.Field.Operator.GetAsInfo();
                            if (!operatorInfo.AllowedDataTypes.Any(t => t.Name == governanceCheckFieldType.Type))
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The field type name ({model.Definition.Governance.Field.FieldTypeName}) used on your Governance Field Check does not support the selected operator.");
                            }

                            var governanceFieldCheckValueCountErrorMessage = checkValidValuesCount(gov.Field.Values, operatorInfo, "Governance Field Check");
                            if (!string.IsNullOrEmpty(governanceFieldCheckValueCountErrorMessage))
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, governanceFieldCheckValueCountErrorMessage);
                            }

                            var governanceFieldCheckValuesValidForType = checkValidValuesByDataType(gov.Field.Values, governanceCheckFieldType.Type, governanceCheckFieldType.LookupObjectType, governanceCheckFieldType.LookupObjectID);
                            if (!governanceFieldCheckValuesValidForType)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"One or more values used on your Governance Field Check are not supported by the data type ({governanceCheckFieldType.Type}) of the referenced field.");
                            }
                        }
                        else if (gov.Owner != null)
                        {
                            var governanceCheckResponsibilityTypeExists = (
                                                                            from r in Company.ResponsibilityTypes
                                                                            from a in r.ResponsibilityTypeRelations
                                                                            where r.UID == gov.Owner.ResponsibilityTypeUid
                                                                            where a.ObjectType == targetAssetType.Object
                                                                            where a.ObjectID == targetAssetType.ObjectID
                                                                            select r
                                                                            ).Any();
                            if (!governanceCheckResponsibilityTypeExists)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The Uid Governance Owner Check does not correspond to a known responsibility type.");
                            }

                            var operatorCheckStatus = checkOperatorForGovernanceMeasure(gov.Owner.Operator, gov.Check);
                            if (operatorCheckStatus != null)
                            {
                                return operatorCheckStatus;
                            }
                        }
                        else if (gov.Predicate != null)
                        {
                            var governanceCheckPredicateExists = (
                                                                    from p in Company.Predicates
                                                                    join r in Company.IntersectTypeDetails on p.ID equals r.PredicateID
                                                                    where p.UID == gov.Predicate.PredicateUid
                                                                    where (r.SubjectAssetTypeID == targetAssetType.ID || r.ObjectAssetTypeID == targetAssetType.ID)
                                                                    select r
                                                                    ).Any();
                            if (!governanceCheckPredicateExists)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The Uid Governance Predicate Check does not correspond to a known predicate.");
                            }

                            var operatorCheckStatus = checkOperatorForGovernanceMeasure(gov.Predicate.Operator, gov.Check);
                            if (operatorCheckStatus != null)
                            {
                                return operatorCheckStatus;
                            }
                        }
                        else if (gov.Relation != null)
                        {
                            var governanceCheckIntersectTypeExists = (
                                                                        from r in Company.IntersectTypeDetails
                                                                        where r.Uid == gov.Relation.IntersectTypeUid
                                                                        where (r.SubjectAssetTypeID == targetAssetType.ID || r.ObjectAssetTypeID == targetAssetType.ID)
                                                                        select r
                                                                        ).Any();
                            if (!governanceCheckIntersectTypeExists)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"The Uid Governance Relation Check does not correspond to a known relationship type.");
                            }

                            var operatorCheckStatus = checkOperatorForGovernanceMeasure(gov.Relation.Operator, gov.Check);
                            if (operatorCheckStatus != null)
                            {
                                return operatorCheckStatus;
                            }
                            var operatorInfo = gov.Relation.Operator.GetAsInfo();
                            var relationCheckValueCountErrorMessage = checkValidValuesCount(gov.Relation.Values, operatorInfo, "Governance Relation Check");
                            if (!string.IsNullOrEmpty(relationCheckValueCountErrorMessage))
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, relationCheckValueCountErrorMessage);
                            }

                            var governanceRelationCheckValuesValidForType = true;
                            if (gov.Relation.Values != null)
                            {
                                gov.Relation.Values.ForEach(v =>
                                {
                                    if (!Guid.TryParse(v, out _))
                                    {
                                        governanceRelationCheckValuesValidForType = false;
                                    }
                                });
                            }

                            if (!governanceRelationCheckValuesValidForType)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"One or more values used on your Governance Relation Check are not supported. All values must correspond to existing asset Uids.");
                            }
                        }

                        #endregion
                        break;
                    default:
                        throw new ArgumentException(string.Format("Allocation ScoreType not recognised.  Value {0}", model.Allocation.ScoreType));
                }
            }

            // Remove any time component from the effective date.
            model.EffectiveDate = model.EffectiveDate.Date;

            if (!model.IsGroup)
            {
                foreach (var group in model.ConditionGroups)
                {
                    #region Condition group validation
                    var groupErrorPrefix = $"Condition group in position {group.Position}";
                    if (model.Allocation.IsThresholdBased)
                    {
                        if (group.Threshold.HasValue)
                        {
                            if (group.Threshold <= 0 || group.Threshold > 1)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"{groupErrorPrefix} must contain a Threshold value between 0 and 1");
                            }
                        }
                        else
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"Because the score definition is threshold-based, {groupErrorPrefix} must contain a valid Threshold between 0 and 1.");
                        }
                    }
                    else
                    {
                        if (group.Threshold.HasValue)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"{groupErrorPrefix} must not provide a value for the Threshold property since the measure you are adding does not belong to a threshold-based score definition.");
                        }
                    }
                    #endregion

                    foreach (var condition in group.ConditionItems)
                    {
                        var fieldType = new FieldType();

                        // Remove null values
                        condition.Values.RemoveAll(v => string.IsNullOrEmpty(v));
                        //Trim remaining values.
                        condition.Values.ForEach(v => v.Trim());

                        if (!string.IsNullOrEmpty(condition.ConditionFieldTypeName))
                        {
                            condition.ConditionFieldTypeName = condition.ConditionFieldTypeName.Trim();
                            fieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == targetAssetType.ID && x.Name == condition.ConditionFieldTypeName);
                        }

                        if (fieldType == null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "FieldType does not exist!");
                        }

                        if (targetAssetType.Object != fieldType.Object || targetAssetType.ObjectID != fieldType.ObjectID)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "Invalid FieldType for this asset!");
                        }

                        condition.ConditionFieldTypeID = fieldType.ID;

                        var operatorInfo = condition.Operator.GetAsInfo();
                        if (!operatorInfo.AllowedDataTypes.Any(dt => dt.Name == fieldType.Type))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"A condition uses an operator ({operatorInfo.Name}) that is not valid for fields with a data type of {fieldType.Type}. ");
                        }

                        var conditionValuesValidForType = checkValidValuesByDataType(condition.Values, fieldType.Type, fieldType.LookupObjectType, fieldType.LookupObjectID);
                        if (!conditionValuesValidForType)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, $"One or more values used on your condition is not supported by the data type ({fieldType.Type}) of the referenced field. ");
                        }

                        var conditionValueCountErrorMessage = checkValidValuesCount(condition.Values, operatorInfo, "Condition Value");
                        if (!string.IsNullOrEmpty(conditionValueCountErrorMessage))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, conditionValueCountErrorMessage);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.Name) && model.Name.Length > 250)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, $"Error " + ((isNew) ? "adding" : "updating") + " metric", "Name cannot be longer than 250 characters.");
            }

            int metricExistsCount = 0;
            var metricCountSql = $@"select count(1) from (
select A.Uid, max(V.EffectiveDate) as EffectiveDate
from metrics.Asset A inner join metrics.AssetVersion V on V.AssetUid = A.Uid and A.State = 1 and A.AllocationUid = @AllocationUid {(model.Uid != Guid.Empty ? "and A.Uid <> @Uid" : "")} and lower(V.Name) = @n and {(model.ParentUid.HasValue && model.ParentUid != Guid.Empty ? "A.ParentUid = @p" : "A.ParentUid is null")} group by A.Uid) O";
            metricExistsCount = Company.Query<int>(metricCountSql, new { n = model.Name.Trim().ToLower(), p = model.ParentUid, model.AllocationUid, model.Uid }).Single();

            if (metricExistsCount > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle,
                    (model.ParentUid.HasValue && model.ParentUid != Guid.Empty) ?
                    "You may not add a metric with the same name under the same grouping." :
                    $"Measure with name '{model.Name}' already exists.");
            }

            if (Company.Connection.State != ConnectionState.Open)
            {
                Company.Connection.Open();
            }

            using (var trans = Company.Connection.BeginTransaction())
            {
                try
                {
                    #region Asset

                    if (isNew)
                    {
                        metricAsset = new MetricAsset
                        {
                            Uid = Guid.NewGuid(),
                            AllocationUid = model.AllocationUid,
                            IsGroup = model.IsGroup,
                            State = State.Active,
                            CreatedBy = Company.CurrentResourceID,
                            CreatedOn = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID,
                            UpdatedOn = DateTime.UtcNow
                        };

                        if (model.ParentUid != Guid.Empty && model.ParentUid.HasValue)
                        {
                            var parentExists = Company.Connection
                                .Query<bool>(
                                    "select cast(iif(count(1) > 0, 1, 0) as bit) from metrics.Asset where AllocationUid = @a and Uid = @p",
                                    new { a = model.AllocationUid, p = model.ParentUid.Value }, transaction: trans)
                                .Single();

                            if (!parentExists)
                            {
                                throw new WorkStatusException(HttpStatusCode.NotFound, "Parent metric not found.");
                            }

                            metricAsset.ParentUid = model.ParentUid;
                        }

                        Company.Connection.Execute(
                            "insert into metrics.Asset (Uid, ParentUid, IsGroup, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, AllocationUid) values (@Uid, @ParentUid, @IsGroup, @CreatedOn, @CreatedBy, @UpdatedOn, @UpdatedBy, @AllocationUid)",
                            metricAsset, transaction: trans
                        );
                    }
                    else
                    {
                        metricAsset.UpdatedBy = Company.CurrentResourceID;
                        metricAsset.UpdatedOn = DateTime.Now;

                        var childMetricCount = Company.Connection.Query<int>(
                            "select count(1) from metrics.Asset where ParentUid = @Uid and State = 1",
                            new { model.Uid }, transaction: trans).Single();

                        var existingAllVersionsResultCount = Company.Connection.Query<int>(
                            "select count(1) from metrics.ScoreItem I inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid and V.AssetUid = @Uid",
                            new { metricAsset.Uid }, transaction: trans).Single();

                        // If results, then you cannot change. 
                        if (existingAllVersionsResultCount > 0 && model.IsGroup && !metricAsset.IsGroup)
                        {
                            throw new WorkStatusException(HttpStatusCode.BadRequest, $"You may not convert this metric to a grouping as there are results already associated with it.");
                        }

                        // If has child metrics, you cannot change.
                        if (childMetricCount > 0 && !model.IsGroup)
                        {
                            throw new WorkStatusException(HttpStatusCode.BadRequest, $"You may not convert this grouping to a metric as there are child metrics already associated with it.");
                        }

                        // If made it past above, then we can save the grouping change.
                        metricAsset.IsGroup = model.IsGroup;

                        Company.Connection.Execute(
                            "update metrics.Asset set IsGroup = @IsGroup, UpdatedOn = @UpdatedOn, UpdatedBy = @UpdatedBy where Uid = @Uid",
                            metricAsset, transaction: trans
                        );
                    }

                    #endregion

                    #region Validation -> No Backdating EffectiveDate

                    var effectiveDate = model.EffectiveDate == DateTime.MinValue ? DateTime.UtcNow : model.EffectiveDate;
                    var maxEffectiveDate = Company.Connection
                        .Query<DateTime?>("select max(EffectiveDate) from metrics.AssetVersion where AssetUid = @Uid", new { model.Uid }, transaction: trans)
                        .SingleOrDefault();

                    if (maxEffectiveDate.HasValue)
                    {
                        if (maxEffectiveDate.Value > effectiveDate.Date)
                        {
                            throw new WorkStatusException(HttpStatusCode.BadRequest, $"You may not backdate the effective date for this metric. You must provide date more recent than {maxEffectiveDate.Value.ToShortDateString()}");
                        }
                    }

                    #endregion

                    #region Version

                    var metricAssetVersionJsonFragments = Company.Connection.Query<string>(@"
select	*,
	(
		select	G.*,
				(
					select	I.*,
							(
								select	*
								from	metrics.AssetVersionConditionItemValue
								where	Uid = I.Uid
								for json path
							) as [Values]
					from	metrics.AssetVersionConditionItem I
					where	I.AssetVersionConditionUid = G.Uid
					for json path
				) as Items
		from	metrics.AssetVersionCondition G
		where	G.AssetVersionUid = V.Uid
		for json path
	) as Conditions
from	metrics.AssetVersion V
where	V.AssetUid = @Uid
	and V.EffectiveDate = @effectiveDate
for json path, WITHOUT_ARRAY_WRAPPER", new { model.Uid, effectiveDate }, transaction: trans);
                    metricAssetVersion = JsonConvert.DeserializeObject<MetricAssetVersion>(string.Join("", metricAssetVersionJsonFragments));
                    string newConditionHash = model.CurrentConditionHash;

                    if (model.Allocation.IsExternallyCalculated)
                    {
                        // Since this is external, clear out the definition.
                        if (model.Definition == null)
                        {
                            model.Definition = new MetricAssetDefinitionViewModel();
                        }
                        model.Definition.DataQuality = null;
                        model.Definition.Governance = null;
                    }

                    Action setVersionUpdateFrequency = () =>
                    {
                        metricAssetVersion.UpdateFrequency = MetricUpdateFrequency.None;

                        if (!model.Allocation.IsExternallyCalculated && !model.IsGroup)
                        {
                            if (model.Definition.DataQuality != null)
                            {
                                metricAssetVersion.UpdateFrequency = MetricUpdateFrequency.Weekly;
                            }
                            else if (model.Definition.Governance != null)
                            {
                                if (model.Definition.Governance.Check == MetricGovernanceCheckType.External)
                                {
                                    if (model.Definition.Governance.External != null)
                                    {
                                        metricAssetVersion.UpdateFrequency = model.Definition.Governance.External.UpdateFrequency;
                                    }
                                }
                            }
                        }
                    };


                    var definitionToSave = model.Definition.CloneThis();
                    if (model.Allocation.ScoreType == ScoreType.DataQuality && definitionToSave.DataQuality != null && !model.Allocation.IsExternallyCalculated)
                    {
                        // These are saved in a table.
                        definitionToSave.DataQuality.FilterMatchType = null;
                        definitionToSave.DataQuality.Filters = null;
                        definitionToSave.DataQuality.ResultPathUid = null;
                    }

                    if (metricAssetVersion == null)
                    {
                        metricAssetVersion = new MetricAssetVersion
                        {
                            Uid = Guid.NewGuid(),
                            AssetUid = metricAsset.Uid,
                            Name = model.Name,
                            Description = model.Description,
                            CreatedBy = Company.CurrentResourceID,
                            CreatedOn = DateTime.UtcNow,
                            MatchConditionsOnly = model.MatchConditionsOnly,
                            EffectiveDate = effectiveDate,
                            Threshold = model.Threshold,
                            Weight = model.Weight,
                            State = metricAsset.State,
                            EffectiveEndDate = null,
                            Definition = definitionToSave.AsJson(),
                            UpdateFrequency = MetricUpdateFrequency.None
                        };

                        setVersionUpdateFrequency();

                        Company.Connection.Execute(
                            "insert into metrics.AssetVersion (AssetUid, EffectiveDate, Weight, ConditionAndOr, CreatedOn, CreatedBy, EffectiveEndDate, [State], Uid, Name, Description, Threshold, UpdateFrequency, MatchConditionsOnly, Definition) values (@AssetUid, @EffectiveDate, @Weight, @ConditionAndOr, @CreatedOn, @CreatedBy, @EffectiveEndDate, @State, @Uid, @Name, @Description, @Threshold, @UpdateFrequency, @MatchConditionsOnly, @Definition)",
                            metricAssetVersion, transaction: trans
                        );

                        // End-date the now previous version, if any.
                        var existingAssetVersions = Company.Connection.Query<MetricAssetVersion>(
                            "select * from metrics.AssetVersion where AssetUid = @Uid and Uid <> @VersionUid and EffectiveEndDate is null order by EffectiveDate desc",
                            new { metricAsset.Uid, VersionUid = metricAssetVersion.Uid },
                            transaction: trans
                        ).ToList();
                        for (var i = 0; i < existingAssetVersions.Count; i++)
                        {
                            if (i == 0)
                            {
                                var endDateToUse = (i == 0) ? effectiveDate : existingAssetVersions[i - 1].EffectiveDate;
                                endDateToUse = endDateToUse.AddDays(-1);
                                existingAssetVersions[i].EffectiveEndDate = endDateToUse;
                                Company.Connection.Execute("update metrics.AssetVersion set EffectiveEndDate = @EffectiveEndDate where Uid = @Uid", existingAssetVersions[i], transaction: trans);
                            }
                        }

                        changeWillEffectScore = true; //the fact that you are adding a new version means you should recalculate.
                    }
                    else
                    {
                        var existingVersionResultCount = Company.Connection.Query<int>("select count(1) from metrics.ScoreItem where AssetVersionUid = @Uid", new { metricAssetVersion.Uid }, transaction: trans).Single();
                        var existingDefinition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(metricAssetVersion.Definition ?? "{}");
                        var existingDefinitionHash = existingDefinition.GetHashValue();
                        var newDefinitionHash = model.Definition.GetHashValue();
                        if (metricAssetVersion.Conditions == null)
                        {
                            metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();
                        }
                        var existingHashItems = (
                                                from g in metricAssetVersion.Conditions
                                                from c in (g.Items ?? new List<MetricAssetVersionConditionItem>())
                                                from v in (c.Values ?? new List<MetricAssetVersionConditionItemValue>())
                                                orderby g.Position, c.ConditionFieldTypeID, c.ConditionIntersectTypeID, v.Value
                                                select $"{g.MatchType};{g.Position};{g.Weight};{c.ConditionFieldTypeID};{c.ConditionIntersectTypeID};{c.ConditionType};{c.Operator};{v.Value}"
                                                ).ToList();
                        var existingConditionHash = string.Join("|", existingHashItems);
                        existingConditionHash = existingConditionHash.GetD3sHashString();

                        // Only validate if there any existing results for this metric. If not, do not worry about it.
                        if (existingVersionResultCount > 0)
                        {
                            if (metricAssetVersion.Weight != model.Weight)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not alter the weight of this metric without also altering its effective date.");
                            }

                            if (metricAssetVersion.MatchConditionsOnly != model.MatchConditionsOnly)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not alter the condition type of this metric without also altering its effective date.");
                            }

                            if (existingDefinitionHash != newDefinitionHash)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not alter the definition of this metric without also altering its effective date.");
                            }

                            if (newConditionHash != existingConditionHash)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, errorTitle, "You may not alter the conditions of this metric without also altering its effective date.");
                            }
                        }

                        // Will existing scores be effected?
                        if (metricAssetVersion.Weight != model.Weight)
                        {
                            changeWillEffectScore = true;
                        }
                        if (metricAssetVersion.MatchConditionsOnly != model.MatchConditionsOnly)
                        {
                            changeWillEffectScore = true;
                        }
                        if (existingDefinitionHash != newDefinitionHash)
                        {
                            changeWillEffectScore = true;
                        }
                        if (newConditionHash != existingConditionHash)
                        {
                            changeWillEffectScore = true;
                        }


                        // Set the properties.
                        metricAssetVersion.Name = model.Name;
                        metricAssetVersion.Description = model.Description;
                        metricAssetVersion.MatchConditionsOnly = model.MatchConditionsOnly;
                        metricAssetVersion.Threshold = model.Threshold;
                        metricAssetVersion.Weight = model.Weight;

                        if (metricAsset.IsGroup && model.Allocation.ScoreType == ScoreType.Governance && definitionToSave.Governance != null)
                        {
                            // Be sure to set the definition for group, no extraneous bad data.
                            definitionToSave.Governance.External = null;
                            definitionToSave.Governance.Field = null;
                            definitionToSave.Governance.Owner = null;
                            definitionToSave.Governance.Predicate = null;
                            definitionToSave.Governance.Relation = null;
                        }
                        metricAssetVersion.Definition = definitionToSave.AsJson();

                        setVersionUpdateFrequency();

                        Company.Connection.Execute(
                            "update metrics.AssetVersion set Name = @Name, Description = @Description, Definition = @Definition, UpdateFrequency = @UpdateFrequency, MatchConditionsOnly = @MatchConditionsOnly, Threshold = @threshold, Weight = @Weight where Uid = @Uid",
                            metricAssetVersion, transaction: trans);
                    }

                    #endregion

                    #region Process data quality rule filtering

                    if (model.Allocation.ScoreType == ScoreType.DataQuality)
                    {
                        if (!isNew)
                        {
                            Company.Connection.Execute("delete metrics.AssetVersionRollupPath where AssetVersionUid = @Uid", new { metricAssetVersion.Uid }, transaction: trans);
                        }

                        if (!model.IsGroup && model.Definition.DataQuality != null)
                        {
                            var assetVersionRollupPath = new MetricAssetVersionRollupPath
                            {
                                AssetVersionUid = metricAssetVersion.Uid,
                                FilterMatchType = model.Definition.DataQuality.FilterMatchType.Value,
                                RollupPathUid = model.Definition.DataQuality.ResultPathUid.Value,
                                Uid = Guid.NewGuid()
                            };

                            var assetVersionRollupPathFilters = new List<MetricAssetVersionRollupPathFilter>();
                            var assetVersionRollupPathFilterValues = new List<MetricAssetVersionRollupPathFilterValue>();

                            if (model.Definition.DataQuality.Filters.Count > 0)
                            {
                                model.Definition.DataQuality.Filters.ForEach(f =>
                                {
                                    var assetVersionRollupPathFilter = new MetricAssetVersionRollupPathFilter
                                    {
                                        AssetTypeID = f.AssetTypeID,
                                        AssetVersionRollupPathUid = assetVersionRollupPath.Uid,
                                        FieldTypeID = f.FieldTypeID,
                                        Operator = f.Operator,
                                        Uid = Guid.NewGuid()
                                    };
                                    assetVersionRollupPathFilters.Add(assetVersionRollupPathFilter);

                                    f.Values.ForEach(v =>
                                    {
                                        assetVersionRollupPathFilterValues.Add(
                                            new MetricAssetVersionRollupPathFilterValue
                                            {
                                                AssetVersionRollupPathFilterUid = assetVersionRollupPathFilter.Uid,
                                                Value = v
                                            }
                                        );
                                    });
                                });
                            }

                            Company.Connection.Execute("insert into metrics.AssetVersionRollupPath (Uid, RollupPathUid, AssetVersionUid, FilterMatchType) values (@Uid, @RollupPathUid, @AssetVersionUid, @FilterMatchType)", assetVersionRollupPath, transaction: trans);

                            assetVersionRollupPathFilters.ForEach(f =>
                            {
                                Company.Connection.Execute("insert into metrics.AssetVersionRollupPathFilter (Uid, AssetVersionRollupPathUid, AssetTypeID, FieldTypeID, Operator) values (@Uid, @AssetVersionRollupPathUid, @AssetTypeID, @FieldTypeID, @Operator)", f, transaction: trans);
                            });

                            assetVersionRollupPathFilterValues.ForEach(v =>
                            {
                                Company.Connection.Execute("insert into metrics.AssetVersionRollupPathFilterValue (AssetVersionRollupPathFilterUid, [Value]) values (@AssetVersionRollupPathFilterUid, @Value)", v, transaction: trans);
                            });
                        }
                    }

                    #endregion

                    #region Process conditions for ADDs or UPDATEs

                    if (model.IsGroup)
                    {
                        if (!isNew)
                        {
                            Company.Connection.Execute("delete metrics.AssetVersionCondition where AssetVersionUid = @Uid", new { metricAssetVersion.Uid }, transaction: trans);
                        }
                    }
                    else
                    {
                        if (model.ConditionGroups.Count > 0)
                        {
                            #region DataTable creation

                            var groups = new DataTable();
                            groups.Columns.Add("AssetVersionUid", typeof(Guid));
                            groups.Columns.Add("Uid", typeof(Guid));
                            groups.Columns.Add("MatchType", typeof(int));
                            groups.Columns.Add("Position", typeof(int));
                            groups.Columns.Add("Threshold", typeof(float));
                            groups.Columns.Add("Weight", typeof(decimal));

                            var items = new DataTable();
                            items.Columns.Add("AssetVersionConditionUid", typeof(Guid));
                            items.Columns.Add("Uid", typeof(Guid));
                            items.Columns.Add("ConditionType", typeof(int));
                            items.Columns.Add("ConditionFieldTypeID", typeof(int));
                            items.Columns.Add("ConditionIntersectTypeID", typeof(int));
                            items.Columns.Add("Operator", typeof(string));

                            var values = new DataTable();
                            values.Columns.Add("Uid", typeof(Guid));
                            values.Columns.Add("Value", typeof(string));

                            #endregion

                            if (metricAssetVersion.Conditions == null)
                            {
                                metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();
                            }

                            model.ConditionGroups.ForEach(group =>
                            {
                                if (group.ConditionItems.Count > 0)
                                {
                                    var usedFieldTypeIDs = new List<int>();
                                    var usedIntersectTypeIDs = new List<int>();

                                    var dbGroup = metricAssetVersion.Conditions.SingleOrDefault(i => i.Uid == group.Uid);

                                    bool okToSendGroup = false;

                                    if (dbGroup == null)
                                    {
                                        dbGroup = new MetricAssetVersionCondition { Uid = Guid.NewGuid() };
                                    }
                                    if (dbGroup.Items == null)
                                    {
                                        dbGroup.Items = new List<MetricAssetVersionConditionItem>();
                                    }

                                    dbGroup.AssetVersionUid = metricAssetVersion.Uid;
                                    dbGroup.MatchType = group.MatchType;
                                    dbGroup.Position = group.Position;
                                    dbGroup.Threshold = group.Threshold;
                                    dbGroup.Weight = group.Weight;

                                    group.ConditionItems.ForEach(item =>
                                    {
                                        var dbItem = dbGroup.Items.SingleOrDefault(i => (item.Uid != Guid.Empty) && (i.Uid == item.Uid));
                                        if (dbItem == null)
                                        {
                                            dbItem = new MetricAssetVersionConditionItem { Uid = Guid.NewGuid(), AssetVersionConditionUid = dbGroup.Uid };
                                        }

                                        bool okToSendItem = (item.Operator.GetAsInfo().MinimumValueCount == 0) || ((item.Operator.GetAsInfo().MinimumValueCount > 0) && item.Values.Any(i => !string.IsNullOrEmpty(i)));

                                        dbItem.ConditionType = item.ConditionType;
                                        dbItem.Operator = item.Operator;

                                        if (item.ConditionFieldTypeID.HasValue)
                                        {
                                            // Only one of the specific field per condition group.
                                            if (usedFieldTypeIDs.Contains(item.ConditionFieldTypeID.Value))
                                            {
                                                okToSendItem = false;
                                            }
                                            else
                                            {
                                                dbItem.ConditionFieldTypeID = item.ConditionFieldTypeID.Value;
                                                dbItem.ConditionIntersectTypeID = null;
                                                usedFieldTypeIDs.Add(item.ConditionFieldTypeID.Value);
                                            }
                                        }
                                        else if (item.ConditionIntersectTypeID.HasValue)
                                        {
                                            // Only one of the specific relationship per condition group.
                                            if (usedIntersectTypeIDs.Contains(item.ConditionFieldTypeID.Value))
                                            {
                                                okToSendItem = false;
                                            }
                                            else
                                            {
                                                dbItem.ConditionFieldTypeID = null;
                                                dbItem.ConditionIntersectTypeID = item.ConditionIntersectTypeID;
                                                usedIntersectTypeIDs.Add(item.ConditionFieldTypeID.Value);
                                            }
                                        }

                                        if (okToSendItem)
                                        {
                                            item.Values.ForEach(value =>
                                            {
                                                if (!string.IsNullOrEmpty(value))
                                                {
                                                    var valueRow = values.NewRow();
                                                    valueRow["Uid"] = dbItem.Uid;
                                                    valueRow["Value"] = value.Trim();
                                                    values.Rows.Add(valueRow);
                                                }
                                            });

                                            var itemRow = items.NewRow();
                                            itemRow["AssetVersionConditionUid"] = dbItem.AssetVersionConditionUid;
                                            itemRow["Uid"] = dbItem.Uid;
                                            itemRow["ConditionType"] = (int)dbItem.ConditionType;
                                            if (dbItem.ConditionFieldTypeID.HasValue)
                                            {
                                                itemRow["ConditionFieldTypeID"] = dbItem.ConditionFieldTypeID;
                                            }
                                            if (dbItem.ConditionIntersectTypeID.HasValue)
                                            {
                                                itemRow["ConditionIntersectTypeID"] = dbItem.ConditionIntersectTypeID;
                                            }
                                            itemRow["Operator"] = (int)dbItem.Operator;
                                            items.Rows.Add(itemRow);

                                            okToSendGroup = true;
                                        }

                                    });

                                    if (okToSendGroup)
                                    {
                                        var groupRow = groups.NewRow();
                                        groupRow["AssetVersionUid"] = dbGroup.AssetVersionUid;
                                        groupRow["Uid"] = dbGroup.Uid;
                                        groupRow["MatchType"] = (int)dbGroup.MatchType;
                                        groupRow["Position"] = dbGroup.Position;
                                        if (dbGroup.Threshold.HasValue)
                                        {
                                            groupRow["Threshold"] = dbGroup.Threshold.Value;
                                        }
                                        if (dbGroup.Weight.HasValue)
                                        {
                                            groupRow["Weight"] = dbGroup.Weight.Value;
                                        }
                                        groups.Rows.Add(groupRow);
                                    }
                                }
                            });

                            if (groups.Rows.Count > 0)
                            {
                                Company.Connection.Execute(@"
create table #Groups (AssetVersionUid uniqueidentifier not null, Uid uniqueidentifier not null, MatchType int not null, [Position] int not null, Threshold float null, Weight decimal(5,3) null);
create table #Items (AssetVersionConditionUid uniqueidentifier not null, Uid uniqueidentifier not null, ConditionType int not null, ConditionFieldTypeID int not null, ConditionIntersectTypeID int null, Operator varchar(10) null );
create table #Values ( Uid uniqueidentifier not null, Value nvarchar(250) not null );", transaction: trans);

                                using (var bulkCopy = Company.Connection.CreateBulkCopy("#Groups", trans: trans))
                                {
                                    bulkCopy.ColumnMappings.Add("AssetVersionUid", "AssetVersionUid");
                                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                    bulkCopy.ColumnMappings.Add("MatchType", "MatchType");
                                    bulkCopy.ColumnMappings.Add("Position", "Position");
                                    bulkCopy.ColumnMappings.Add("Threshold", "Threshold");
                                    bulkCopy.ColumnMappings.Add("Weight", "Weight");
                                    bulkCopy.WriteToServer(groups);
                                }
                                using (var bulkCopy = Company.Connection.CreateBulkCopy("#Items", trans: trans))
                                {
                                    bulkCopy.ColumnMappings.Add("AssetVersionConditionUid", "AssetVersionConditionUid");
                                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                    bulkCopy.ColumnMappings.Add("ConditionType", "ConditionType");
                                    bulkCopy.ColumnMappings.Add("ConditionFieldTypeID", "ConditionFieldTypeID");
                                    bulkCopy.ColumnMappings.Add("ConditionIntersectTypeID", "ConditionIntersectTypeID");
                                    bulkCopy.ColumnMappings.Add("Operator", "Operator");
                                    bulkCopy.WriteToServer(items);
                                }
                                using (var bulkCopy = Company.Connection.CreateBulkCopy("#Values", trans: trans))
                                {
                                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                    bulkCopy.ColumnMappings.Add("Value", "Value");
                                    bulkCopy.WriteToServer(values);
                                }

                                Company.Connection.Execute(@"
delete  T
from    metrics.AssetVersionCondition T
    left join #Groups S on S.AssetVersionUid = T.AssetVersionUid and S.Uid = T.Uid
where   T.AssetVersionUid = @V
    and S.Uid is null;

merge   metrics.AssetVersionCondition as T
using   #Groups as S
on      (T.AssetVersionUid = S.AssetVersionUid and T.Uid = S.Uid)
when    matched then
update  set
    T.MatchType = S.MatchType,
    T.Position = S.Position,
    T.Threshold = S.Threshold,
    T.Weight = S.Weight
when    not matched by target then
insert  (AssetVersionUid, Uid, MatchType, [Position], Threshold, Weight)
values  (S.AssetVersionUid, S.Uid, S.MatchType, S.[Position], S.Threshold, S.Weight);

delete  T
from    metrics.AssetVersionConditionItem T
    inner join #Groups G on G.Uid = T.AssetVersionConditionUid
    left join #Items S on S.AssetVersionConditionUid = G.Uid and S.Uid = T.Uid
where   S.Uid is null;

merge   metrics.AssetVersionConditionItem as T
using   #Items as S
on      (T.AssetVersionConditionUid = S.AssetVersionConditionUid and T.Uid = S.Uid)
when    matched then
update  set
    T.ConditionType = S.ConditionType,
    T.ConditionFieldTypeID = S.ConditionFieldTypeID,
    T.ConditionIntersectTypeID = S.ConditionIntersectTypeID,
    T.Operator = S.Operator
when    not matched by target then
insert  (AssetVersionConditionUid, Uid, ConditionType, [ConditionFieldTypeID], ConditionIntersectTypeID, Operator)
values  (S.AssetVersionConditionUid, S.Uid, S.ConditionType, S.[ConditionFieldTypeID], S.ConditionIntersectTypeID, S.Operator);

delete  T
from    metrics.AssetVersionConditionItemValue T
    inner join #Items I on I.Uid = T.Uid
    left join #Values S on S.Uid = I.Uid and S.Value = T.Value
where   S.Uid is null;

merge   metrics.AssetVersionConditionItemValue as T
using   #Values as S
on      (T.Uid = S.Uid and T.Value = S.Value)
when    not matched by target then
insert  (Uid, Value)
values  (S.Uid, S.Value);", new { V = metricAssetVersion.Uid }, transaction: trans);
                            }
                        }
                        else
                        {
                            if (!isNew)
                            {
                                Company.Connection.Execute(@"
update  T
set     T.ConditionUid = null
from    metrics.ScoreItem T
    inner join metrics.AssetVersionCondition G on G.Uid = T.ConditionUid and G.AssetVersionUid = @Uid
    left join metrics.AssetVersionConditionItem I on I.AssetVersionConditionUid = G.Uid
where   I.Uid is null;

delete metrics.AssetVersionCondition where AssetVersionUid = @Uid", new { metricAssetVersion.Uid }, transaction: trans);
                            }
                        }
                    }

                    #endregion

                    trans.Commit();
                }
                catch (WorkStatusException ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }
                    return new WorkHttpStatus(ex.Status, errorTitle, ex.Message);
                }
                catch
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }
                    return new WorkHttpStatus(HttpStatusCode.InternalServerError, errorTitle, $"An unhandled error occured. Please try your request again.");
                }
            }

            if (metricAsset != null && metricAssetVersion != null && changeWillEffectScore)
            {
                Company.CreateMeasureChangedNotificationExecution(metricAssetVersion, model.EffectiveDate);
            }

            return new WorkHttpStatus(isNew ? HttpStatusCode.Created : HttpStatusCode.OK, "", "");
        }

        [Obsolete]
        public MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;
            if (!effectiveDate.HasValue)
            {
                effectiveDate = DateTime.UtcNow.Date;
            }

            var sql = @"
                    drop table if exists #tbl
                    create table #tbl ([Uid] uniqueidentifier, VersionUid uniqueidentifier, Name nvarchar(250), ParentUid uniqueidentifier, IsGroup bit, Weight decimal(5,3), EffectiveDate date, Description nvarchar(4000))
                    
                    insert into #tbl 
                    	select	A.[Uid],
                    			V.Uid,
                                V.Name,
                    			A.ParentUid,
                    			A.IsGroup,
                    			V.Weight,
                    			V.EffectiveDate,
                    			V.Description

                    	from	metrics.AssetVersion V
                    			inner join (
                    					select		IA.[Uid],
                    								max(IV.EffectiveDate) as EffectiveDate
                    					from		metrics.AssetVersion IV
                    								inner join metrics.Asset IA on IA.[Uid] = IV.AssetUid 
                    															and IV.EffectiveDate <= @effectiveDate 
                    															and ((IA.State = 1 and EffectiveEndDate is null) or (IA.State = 3 and EffectiveEndDate >= @effectiveDate))
                                                    inner join metrics.Allocation Al on Al.Uid = IA.AllocationUid and Al.AssetTypeUid = @assetTypeUid 
                    					group by	IA.[Uid]
                    			) MV on MV.[Uid] = V.AssetUid AND MV.EffectiveDate = V.EffectiveDate
                    			inner join metrics.Asset A on A.[Uid] = V.AssetUid;
                    
                    with h as (
                    	select	*,
                    			1 as [Level]
                    	from	#tbl
                    	where	ParentUid is null
                    	union all
                    	select	A.*,
                    			h.[Level]+1 as [Level]
                    	from	#tbl A
                    			inner join h on h.[Uid] = A.ParentUid
                    )
                    
                    select	[Uid],
                    		ParentUid,
                    		[Level],
                    		Name,
                    		IsGroup,
                    		Weight,
                            EffectiveDate,
                            Description,
                    		'[]' as ConditionsJson
                    from	h
                    order by [Level] asc";

            if (cnn.State != ConnectionState.Open)
                cnn.Open();

            var results = cnn.Query<MetricAssetTypeHierarchyModel>(sql, new { assetTypeUid, effectiveDate = effectiveDate.Value }, commandTimeout: ApiTimeout).ToList();
            var model = new MetricAssetTypeHierarchyModels();
            var builder = new MetricHierarchyBuilder();

            foreach (var i in results.Where(i => !i.ParentUid.HasValue))
            {
                builder.BuildMetricHierarchy(results, model, null, i);
            }

            return model;
        }

        public List<RootMetricAssetHierarchyModel> GetMetricHierarchyByAsset(Guid allocationUid, Guid assetUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;

            if (!effectiveDate.HasValue)
            {
                effectiveDate = DateTime.UtcNow.Date;
            }
            else
            {
                effectiveDate = effectiveDate.Value.ToUniversalTime().Date;
            }

            string sql = $@"
drop table if exists #results;
select	*
into    #results
from	(
		select  Ma.[Uid], 
				Ma.ParentUid,
				V.Uid as VersionUid,
				V.Name,
				V.Description,
				Ma.IsGroup,
				SI.Uid as ScoreItemUid,
                SI.RunDate,
                V.EffectiveDate, 
				ROW_NUMBER() OVER(PARTITION BY Ma.Uid ORDER BY S.EffectiveDate DESC, SI.UpdatedOn desc) as RowNum,
				V.EffectiveEndDate as EndDate,
				V.[Weight],
				iif(SI.AdjustedWeight > 1, 1, SI.AdjustedWeight) as AdjustedWeight,
				iif(SI.AdjustedMaxWeight > 1, 1, SI.AdjustedMaxWeight) as AdjustedMaxWeight,
				coalesce(SI.DisplayWeight, SI.AdjustedWeight) as DisplayWeight,
                coalesce(SI.DisplayMaxWeight, SI.AdjustedMaxWeight) as DisplayMaxWeight,
                iif(Ma.IsGroup = 1, null, SI.Value) as Value,
                iif(SI.DecimalValue > 1, 1, SI.DecimalValue) as DecimalValue,
				cast(iif(SI.Evidence is not null and SI.Evidence <> '', 1, 0) as bit) as HasEvidence,
				A.ScoreType,
				V.MatchConditionsOnly,
                SI.ConditionUid,
                SI.OtherConditions as 'OtherConditionsJSON'
		from    metrics.Score S 
				inner join metrics.Allocation A on A.Uid = S.AllocationUid
                inner join metrics.ScoreItemLink SIL on SIL.ScoreUid = S.Uid 
				inner join metrics.ScoreItem SI on SI.Uid = SIL.ScoreItemUid
				inner join metrics.AssetVersion V on V.Uid = SI.AssetVersionUid
				inner join metrics.Asset Ma on Ma.Uid = V.AssetUid
        where   S.AllocationUid = @allocationUid 
                and S.AssetUid = @assetUid 
				and S.EffectiveDate <= @effectiveDate and (S.EndDate >= @effectiveDate or S.EndDate is null)
		) O 
where	O.RowNum = 1
order by ParentUid, Name

select		R.*,
			(
			select		M.*,
						(
                    	SELECT 
                            C.Uid,
							MatchType,
							Position,
							Threshold,
							Weight,
							(
                            select	F.FriendlyName as FieldName,
                    					CI.Operator,
                    					(
											case when F.Type = 'Lookup' then 
												(
													select	top 1
															[Text]
													from	FieldLookupValue
													where	FieldTypeID = F.ID and LookupObjectType = F.LookupObjectType and LookupObjectID = F.LookupObjectID and AssetUid = CIV.Value
												)
											ELSE CIV.Value
										end
										) as [Value]
                    			from	[metrics].[AssetVersionCondition] C1
										inner join metrics.AssetVersionConditionItem CI on CI.AssetVersionConditionUid = C.Uid
										left join metrics.AssetVersionConditionItemValue CIV on CIV.Uid = CI.Uid 
                    					inner join FieldType F on F.ID = CI.ConditionFieldTypeID
                    			where	C.AssetVersionUid = M.VersionUid and CI.[AssetVersionConditionUid] = C1.[Uid]
                    			for json path
                                ) as ConditionItems
                    	from	[metrics].[AssetVersionCondition] C
                                inner join metrics.AssetVersionConditionItem CI on CI.AssetVersionConditionUid = C.Uid
                                left join metrics.AssetVersionConditionItemValue CIV on CIV.Uid = CI.Uid 
                    			inner join FieldType F on F.ID = CI.ConditionFieldTypeID
                    	where	C.AssetVersionUid = M.VersionUid
                    	for json path							
						) as Conditions
			from		#results M
			where		M.ParentUid = R.Uid
			order by	M.[Name]
			for json path
			) as MeasuresJson,
		    (
             SELECT 
                C.Uid,
		    	MatchType,
		    	Position,
		    	threshold,
		    	Weight,
		    	(
                      select	F.FriendlyName as FieldName,
                  				CI.Operator,
                  				(
		    					case when F.Type = 'Lookup' then 
		    						(
		    							select	top 1
		    									[Text]
		    							from	FieldLookupValue
		    							where	FieldTypeID = F.ID and LookupObjectType = F.LookupObjectType and LookupObjectID = F.LookupObjectID and AssetUid = CIV.Value
		    						)
		    					ELSE CIV.Value
		    				end
		    				) as [Value]
                  		from	[metrics].[AssetVersionCondition] C1
		    				inner join metrics.AssetVersionConditionItem CI on CI.AssetVersionConditionUid = C.Uid
		    				left join metrics.AssetVersionConditionItemValue CIV on CIV.Uid = CI.Uid 
                  			inner join FieldType F on F.ID = CI.ConditionFieldTypeID
                  		where	C.AssetVersionUid = R.VersionUid and CI.[AssetVersionConditionUid] = C1.[Uid]
                  		for json path
                        ) as ConditionItems
		    	from metrics.AssetVersionCondition C
		    	where	C.AssetVersionUid = R.VersionUid
		    	for json path
		    ) as ConditionsJson
from		#results R
where		R.ParentUid is null
order by	R.[Name]";

            if (cnn.State != ConnectionState.Open)
            {
                cnn.Open();
            }

            return cnn.Query<RootMetricAssetHierarchyModel>(sql, new { allocationUid, assetUid, effectiveDate = effectiveDate.Value }, commandTimeout: ApiTimeout).ToList();
        }

        public List<MetricAssetViewModel> GetMetricStructureByAllocation(Guid allocationUid, List<State> states = null)
        {
            if (states == null || states.Count == 0)
            {
                states.Add(State.Active);
            }
            var endDateString = states.Contains(State.Deleted) ? ",V.EffectiveEndDate" : "";
            var fragments = Company.Query<string>($@"
                    select	A.Uid,
                    		A.ParentUid,
                            A.AllocationUid,
                    		Al.AssetTypeUid,
                    		A.IsGroup,
                    		V.Name,
                    		V.Description,
                    		V.EffectiveDate,
							V.Weight,
                    		V.Threshold,
							V.UpdateFrequency,
							V.MatchConditionsOnly,
                            {hasResultsSql("V.Uid")},
							{conditionGroupsJsonSql("V.Uid")},
                            {dataQualityDefinitionSql("V.Definition", "V.Uid")},
                            VC.Count as [VersionCount],
                            A.[State],
                            V.Definition as [DefinitionJson]
                            {endDateString}
                    from	metrics.Asset A
                    		inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.Uid = @allocationUid
                            cross apply (
                    			select	max(EffectiveDate) as EffectiveDate
                    			from	metrics.AssetVersion
                    			where	AssetUid = A.Uid
                    		) MV
                    		inner join metrics.AssetVersion V on V.AssetUid = A.Uid and V.EffectiveDate = MV.EffectiveDate and A.[State] IN @states
                            cross apply (select count(1) as [Count] from metrics.AssetVersion where AssetUid = A.Uid) VC
                    order by A.ParentUid, V.Name
                    for		json path", new { allocationUid, states }, ApiTimeout).ToList();

            var jsonString = string.Join("", fragments);
            JArray items = JArray.Parse(string.IsNullOrEmpty(jsonString) ? "[]" : jsonString);
            var models = items.ToObject<List<MetricAssetViewModel>>();

            if (models == null)
            {
                models = new List<MetricAssetViewModel>();
            }

            models.ForEach(m =>
            {
                processConditionGroup(m);
                processDefinition(m);
            });

            return models;
        }

        public List<MetricFieldTypeViewModel> GetMetricConditionsFields(Guid assetTypeUid)
        {
            return Company.Query<MetricFieldTypeViewModel>($@"
                            select	A.Uid as AssetTypeUid,
			                        A.Name as AssetTypeName,
                                    F.ID,
                            		F.FriendlyName as Name,
                                    F.Name as ApiName,
                            		F.Type,
                            		(
                            			select	AssetUid as Value,
                            					Text
                            			from	FieldLookupValue
                            			where	FieldTypeID = F.ID
                            			for		json path
                            
                            		) as ValuesJson
                            from	AssetType A
                            		inner join FieldType F on F.AssetTypeID = A.ID and A.[uid] = @assetTypeUid and F.Type in ('Boolean', 'Decimal', 'Date', 'DateTime', 'Html', 'Lookup', 'Number', 'Text')",
                                    new { assetTypeUid }, ApiTimeout).ToList();
        }

        public async Task<List<MetricFieldTypeViewModel>> GetFieldsByRuleResultPath(Guid ruleResultPathUid)
        {
            var sql = @"
select      A.Uid as AssetTypeUid,
			A.Name as AssetTypeName,
            F.ID,
            F.Name as ApiName,
            F.FriendlyName as Name,
            F.[Type],
		    (
                select      AssetUid as Value,
					        [Text]
                from        FieldLookupValue
                where       FieldTypeID = F.ID
                order by    [Text]
                for json path
            ) as ValuesJson
from        [metrics].[RollupPath] P
            inner join [metrics].[RollupPathSegment] SE on SE.RollupPathUid = P.Uid
            inner join AssetType A on A.ID = SE.AssetTypeID
            inner join FieldType F on F.AssetTypeID = A.ID
where       P.Uid = @ruleResultPathUid 
            and SE.[Position] > 1
            and F.[Type] in ('Boolean', 'Date', 'DateTime', 'Decimal', 'Html', 'Lookup', 'Number', 'Text')
order by    SE.[Position], F.FriendlyName";
            var results = await Company.QueryAsync<MetricFieldTypeViewModel>(sql, new { ruleResultPathUid }, ApiTimeout);
            return results.ToList();
        }

        public async Task<IEnumerable<MetricPathOptionViewModel>> GetMetricPathOptionsBy(int assetTypeId, ScoreType scoreType)
        {
            var sql = @"
select  *
from    (
        select	P.Uid,
		        P.State,
		        metrics.CalculateRollupPath(P.Uid) as [Path],
		        (
                    select      A.Uid as AssetTypeUid,
					            A.Name,
                                AP.[Path]
                    from        [metrics].[RollupPathSegment] SE
                                inner join AssetType A on A.ID = SE.AssetTypeID
                                cross apply dbo.GetAssetTypeTextPathById(A.ID, '->') AP
                    where       RollupPathUid = P.Uid
                    order by    [Position]
                    for json path
                ) as SegmentsJson
        from    [metrics].[RollupPath] P
        where   P.ScoreType = @scoreType 
                and P.AssetTypeid = @assetTypeId
                and P.[State] = 1
        ) P
order by P.[Path]";
            return await Company.QueryAsync<MetricPathOptionViewModel>(sql, new { assetTypeId, scoreType = (int)scoreType }, ApiTimeout);
        }

        public (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var result = new MetricScoreApiModel();
            var parameters = new DynamicParameters();

            List<string> outerFilters = new List<string>();
            List<string> innerFilters = new List<string>();
            List<string> fieldJoins = new List<string>();

            var dateStart = DateTime.MinValue;
            var dateEnd = DateTime.MinValue;
            Guid allocationUid = Guid.Empty;
            MetricAllocation allocation = null;

            if (!queryParams.Any(x => x.Key.ToLower() == "_scoretype") && !queryParams.Any(x => x.Key.ToLower() == "_allocationuid"))
            {
                // Look up default Governance score allocation.
                allocation = Company.Filter<MetricAllocation>(a => a.AssetTypeUid == at.uid && a.ScoreType == ScoreType.Governance && string.IsNullOrEmpty(a.OverrideName)).FirstOrDefault();
                if (allocation == null)
                {
                    return (null, $"Allocation for {ScoreType.Governance} score type and asset type does not exist");
                }

                parameters.Add("@allocationUid", allocation.Uid);
                allocation = null;
            }

            int customFieldsCounter = 0;
            foreach (var param in queryParams)
            {
                switch (param.Key.ToLowerInvariant())
                {
                    case "_pagesize":
                        int pageSize = 0;

                        if (!int.TryParse(param.Value, out pageSize) || pageSize <= 0)
                        {
                            return (null, "Invalid '_pagesize' parameter value");
                        }

                        result.pageSize = pageSize;
                        break;
                    case "_pagenum":
                        int pageNum = 0;

                        if (!int.TryParse(param.Value, out pageNum) || pageNum <= 0)
                        {
                            return (null, "Invalid 'pageNum' parameter value");
                        }

                        result.pageNum = pageNum;
                        break;
                    case "_effectivedatestart":
                        DateTime.TryParse(param.Value, out dateStart);

                        if (dateStart == DateTime.MinValue)
                        {
                            return (null, "Invalid '_effectiveDateStart' parameter value");
                        }

                        parameters.Add("@dateStart", dateStart);
                        innerFilters.Add("IMS.EffectiveDate >= @dateStart");
                        outerFilters.Add("MS.EffectiveDate >= @dateStart");
                        break;
                    case "_effectivedateend":
                        DateTime.TryParse(param.Value, out dateEnd);

                        if (dateEnd == DateTime.MinValue)
                        {
                            return (null, "Invalid '_effectiveDateEnd' parameter value");
                        }

                        parameters.Add("@dateEnd", dateEnd);
                        innerFilters.Add("IMS.EffectiveDate <= @dateEnd");
                        outerFilters.Add("MS.EffectiveDate <= @dateEnd");
                        break;
                    case "_assetuid":
                        Guid assetUid = Guid.Empty;
                        if (!Guid.TryParse(param.Value, out assetUid))
                        {
                            return (null, "Invalid '_assetUid' parameter value");
                        }

                        var assetTypeId = Company.Assets.Where(x => x.uid == assetUid).FirstOrDefault()?.AssetTypeID;

                        if (assetTypeId != at.ID)
                        {
                            return (null, "Asset of given asset type Uid does not exists");
                        }

                        if (queryParams.Any(x => x.Key.ToLower() == "customfield"))
                        {
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");
                        }

                        parameters.Add("@assetUid", assetUid);
                        outerFilters.Add("MS.AssetUid = @assetUid");
                        break;
                    case "_scoretype":
                        ScoreType scoretype;
                        if (!Enum.TryParse(param.Value, out scoretype))
                        {
                            return (null, "Invalid '_scoreType' parameter value");
                        }

                        if (queryParams.Any(x => x.Key.ToLower() == "_allocationuid"))
                        {
                            return (null, "'_allocationUid' AND '_scoreType' are exclusive filters and may not be combined.");
                        }

                        allocation = Company.Filter<MetricAllocation>(a => a.AssetTypeUid == at.uid && a.ScoreType == scoretype && string.IsNullOrEmpty(a.OverrideName)).FirstOrDefault();

                        if (allocation == null)
                        {
                            return (null, "Allocation for specified score type and asset type does not exist");
                        }

                        parameters.Add("@allocationUid", allocation.Uid);
                        allocation = null;
                        break;
                    case "_allocationuid":
                        if (!Guid.TryParse(param.Value, out allocationUid))
                        {
                            return (null, "Invalid '_allocationUid' parameter value");
                        }

                        allocation = Company.GetByUid<MetricAllocation>(allocationUid);

                        if (allocation == null)
                        {
                            return (null, "Allocation does not exist");
                        }

                        if (allocation.AssetTypeUid != at.uid)
                        {
                            return (null, "Allocation is not associated with the given asset type");
                        }

                        if (queryParams.Any(x => x.Key.ToLower() == "_scoretype"))
                        {
                            return (null, "'_allocationUid' AND '_scoreType' are exclusive filters and may not be combined.");
                        }

                        parameters.Add("@allocationUid", allocationUid);
                        allocation = null;
                        break;

                    default:
                        customFieldsCounter++;

                        int? filterFieldTypeId = null;
                        filterFieldTypeId = Company.FieldTypes.Where(x => x.AssetTypeID == at.ID && x.Name.ToLower() == param.Key.ToLower()).FirstOrDefault()?.ID;
                        if (filterFieldTypeId == null)
                        {
                            return (null, $"Invalid custom field parameter. Field type with name '{param.Key}' does not exists");
                        }

                        if (parameters.ParameterNames.Any(x => x.ToLower() == "_assetuid"))
                        {
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");
                        }

                        fieldJoins.Add($"inner join Field F{customFieldsCounter} on F{customFieldsCounter}.FieldTypeID = @ftId{customFieldsCounter} and F{customFieldsCounter}.AssetID = A.ID and F{customFieldsCounter}.FormattedValue = @ftValue{customFieldsCounter}");
                        parameters.Add("@ftId" + customFieldsCounter, filterFieldTypeId);
                        parameters.Add("@ftValue" + customFieldsCounter, param.Value);

                        break;
                }
            }

            bool takeOnlyLastScore = false;

            if (dateEnd < dateStart && (dateStart != DateTime.MinValue && dateEnd != DateTime.MinValue))
            {
                return (null, "Effective start date should be before effective end date parameter");
            }
            if (dateStart == DateTime.MinValue && dateEnd == DateTime.MinValue)
            {
                takeOnlyLastScore = true;
            }

            parameters.Add("@pageSize", result.pageSize);
            parameters.Add("@pageNum", result.pageNum);


            if (!Company.CurrentResourceIsAdmin)
            {
                outerFilters.Add($"A.ID not in ({Company.GetNoReadSqlStatement()})");
            }

            outerFilters.Add("MS.AllocationUid = @allocationUid");

            string outerWhere = string.Join(" and ", outerFilters);
            string innerWhere = innerFilters.Count == 0 ? "" : " and " + string.Join(" and ", innerFilters);
            string fieldJoinStatement = string.Join(" ", fieldJoins) + "";

            if (!string.IsNullOrEmpty(outerWhere))
            {
                outerWhere = "where " + outerWhere;
            }

            var countSql = $@"
select  count(distinct MS.AssetUid) 
from    metrics.Score MS 
        inner join Asset A on A.Uid = MS.AssetUid 
        {fieldJoinStatement} 
        {outerWhere}";

            result.total = Company.Query<int>(countSql, parameters, ApiTimeout).FirstOrDefault();

            var sql = $@"
select      MS.AssetUid,
            (
            select      {(takeOnlyLastScore ? "top 1" : "")} 
                        IMS.EffectiveDate, 
                        IMS.Value as Score, 
                        Al.ScoreType 
            from        metrics.Score IMS
                        inner join metrics.Allocation Al on Al.Uid = IMS.AllocationUid 
		    where       IMS.AllocationUid = @allocationUid and IMS.AssetUid = MS.AssetUid {innerWhere}
		    order by    IMS.EffectiveDate desc
		    for json path
            ) as Scores 
from        metrics.Score MS
            inner join Asset A on A.Uid = MS.AssetUid 
            {fieldJoinStatement} {outerWhere}
group by    MS.AssetUid
order by    MS.AssetUid
offset ((@pageNum-1)*@pageSize) rows fetch next @pageSize rows only
for json path";

            var itemsJson = string.Join("", Company.Query<string>(sql, parameters, ApiTimeout).ToList());

            result.items = JsonConvert.DeserializeObject<List<MetricAssetScoreModel>>(itemsJson);
            if (result.items == null)
            {
                result.items = new List<MetricAssetScoreModel>();
            }

            return (result, "");
        }

        public List<DataQualityResponseModel> InsertDataQualityResult(List<DataQualityInsertModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityResponseModel> results = null;
            try
            {
                List<IDataQualityUpsert> upsert = new List<IDataQualityUpsert>();
                upsert.AddRange(request);
                results = Company.UpsertAssetResults(upsert, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public DataQualityGetResultModel GetDataQualityResults(Guid owningAssetUid, Guid? evaluatedAssetUid = null, int pageSize = 250, int pageNum = 1, string sort = null, string direction = "asc", DateTime? effectiveDateStart = null, DateTime? effectiveDateEnd = null, bool includeDuplicateFlag = false, string _filter = "", string _simpleFilter = "")
        {
            var result = new DataQualityGetResultModel();
            var parameters = new DynamicParameters();

            List<string> simpleFilterWhereConditions = new List<string>();
            List<string> whereConditions = new List<string>();

            parameters.Add("@evaluatedAssetUid", evaluatedAssetUid, DbType.Guid, ParameterDirection.Input);
            parameters.Add("@owningAssetUid", owningAssetUid, DbType.Guid, ParameterDirection.Input);

            if (effectiveDateStart.HasValue)
            {
                whereConditions.Add("R.EffectiveDate >= @effectiveStartDate");
                parameters.Add("@effectiveStartDate", effectiveDateStart.Value, DbType.DateTime2, ParameterDirection.Input);
            }
            
            if (effectiveDateEnd.HasValue)
            {
                whereConditions.Add("R.EffectiveDate <= @effectiveEndDate");
                parameters.Add("@effectiveEndDate", effectiveDateEnd.Value, DbType.DateTime2, ParameterDirection.Input);
            }

            if (!string.IsNullOrEmpty(_filter))
            {
                var filterExpressionParser = new FilterExpressionParser(Company, FilterExpressionParseType.RuleResults);
                Dictionary<string, object> sqlParams;
                var query = "(" + filterExpressionParser.Parse(_filter, out sqlParams, out _) + ")";

                foreach (var item in sqlParams)
                {
                    parameters.Add(item.Key, item.Value);
                }
                whereConditions.Add(query);
            }

            if (!string.IsNullOrEmpty(_simpleFilter))
            {
                parameters.Add("@simpleFilterLike", Company.GetEscapedFilterString(_simpleFilter, true), DbType.String, ParameterDirection.Input);
                parameters.Add("@simpleFilter",_simpleFilter.ToLower(), DbType.String, ParameterDirection.Input);
                 
                simpleFilterWhereConditions.Add("R.EffectiveDate like @simpleFilterLike");
                simpleFilterWhereConditions.Add("R.FailCount like @simpleFilterLike");
                simpleFilterWhereConditions.Add("R.PassCount like @simpleFilterLike");
                simpleFilterWhereConditions.Add("R.PassFraction like @simpleFilterLike");
                simpleFilterWhereConditions.Add("R.TotalCount like @simpleFilterLike");
                simpleFilterWhereConditions.Add("R.Segments.exist('/path/segment[contains(lower-case(.),sql:variable(\"@simpleFilter\"))]') = 1");

                var classes = AssetTypeClass.BusinessAsset.GetAsList();
                var match = classes.Where(x => x.Name.ToLower(CultureInfo.InvariantCulture).Contains(_simpleFilter.ToLower(CultureInfo.InvariantCulture).Trim('\''))
                || x.Value.ToLower(CultureInfo.InvariantCulture).Contains(_simpleFilter.ToLower(CultureInfo.InvariantCulture).Trim('\''))).ToList();

                if (match.Count > 0)
                {
                    simpleFilterWhereConditions.Add($"(R.Class in ({string.Join(", ", match.Select(x => (int)x.ID))}))");
                }
            }

            string whereStatement = "";
            if (whereConditions.Count > 0 || simpleFilterWhereConditions.Count > 0)
            {
                whereStatement = "where ";
            }

            if (whereConditions.Count > 0)
            {
                whereStatement += string.Join(" and ", whereConditions);
            }

            if (simpleFilterWhereConditions.Count > 0)
            {
                whereStatement += (whereConditions.Count > 0) ? " and " : "";
                whereStatement += "(" + string.Join(" or ", simpleFilterWhereConditions) + ")";
            }

            #region ordering selection logic

            string orderSql = "EffectiveDate";
            if (!string.IsNullOrWhiteSpace(sort))
            {
                switch (sort.Trim())
                {
                    case "EffectiveDate":
                    case "EvaluatedAssetUid":
                    case "FailCount":
                    case "OwningAssetUid":
                    case "PassCount":
                    case "Passed":
                    case "PassFraction":
                    case "ResultUid":
                    case "RunDate":
                    case "TotalCount":
                        orderSql = $"{sort}";
                        break;
                    case "EvaluatedAssetClass":
                        orderSql = "EvaluatedAssetTypeClass";
                        break;
                    case "EvaluatedAssetDisplayPath":
                        orderSql = "EvaluatedAssetDisplayPath";
                        break;
                    case "EvaluatedAssetPath":
                        orderSql = "EvaluatedAssetPath";
                        break;
                    case "EvaluatedAssetTypePath":
                        orderSql = "EvaluatedAssetTypePath";
                        break;
                    default:
                        orderSql = "EffectiveDate";
                        break;
                }
            }
            orderSql = $"order by {orderSql} {direction ?? ""}";

            #endregion

            if (pageNum <= 0)
            {
                pageNum = 1;
            }

            if (pageSize <= 0 || pageSize > 1000)
            {
                pageSize = 25;
            }

            result.pageNum = pageNum;
            result.pageSize = pageSize;

            parameters.Add("@pageNum", result.pageNum, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@pageSize", result.pageSize, DbType.Int32, ParameterDirection.Input);

            var cteQuery = @"with R as (
	select	R.Uid as ResultUid,
			O.Uid as OwningAssetUid,
			E.ID as EvaluatedAssetId,
			E.Uid as EvaluatedAssetUid,
			E.Segments,
			E.Class,
			P.[Path] as EvaluatedAssetTypePath, 
			R.EffectiveDate, 
			R.RunDate, 
			R.PassCount, 
			R.FailCount, 
			R.TotalCount, 
			R.PassFraction, 
			case 
				when R.PassCount = 0 and R.FailCount = 0 then null 
				when Ru.Threshold <= R.PassFraction then cast(1 as bit) 
				else cast(0 as bit)
			end as Passed, 
			case 
				when ROW_NUMBER() over (partition by O.Uid, coalesce(E.Uid, newid()), R.EffectiveDate order by R.RunDate desc) = 1 then cast(0 as bit) 
				else cast(1 as bit) 
			end as IsDuplicate 
	from	AssetResult R
			inner join AssetResultEdge Oe on Oe.$to_id = R.$node_id and Oe.Class = 1
			inner join graph.AssetNode O on O.$node_id = Oe.$from_id and O.Uid = @owningAssetUid
			inner join Asset Oa on Oa.ID = O.ID
			inner join [Rule] Ru on Ru.ID = Oa.ObjectID
			left join AssetResultEdge Ee on Ee.$to_id = R.$node_id and Ee.Class = 2
			left join graph.AssetNode E on E.$node_id = Ee.$from_id and (@evaluatedAssetUid is null or (@evaluatedAssetUid is not null and E.Uid = @evaluatedAssetUid))
			outer apply dbo.GetAssetTypeTextPathById(E.AssetTypeID, ' > ') P
)";

            var countQuery = $@"
{cteQuery}
select	count(1)
from	R
{whereStatement}";

            result.total = Company.Query<int>(countQuery, parameters).Single();

            var dupeColumnReference = "";
            if (includeDuplicateFlag)
            {
                dupeColumnReference = ", R.IsDuplicate";
            }

            var itemsQuery = $@"
{cteQuery}

select	R.ResultUid,
		R.OwningAssetUid,
		R.EvaluatedAssetUid,
		EKP.KeyPath as EvaluatedAssetPath,
		EDP.DisplayPath as EvaluatedAssetDisplayPath,
		R.Segments as EvaluatedAssetSegments,
		R.EvaluatedAssetTypePath,
		R.Class as EvaluatedAssetTypeClass,
		R.EffectiveDate,
		R.RunDate, 
		R.PassCount, 
		R.FailCount, 
		R.TotalCount, 
		R.PassFraction, 
		R.Passed{dupeColumnReference}
from	R 
        left join[graph].[AssetNodeDisplayPath] EDP on EDP.ID = R.EvaluatedAssetId 
        left join[graph].[AssetNodeKeyPath] EKP on EKP.ID = R.EvaluatedAssetId 
{whereStatement} 
{orderSql} 
offset((@pageNum - 1) * @pageSize) rows fetch next @pageSize rows only";

            result.items = Company.Query<DataQualityGetResultItem>(itemsQuery, parameters, ApiTimeout).ToList();
            
            if (result.items == null)
            {
                result.items = new List<DataQualityGetResultItem>();
            }

            return result;
        }

        public List<DataQualityAssetResultModel> GetAssetResultDetailsByUid(Guid value)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Uid", value);

            string assetResultSQL = $@"select 
	                                    AR.Uid as ResultUid, ARE.[Class] as Class, AN.UID as AssetUid, AR.EffectiveDate as EffectiveDate, AR.RunDate as RunDate
                                    from 
	                                    AssetResult AR, assetResultedge ARE, graph.AssetNode AN					
                                    where 
	                                    Match (AN -(ARE)-> AR)
	                                    and 
	                                    AR.Uid = @Uid";

            return Company.Query<DataQualityAssetResultModel>(assetResultSQL, parameters, ApiTimeout).ToList();
        }

        public List<DataQualityDeleteResponseModel> DeleteDataQualityResult(List<DataQualityDeleteModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityDeleteResponseModel> results = null;
            try
            {
                results = Company.DeleteAssetResults(request, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public List<DataQualityResponseModel> UpdateDataQualityResult(List<DataQualityUpdateModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityResponseModel> results = null;
            try
            {
                List<IDataQualityUpsert> upsert = new List<IDataQualityUpsert>();
                upsert.AddRange(request);
                results = Company.UpsertAssetResults(upsert, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public async Task<ApiExecutionInfo> PostBulkDataQualityResults(List<DataQualityInsertModel> request, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = Company.CurrentCompanyID,
                CompanyDomainPrefix = Company.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.PostDataQualityResults,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            await StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(request));


            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;

            Company.Add(execution);

            // Save to queue.
            if (!await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
            {
                throw new ArgumentException(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
            }

            return executionInfo;
        }

        public List<MeasureVersionHistoryModel> GetMetricVersionHistory(Guid measureUid)
        {
            var fragments = Company.Query<string>($@"                    
                    select ROW_NUMBER() over (Order by V.EffectiveDate asc, ISNULL(V.EffectiveEndDate, GETDATE()) asc) as version, 
                            A.Uid as MeasureUid,
                    		V.Name,
                    		V.Description,
                    		V.EffectiveDate,
							V.EffectiveEndDate,
							V.Weight,
							V.Uid as versionuid,
                            {hasResultsSql("V.Uid")}, 
							{conditionGroupsJsonSql("V.Uid")}, 
                            {dataQualityDefinitionSql("V.Definition", "V.Uid")},
                            V.Definition as [DefinitionJson],
                            V.MatchConditionsOnly
                    from	metrics.Asset A                    		
                            cross apply (
                    			select	EffectiveDate as EffectiveDate
                    			from	metrics.AssetVersion
                    			where	AssetUid = A.Uid
                    		) MV
                    		inner join metrics.AssetVersion V on V.AssetUid = A.Uid and V.EffectiveDate = MV.EffectiveDate
					where   A.Uid = @measureUid
				    order by version
                    for		json path", new { measureUid }, ApiTimeout).ToList();

            var jsonString = string.Join("", fragments);
            JArray items = JArray.Parse(string.IsNullOrEmpty(jsonString) ? "[]" : jsonString);
            var models = items.ToObject<List<MeasureVersionHistoryModel>>();

            if (models == null)
                models = new List<MeasureVersionHistoryModel>();

            models.ForEach(m =>
            {
                processConditionGroup(m);
                processDefinition(m);
            });

            return models;
        }

        internal class ReclaulatMeasureExecutionFields
        {
            public Guid measureUid { get; set; }
            public string action { get; set; }
        }
        public Guid RecalculateMeasureScoreItems(Guid allocationUid, Guid measureUid)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new StatusCodeException(HttpStatusCode.Forbidden);
            }

            var measure = Company.GetByUid<MetricAsset>(measureUid, a => a.Versions, a => a.Allocation);

            if (measure == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, $"Measure with Uid of {measureUid} could not be found.");
            }

            if (measure.AllocationUid != allocationUid)
            {
                throw new GenericException(HttpStatusCode.NotFound, $"Measure does not belong to allocation with Uid of {allocationUid}.");
            }

            if (measure.Allocation == null)
            {
                throw new GenericException(HttpStatusCode.Conflict, $"Measure does not belong to a valid Allocation, indicating an invalid measure.");
            }

            if (measure.Versions == null)
            {
                throw new GenericException(HttpStatusCode.Conflict, $"Measure does not contain any versions, indicating an invalid measure.");
            }

            var latestVersion = measure.Versions.OrderByDescending(v => v.EffectiveDate).FirstOrDefault();

            if (latestVersion == null)
            {
                throw new GenericException(HttpStatusCode.Conflict, $"Measure does not contain any versions, indicating an invalid measure.");
            }

            var startedOnLimit = DateTime.UtcNow.AddHours(-2);
            var existingExecutions = Company.Any<ScoreExecution>(e => !e.CompletedOn.HasValue && e.TriggeredByMeasureUid == measureUid);

            if (existingExecutions)
            {
                throw new GenericException(HttpStatusCode.BadRequest, $"Measure is currently being recalculated. Please wait until we have completed this action then try again.");
            }

            return Company.CreateMeasureChangedNotificationExecution(latestVersion, latestVersion.EffectiveDate, measureUid);
        }
    }
}