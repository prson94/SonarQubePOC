using d360.core.entities;
using d360.core.entities.Scoring;
using d360.core.enums;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ScoringRepository : IScoringRepository
    {
        ICompanyContext companyContext;
        public ScoringRepository(ICompanyContext companyContext)
        {
            this.companyContext = companyContext;
        }
        public List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            List<string> whereStatements = new List<string>();
            var dbArgs = new DynamicParameters();

            foreach (var kp in queryParams)
            {
                switch (kp.Key.ToLower())
                {
                    case "_state":
                        var value = kp.Value;
                        State stateValue = (State)Enum.Parse(typeof(State), value);
                        whereStatements.Add("AL.[state] = @state");
                        dbArgs.Add("@state", stateValue);
                        break;
                    default: break;
                }
            }
            string sqlWhere = whereStatements.Count > 0 ? " where " + string.Join(" and ", whereStatements) : "";
            var sql = $@"select 
	                        AL.uid,
	                        AT.class as assetClassName,
	                        AL.assettypeuid,
	                        P.[Path] as assetTypePath,
	                        AL.scoreType,
	                        AL.[state]
                        from metrics.Allocation AL
	                        inner join AssetType AT on AT.uid = AL.assettypeuid                                    
	                        cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
                        {sqlWhere}
                        ";

            List<AllocationApiGetModel> allocations = companyContext.Query<AllocationApiGetModel>(sql, dbArgs).ToList();
            return allocations;
        }

        public AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref ScoreTypeAllocation alloc)
        {
            if (alloc != null)
            {
                alloc.State = State.Active;
                alloc.UpdatedBy = companyContext.CurrentResourceID;
                alloc.UpdatedOn = DateTime.UtcNow;
                companyContext.SaveChanges();
            }
            else
            {
                alloc = new ScoreTypeAllocation();
                alloc.AssetTypeUid = model.assetTypeUid.Value;
                alloc.ScoreType = model.scoreType.Value;
                alloc.CreatedBy = alloc.UpdatedBy = companyContext.CurrentResourceID;
                alloc.CreatedOn = alloc.UpdatedOn = DateTime.UtcNow;
                companyContext.ScoreTypeAllocations.Add(alloc);
                companyContext.SaveChanges();

            }


            var dbArgs = new DynamicParameters();
            dbArgs.Add("@uid", alloc.Uid);

            var sql = $@"select 
	                        AL.uid,
	                        AT.class as assetClassName,
	                        AL.assettypeuid,
	                        P.[Path] as assetTypePath,
	                        AL.scoreType,
	                        AL.[state]
                        from metrics.Allocation AL
	                        inner join AssetType AT on AT.uid = AL.assettypeuid                                    
	                        cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
                        where AL.uid = @uid";

            AllocationApiGetModel allocation = companyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();
            return allocation;
        }

        public AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, ScoreTypeAllocation alloc)
        {
            alloc.AssetTypeUid = model.assetTypeUid.Value;
            alloc.ScoreType = model.scoreType.Value;
            alloc.UpdatedBy = companyContext.CurrentResourceID;
            alloc.UpdatedOn = DateTime.UtcNow;
            companyContext.SaveChanges();



            var dbArgs = new DynamicParameters();
            dbArgs.Add("@uid", alloc.Uid);

            var sql = $@"select 
	                        AL.uid,
	                        AT.class as assetClassName,
	                        AL.assettypeuid,
	                        P.[Path] as assetTypePath,
	                        AL.scoreType,
	                        AL.[state]
                        from metrics.Allocation AL
	                        inner join AssetType AT on AT.uid = AL.assettypeuid                                    
	                        cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
                        where AL.uid = @uid";

            AllocationApiGetModel allocation = companyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();
            return allocation;
        }

        public void DeleteAllocation(ScoreTypeAllocation alloc)
        {
            alloc.UpdatedBy = companyContext.CurrentResourceID;
            alloc.UpdatedOn = DateTime.UtcNow;
            alloc.State = State.Deleted;
            companyContext.SaveChanges();
        }


        public bool HasActiveMeasures(ScoreTypeAllocation alloc)
        {
            return companyContext.MetricAssets.Any(x => x.State == State.Active && x.AssetTypeUid == alloc.AssetTypeUid && x.ScoreType == alloc.ScoreType);
        }

        public bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model)
        {
            return companyContext.ScoreTypeAllocations.Any(x => x.Uid != allocationUid && x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
        }

        public ScoreTypeAllocation GetAllocationByUid(Guid allocationUid)
        {
            return companyContext.ScoreTypeAllocations.FirstOrDefault(x => x.Uid == allocationUid);
        }

        public ScoreTypeAllocation GetAllocationByModel(AllocationApiUpsertModel model)
        {
            return companyContext.ScoreTypeAllocations.FirstOrDefault(x => x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
        }
    }
}
