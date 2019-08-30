using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using igx.jobs;
using d360.utils.company;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace igx.jobs.assetprocessor
{
    public class GraphSync
    {
        const string functionName = "AssetProcessor_GraphSync";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else
        const string timerSettings = "0 */2 * * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
#if DEBUG
            var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 4).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif


            foreach(var company in companies)
            {
                try
                {
                    var conn = CompanyConnectionUtils.GetCompanyConnection(company.CompanyID, company.Server, company.Username, company.Password);

                    using (conn)
                    {
                        const int timeout = 1000 * 60 * 10;

                        conn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        using (var trans = conn.BeginTransaction())
                        {
                            try
                            {
                                await conn.ExecuteAsync(@"
    delete E
    from graph.AssetEdge E
    where E.ID not in (select ID from [Intersect])", transaction: trans, commandTimeout: timeout);

                                await conn.ExecuteAsync(@"
    delete N
	from graph.AssetNode N
	where N.ID not in (select ID from Asset)", transaction: trans, commandTimeout: timeout);

                                await conn.ExecuteAsync(@"
merge graph.AssetNode T
	using
	(
		select
			A.ID,
			A.[Uid],
			A.AssetTypeID,
			T.[UID] as AssetTypeUid,
			A.[State],
			A.UpdatedOn
		from Asset A
		inner join AssetType T on T.ID = A.AssetTypeID
	) S on (S.ID = T.ID)
	when matched and (T.UpdatedOn < S.UpdatedOn) then update 
		set T.[State] = S.[State],
			T.[UpdatedOn] = S.[UpdatedOn],
			T.AssetTypeID = S.AssetTypeID,
			T.AssetTypeUid = S.AssetTypeUid
	when not matched by target then insert 
		(ID, [Uid], AssetTypeID, AssetTypeUid, [State], UpdatedOn) values
		(ID, [Uid], AssetTypeID, AssetTypeUid, [State], UpdatedOn)", transaction: trans, commandTimeout: timeout);

                                await conn.ExecuteAsync(@"
merge into graph.AssetEdge T
using
	(
		select
			SG.$node_id as from_id,
			OG.$node_id as to_id,
			I.ID,
			I.[Uid],
			I.IntersectTypeID,
			T.[Uid] as IntersectTypeUid,
			T.PredicateID,
			P.[Uid] as PredicateUid,
			P.[Type] as PredicateType,
			'<props/>' as Properties,
			I.[State],
			coalesce(I.UpdatedOn, I.CreatedOn, getutcdate()) as UpdatedOn
		from	[Intersect] I
				inner join Asset SA on SA.[Object] = I.[Subject] and SA.ObjectID = I.SubjectID
				inner join graph.AssetNode SG on SG.ID = SA.ID
				inner join Asset OA on OA.[Object] = I.[Object] and OA.ObjectID = I.ObjectID
				inner join graph.AssetNode OG on OG.ID = OA.ID
				inner join IntersectType T on T.ID = I.IntersectTypeID
				inner join [Predicate] P on P.ID = T.PredicateID
	) S on (S.ID = T.ID and S.from_id = T.$from_id and S.to_id = T.$to_id)
	when matched and (T.UpdatedOn < S.UpdatedOn) then update 
		set 
			T.IntersectTypeID = S.IntersectTypeID,
			T.IntersectTypeUid = S.IntersectTypeUid,
			T.PredicateID = S.PredicateID,
			T.PredicateUid = S.PredicateUid,
			T.PredicateType = S.PredicateType,
			T.[State] = S.[State]
	when not matched by target then insert 
		($from_id, $to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn) values
		(from_id, to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn)", transaction: trans, commandTimeout: timeout);


                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                            }

                        }

                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

        }
    }
}
