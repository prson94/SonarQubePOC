using d360.core.entities.Metric;
using d360.core.enums;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class WorkflowCheckProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var Db = GetCompanyContext();
            ExecutionRecord = getExecution(Db.Connection);

            var results = await Db.Connection.QueryMultipleAsync(@"
drop table if exists #Tbl;
create table #Tbl (
AllocationUid uniqueidentifier not null,
AssetUid uniqueidentifier not null,
Object varchar(50), ObjectID int,
[Type] varchar(50), TypeID int,
HasScoreWorkflow bit);

insert into #Tbl
select	P.AllocationUid,
        A.Uid,
		A.Object,
		A.ObjectID,
		TA.Object as [Type],
		TA.ObjectID  as TypeID,
		cast(iif(W.ID is not null, 1, 0) as bit) as HasWorkflow
from	metrics.ExecutionItem I
		cross apply openjson(I.Payload) with (AllocationUid uniqueidentifier '$.AllocationUid', AssetUid uniqueidentifier '$.AssetUid', EffectiveDate date '$.EffectiveDate') P
		inner join Asset A on A.Uid = P.AssetUid
		inner join AssetType TA on TA.ID = A.AssetTypeID
		left join workflow.EventRegistration W on W.Object = TA.Object and W.ObjectID = TA.ObjectID and W.ChangeType = 5
where	I.ExecutionID = @ID
		and I.[State] = 0 
		and P.AllocationUid is not null

select	[Type], TypeID,
	(
	select	Object, ObjectID from #Tbl where [Type] = T.[Type] and TypeID = T.TypeID for json path
	) as Assets
from	#Tbl T
group by [Type], TypeID
for json path;

select * from metrics.Allocation where Uid in (select top 1 AllocationUid from #Tbl)", new { ExecutionRecord.ID });

            var jsonStrings = results.Read<string>();
            var allocation = results.Read<MetricAllocation>().FirstOrDefault();
                
            var groups = JsonConvert.DeserializeObject<List<WorkflowScoreGroup>>(string.Join("", jsonStrings));
                
            if (groups != null)
            {
                groups.ForEach(g =>
                {
                    Db.SendWorkflowEvents(g.Type, g.TypeID, g.Assets, scoreType: (allocation != null) ? allocation.ScoreType : ScoreType.Governance);
                });
            }

            updateExecutionMarkingItemsAsComplete(Db.Connection, ExecutionRecord);
        }
    }
}
