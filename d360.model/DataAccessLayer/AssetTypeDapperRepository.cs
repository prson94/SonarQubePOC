using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d360.core.entities;
using d360.model.DataAccessLayer.repositories;

namespace d360.model.DataAccessLayer
{
    internal sealed class AssetTypeDapperRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IAssetTypeRepository
    {
        public AssetTypeDapperRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
        {
        }

        public async Task<ICollection<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
        {
            const string sql = @"
WITH cte AS (  
	select	*, 
			0 as lvl
	from	AssetType
	where	[uid] = @assetUid
	union all
	select	a.*,
			cte.lvl - 1 
	from	IntersectType it
            inner join [Predicate] p on it.PredicateID = p.ID and p.Type IN (3) 
			inner join cte on cte.ID = it.ObjectAssetTypeID 
            inner join AssetType a on a.ID = it.SubjectAssetTypeID
)
select		*
from		cte
order by	lvl
            ";
            var result = await QueryComposer.QueryListAsync<AssetType>(sql, new { assetUid });
            return result.ToList();
        }
    }
}
