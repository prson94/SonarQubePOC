using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class AssetTypeRepository : Repository, IAssetTypeRepository
	{
		public async Task<IEnumerable<AssetType>> GetAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			using (var db = new SqlConnection(ConnectionString)) 
			{ 
				await db.OpenAsync(cancellationToken);
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
order by	lvl";

				return await db.QueryAsync<AssetType>(sql, new { assetUid });
			}
		}
	}
}
