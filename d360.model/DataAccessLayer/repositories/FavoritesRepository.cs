using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
    internal sealed class FavoritesRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IFavoritesRepository
    {
        public FavoritesRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
        {
        }

        // TODO: think if this is good place for this method
        public async Task<IReadOnlyList<BreadcrumbItemResponse>> GetBreadcrumbs(IEnumerable<BreadcrumbForObjectRequest> items)
        {
            var distinctItems = items
                .Select(i => new ObjectsTableUDT
                {
                    ObjectId = i.ObjectId,
                    ObjectType = i.ObjectType.ToString()
                })
                .Distinct();

            var grid = await this.QueryComposer.QueryMultipleAsync(@"
select 
	o.ObjectId as ForObjectId,
	o.ObjectType as ForObjectType,
	breadcrumbs.*
from @objects as o
outer apply dbo.GetBreadcrumbs(o.ObjectType, o.ObjectId) as breadcrumbs
", new { objects = distinctItems.AsUDTParameter() });

            return await grid.ReadListAsync<BreadcrumbItemResponse>();
        }
    }
}
