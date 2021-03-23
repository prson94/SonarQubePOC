using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.model;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using igx.jobs.scoreprocessor.ChangeTypes;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class ExternalMeasureResultsCreatedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var models = await Storage.DeserializeJsonObjectFromBlobAsync<List<ExternalMeasureResultsCreatedModel>>(Info.StorageFolder, Info.StorageFile);

            if (model == null)
            {
                throw new Exception("Cannot load score file from storage");
            }

            var Db = GetCompanyContext();
            using (var company = GetEnvironmentConnection())
            {
                // Load assets to a temporary table to get the list of asset types with all associated measures for the specific effective date.
                var assets = new DataTable();
                assets.Columns.Add("AssetUid", typeof(Guid));
                assets.Columns.Add("MetricAssetUid", typeof(Guid));
                assets.Columns.Add("EffectiveDate", typeof(DateTime));
                assets.Columns.Add("Result", typeof(bool));

                foreach (var model in models)
                {
                    var assetRow = assets.NewRow();
                    assetRow["AssetUid"] = model.AssetUid;
                    assetRow["MetricAssetUid"] = model.MetricAssetUid;
                    assetRow["EffectiveDate"] = model.EffectiveDate.Date;
                    assetRow["Result"] = model.Result;
                    assets.Rows.Add(assetRow);
                }

                if (company.State != ConnectionState.Open)
                    company.Open();

                using (var trans = company.BeginTransaction())
                {
                    #region Populate models with relevant details

                    await company.ExecuteAsync(@"create table #AssetAllocations (
                            AssetUid uniqueidentifier not null,
                            EffectiveDate date not null,
                            MetricAssetUid uniqueidentifier not null,
                            Result bit not null,
                            MetricAssetVersionUid uniqueidentifier null,
                            AllocationUid uniqueidentifier null,
                            AssetTypeId int null
                        )", transaction: trans);

                    using (var bulkCopy = CreateBulkCopy(company, trans, "#AssetAllocations"))
                    {
                        bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                        bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                        bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                        bulkCopy.ColumnMappings.Add("Result", "Result");

                        await bulkCopy.WriteToServerAsync(assets);
                    }
                  
                    // Figure out which measure versions we are dealing with.
                    await company.ExecuteAsync(
                        "update T " +
                        "set T.MetricAssetVersionUid = S.Uid " +
                        "from #AssetAllocations T " +
                        "inner join metrics.AssetVersion S on S.AssetUid = T.MetricAssetUid " +
                        "and ( (T.EffectiveDate between S.EffectiveDate and S.EffectiveEndDate) or (T.EffectiveDate >= S.EffectiveDate and S.EffectiveEndDate is null) )",
                        transaction: trans
                        );

                    #endregion

                    var supportingDataRequest = await company.QueryAsync<ExternalMeasureResultsCreatedModel>(@"select * from #AssetAllocations;", transaction: trans);
                    models = supportingDataRequest.ToList();
                    
                    var assetMeasureModels = models
                        .GroupBy(m => new { m.AssetUid, m.EffectiveDate })
                        .Select(m => new AssetMeasureModel
                        {
                            AssetUid = m.Key.AssetUid,
                            EffectiveDate = m.Key.EffectiveDate,
                            Measures = m.Select(o => new AssetMeasureChildModel
                            {
                                MetricAssetUid = o.MetricAssetUid,
                                MetricAssetVersionUid = o.MetricAssetVersionUid,
                                Result = o.Result
                            }).ToList()
                        }).ToList();

                    await Db.SendContinuingScoreEventWithPayload(ScoreQueueChangeType.AssetMeasures, assetMeasureModels, Info.ExecutionUid, Info.StartedOn );
                }
            }
        }
    }
}
