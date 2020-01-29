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

        public List<AssetTypeClass> AllowedClassesForScoreType()
        {
            return new List<AssetTypeClass>
                {
                    AssetTypeClass.BusinessAsset,
                    AssetTypeClass.TechnicalAsset,
                    AssetTypeClass.Model,
                    AssetTypeClass.Policy,
                    AssetTypeClass.Rule
                };
        }

        public List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams, out string error)
        {
            error = string.Empty;
            List<string> whereStatements = new List<string>();

            var dbArgs = new DynamicParameters();

            foreach (var kp in queryParams)
            {
                switch (kp.Key.ToLower())
                {
                    case "_state":
                        State stateValue;
                        Enum.TryParse(kp.Value, true, out stateValue);

                        if ((stateValue != State.Active && stateValue != State.Deleted) || string.IsNullOrEmpty(kp.Value))
                        {
                            error = "Invalid state value specified.";
                            return null;
                        }

                        whereStatements.Add("AL.[state] = @state");
                        dbArgs.Add("@state", stateValue);
                        break;

                    case "assetclassname":
                        var classList = AssetTypeClass.Generic.GetAsList();
                        var filteredClasses = classList.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
                        whereStatements.Add("AT.class in @filteredClasses");
                        dbArgs.Add("@filteredClasses", filteredClasses);
                        break;

                    case "assettypepath":
                        whereStatements.Add("P.[Path] like @pathname");
                        dbArgs.Add("@pathname", "%" + kp.Value.Trim() + "%");
                        break;

                    case "scoretype":
                        var sc = kp.Value.Trim();
                        var scoretypeInfos = ScoreType.DataQuality.GetAsList();
                        var filteredScoreTypes = scoretypeInfos.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
                        whereStatements.Add("AL.scoreType in @filteredScoreTypesGlobal");
                        dbArgs.Add("@filteredScoreTypesGlobal", filteredScoreTypes);
                        break;

                    case "global":
                        List<string> globalFilters = new List<string>();
                        var classListGlobal = AssetTypeClass.Generic.GetAsList();
                        var filteredClassesGlobal = classListGlobal.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
                        globalFilters.Add("AT.class in @filteredClassesGlobal");
                        dbArgs.Add("@filteredClassesGlobal", filteredClassesGlobal);

                        globalFilters.Add("P.[Path] like @pathnameGlobal");
                        dbArgs.Add("@pathnameGlobal", "%" + kp.Value.Trim() + "%");

                        var scGlobal = kp.Value.Trim();
                        var scoretypeInfosGlobal = ScoreType.DataQuality.GetAsList();
                        var filteredScoreTypesGlobal = scoretypeInfosGlobal.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
                        globalFilters.Add("AL.scoreType in @filteredScoreTypesGlobal");
                        dbArgs.Add("@filteredScoreTypesGlobal", filteredScoreTypesGlobal);

                        whereStatements.Add($"({string.Join(" or ", globalFilters)})");

                        break;

                    default: break;
                }
            }

            //Defaults
            if (dbArgs.ParameterNames.Contains("@state"))
            {
                whereStatements.Add("AL.[state] = @state");
                dbArgs.Add("@state", State.Active);
            }

            string sqlWhere = whereStatements.Count > 0 ? " where " + string.Join(" and ", whereStatements) : "";
            var sql = $@"select 
	                        AL.uid,
	                        AT.class as assetClassName,
	                        AL.assettypeuid,
	                        P.[Path] as assetTypePath,
	                        AL.scoreType,
	                        AL.[state],
                            case 
                                when Measures.F > 0 then 1
								else 0
							end as hasMeasure
                        from metrics.Allocation AL
	                        inner join AssetType AT on AT.uid = AL.assettypeuid                                    
	                        cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
                            cross apply (select count(*) from metrics.Asset where State = 1 and AssetTypeUid = AL.AssetTypeUid and ScoreType = AL.ScoreType)Measures(F)
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
                alloc.AssetTypeUid = model.assetTypeUid;
                alloc.ScoreType = model.scoreType;
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
            alloc.AssetTypeUid = model.assetTypeUid;
            alloc.ScoreType = model.scoreType;
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

        public async Task<List<AllocationApiGetUnallocatedAssetTypeModel>> GetUnallocatedAssetTypes(ScoreType scoreType)
        {
            var dbArgs = new DynamicParameters();
            dbArgs.Add("@scoreType", (int)scoreType);

            dbArgs.Add("@supportedAssetClasses", AllowedClassesForScoreType().Select(x => (int)x));

            var sql = $@"select 
	                        att.[uid] as assetTypeUid,
	                        atp.Path as assetTypePath,
	                        att.Class as assetTypeClass
                        from
	                        [dbo].[assettype] att
	                        cross apply [dbo].[GetAssetTypeTextPathById](att.id,'/') atp
                        where 
	                        att.class in @supportedAssetClasses
		                        and
	                        not exists (select 1 from [metrics].Allocation a where a.[state] = 1 and a.assettypeuid = att.[uid] and a.scoretype = @scoreType)";

            return (await companyContext.QueryAsync<AllocationApiGetUnallocatedAssetTypeModel>(sql, dbArgs)).ToList();

        }
    }
}
