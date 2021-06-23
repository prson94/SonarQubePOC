using d360.core.queue;
using Dapper;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class CheckTypeDependencyRemovedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var Db = GetCompanyContext();

            ExecutionRecord = getExecution(Db.Connection);
            var executionItems = getExecutionItems(Db.Connection, 0);

            string endScoreSql = @"create table #Scores (ScoreUid uniqueidentifier);

insert into #Scores
	select	distinct
			S.Uid
	from	metrics.AssetVersion V
			inner join metrics.ScoreItem I on  I.AssetVersionUid = V.Uid
			inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid 
			inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
			cross apply (
				select	count(1) as [Count]
				from	metrics.ScoreItemLink IL
				where	IL.ScoreUid = S.Uid and IL.ScoreItemUid <> L.ScoreItemUid
			) Cnt
    where   Cnt.[Count] = 0
            and V.Uid in @VersionUids;

declare @date date = getutcdate();

update	T
set		T.EndDate = @date,
        [Log] = [Log] + 'End-dated by Score Execution ' + cast(@ExecutionId as varchar) + '; '
from	metrics.Score T
		inner join #Scores S on S.ScoreUid = T.Uid;";

			foreach (var executionItem in executionItems)
            {
                var model = executionItem.GetPayload<CheckTypeDependencyRemovedModel>();
				if (Db.Database.Connection.State != System.Data.ConnectionState.Open)
				{ 
					Db.Database.Connection.Open();
				}
				Db.Database.Connection.Execute(endScoreSql, new { ExecutionId = ExecutionRecord.ID, model.VersionUids });	// End-date asset scores where this measure is the only one that was present (a one-measure score).
				Db.CreateCheckDependencyRemovedResultExecution(model.VersionUids);
			}

			// Only delete this execution if there is nothing to do here.
			updateExecution(Db.Connection, ExecutionRecord, true, shouldDeleteAfterCompletion: (executionItems.Count == 0));
		}
	}
}
