using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    internal class WorkflowScoreGroup
    {
        public string Type { get; set; }
        public int TypeID { get; set; }
        public List<WorkflowScoredAsset> Assets { get; set; }
    }

    internal class RawWorkflowScoreAsset
    {
        public Guid AssetUid { get; set; }
        public string Type { get; set; }
        public int TypeID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }

    public class WorkflowCheckProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var scores = await Storage.DeserializeJsonObjectFromBlobAsync<List<ScoreCreatedModel>>(Info.StorageFolder, Info.StorageFile);

            if (scores == null)
            {
                throw new ArgumentNullException("scores","Cannot load score file from storage");
            }

            var Db = GetCompanyContext();

            var tbl = new DataTable();
            tbl.Columns.Add("AssetUid", typeof(Guid));

            foreach (var model in scores)
            {
                var row = tbl.NewRow();
                row["AssetUid"] = model.AssetUid;
                tbl.Rows.Add(row);
            }

            List<WorkflowScoreGroup> groups = null;

            if (Db.Connection.State != ConnectionState.Open)
                Db.Connection.Open();

            MetricAllocation allocation = null;
            if (scores.Count > 0)
            {
                var allocationUid = scores[0].AllocationUid;
                allocation = Db.MetricAllocations.SingleOrDefault(al => al.Uid == allocationUid);
            }

            using (var trans = Db.Connection.BeginTransaction())
            {
                await Db.Connection.ExecuteAsync(@"create table #Tbl (
                            AssetUid uniqueidentifier not null,
                            Object varchar(50), ObjectID int,
                            [Type] varchar(50), TypeID int,
                            HasScoreWorkflow bit)", transaction: trans);

                using (var bulkCopy = CreateBulkCopy(Db.Connection, trans, "#Tbl"))
                {
                    bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                    await bulkCopy.WriteToServerAsync(tbl);
                }

                await Db.Connection.ExecuteAsync(@"
update  T
set     T.Object = A.Object,
        T.ObjectID = A.ObjectID,
        T.[Type] = TA.Object,
        T.TypeID = TA.ObjectID,
        T.HasScoreWorkflow = cast(iif(W.ID is not null, 1, 0) as bit)
from    #Tbl T
        inner join Asset A on A.Uid = T.AssetUid
        inner join AssetType TA on TA.ID = A.AssetTypeID
        left join workflow.EventRegistration W on W.Object = TA.Object and W.ObjectID = TA.ObjectID and W.ChangeType = 5", transaction: trans);

                groups = Db.Connection
                    .Query<RawWorkflowScoreAsset>("select * from #Tbl where HasScoreWorkflow = 1", transaction: trans)
                    .GroupBy(g => new WorkflowScoreGroup { Type = g.Type, TypeID = g.TypeID } )
                    .Select(g => new WorkflowScoreGroup { 
                        Type = g.Key.Type, 
                        TypeID = g.Key.TypeID, 
                        Assets = g.Select(i => new WorkflowScoredAsset { Object = i.Object, ObjectID = i.ObjectID }).ToList() 
                    }).ToList();
            }

            if (groups != null)
            {
                groups.ForEach(g =>
                {
                    Db.SendWorkflowEvents(g.Type, g.TypeID, g.Assets, scoreType: (allocation != null) ? allocation.ScoreType : ScoreType.Governance);
                });
            }
        }
    }
}
