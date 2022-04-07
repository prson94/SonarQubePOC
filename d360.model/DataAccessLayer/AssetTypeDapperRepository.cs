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
                    INNER JOIN[Predicate] p ON it.PredicateID = p.ID
                    INNER JOIN cte ON cte.ObjectID = it.ObjectID
                    INNER JOIN AssetType ON AssetType.ObjectID = it.SubjectID
                    WHERE it.[Object] = 'ArtifactType' AND p.Type IN(3,4)
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
