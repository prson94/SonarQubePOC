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
                   SELECT *
                        , 0 as lvl
                     FROM AssetType
                    WHERE AssetType.[uid] = @assetUid
                   
                   UNION ALL
                   
                   SELECT AssetType.*
                        , cte.lvl - 1 
                     FROM IntersectType it
                    INNER JOIN[Predicate] p ON it.PredicateID = p.ID and p.Type IN(3,4) 
                    INNER JOIN cte ON cte.ID = it.ObjectAssetTypeID 
                    INNER JOIN AssetType ON AssetType.ID = it.SubjectAssetTypeID 
                )
                SELECT *
                  FROM cte
                 ORDER BY lvl
            ";
            var result = await QueryComposer.QueryListAsync<AssetType>(sql, new { assetUid });
            return result.ToList();
        }
    }
}
