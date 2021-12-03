using Dapper;
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
            var distinctItems = items.Select(i => new { i.ObjectId, i.ObjectType }).Distinct();

            var grid = await this.QueryComposer.QueryMultipleAsync(@"
select 
	o.ObjectId as ForObjectId,
	o.ObjectType as ForObjectType,
	breadcrumbs.*
from @objects as o
outer apply dbo.GetBreadcrumbs(o.ObjectType, o.ObjectId) as breadcrumbs
", new { objects = GetObjects() });

            return await grid.ReadListAsync<BreadcrumbItemResponse>();

            SqlMapper.ICustomQueryParameter GetObjects()
            {
                // TODO: write sugar for that
                var dataTable = new DataTable();
                dataTable.Columns.Add("ObjectType");
                dataTable.Columns.Add("ObjectId");
                foreach (var item in distinctItems)
                {
                    dataTable.Rows.Add(item.ObjectType, item.ObjectId);
                }
                var objects = dataTable.AsTableValuedParameter("dbo.ObjectsTable");
                return objects;
            }
        }
    }
}
