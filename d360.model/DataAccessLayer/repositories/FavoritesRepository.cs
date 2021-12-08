using d360.core;
using d360.core.enums;
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

        public async Task<IReadOnlyList<FavoriteShortModel>> GetFavorites(int resourceId)
        {
            string sql = $@"
select 
	favorite.Id,
	favorite.Route,
	favorite.IsHomePage,
favorite.SortOrder
from dbo.Favorite favorite
where favorite.ResourceId = @resourceId";

            var results = await QueryComposer.QueryMultipleAsync(sql, new { resourceId });

            return (await results.ReadListAsync<FavoriteShortModel>());
        }

        public async Task<IReadOnlyList<FavoritesObjectDetailsResponse>> GetFavoriteDetails(
            IEnumerable<FavoritesObjectDetailsRequest> items
        )
        {
            var correctItems = items.Distinct();

            var grid = await this.QueryComposer.QueryMultipleAsync(@"
	declare @correctFavorites table (
		[FavoriteId] [int] not null,
		[ObjectType] [varchar](25) not null,
		[ObjectId] [int] not null,
		[Name] [varchar](max) not null,
		[AssetTypeClass] [int] not null
	)
	
	insert into @correctFavorites
	select 
		favorite.FavoriteId,
		asset.Object as ObjectType,
		asset.ObjectID as ObjectId,
		AssetName.DisplayValue,
		assetType.Class
	from @favorites favorite
	join dbo.Asset asset 
		on 
		(
			((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
			and favorite.ObjectId = asset.ObjectId
		)
		or (
			((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
			and favorite.AssetId = asset.Id
		) 
		or (
			((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
			and favorite.Uid = asset.Uid
		)
	join dbo.AssetType assetType
		on asset.AssetTypeId = assetType.Id
	outer apply [dbo].[GetAssetDisplayValueById](asset.Id) AssetName

	insert into @correctFavorites
	select 
		favorite.FavoriteId,
		assetType.Object as ObjectType,
		assetType.ObjectID as ObjectId,
		assetType.Name,
		assetType.Class
	from @favorites favorite
	join dbo.AssetType assetType
		on 
		(
			((favorite.ObjectType is null) or (favorite.ObjectType = assetType.Object))
			and favorite.ObjectId = assetType.ObjectId
		)
		or (
			((favorite.ObjectType is null) or (favorite.ObjectType = assetType.Object))
			and favorite.AssetTypeId = assetType.Id
		) 
		or (
			((favorite.ObjectType is null) or (favorite.ObjectType = assetType.Object))
			and favorite.Uid = assetType.Uid
		)

	select favorite.*
	from @correctFavorites favorite

	select favorite.FavoriteId, breadcrumbs.*
	from @correctFavorites favorite
	outer apply dbo.GetBreadcrumbs(favorite.ObjectType, favorite.ObjectId) as breadcrumbs
", new { favorites = correctItems.AsUDTParameter() });

            var favorites = await grid.ReadListAsync<FavoriteItem>();
            var breadcrumbs = await grid.ReadListAsync<FavoriteBreadcrumbItem>();

            var favoritesMapped = from favorite in favorites
                                  join breadcrumb in breadcrumbs
                                    on favorite.FavoriteId equals breadcrumb.FavoriteId
                                    into breadcrumbsGroup
                                  select new FavoritesObjectDetailsResponse
                                  {
                                      FavoriteId = favorite.FavoriteId,
                                      Name = favorite.Name,
                                      ObjectType = favorite.ObjectType,
                                      ObjectId = favorite.ObjectId,
                                      AssetTypeClass = favorite.AssetTypeClass,
                                      Breadcrumbs = breadcrumbsGroup.Select(b => new BreadcrumbsInfo
                                      {
                                          Level = b.Level,
                                          Name = b.Name,
                                          TypeName = b.TypeName,
                                          TypeUrl = b.TypeUrl,
                                          Url = b.Url
                                      }).ToList()
                                  };

            return favoritesMapped.ToList();
        }

        public class FavoriteItem
        {
            public int FavoriteId { get; set; }

            public SystemObjects ObjectType { get; set; }

            public int ObjectId { get; set; }

            public string Name { get; set; }

            public AssetTypeClass AssetTypeClass { get; set; }
        }


        public class FavoriteBreadcrumbItem
        {
            public int FavoriteId { get; set; }

            public int Level { get; set; }

            public string TypeName { get; set; }

            public string Name { get; set; }

            public string TypeUrl { get; set; }

            public string Url { get; set; }
        }
    }
}
