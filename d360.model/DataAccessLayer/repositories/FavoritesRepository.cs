using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.enums;

namespace d360.model.DataAccessLayer.repositories
{
	internal sealed class FavoritesRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IFavoritesRepository
	{
		public FavoritesRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
		{
		}

		public async Task<IReadOnlyList<FavoriteShortModel>> GetFavorites(int resourceId, bool homePageOnly)
		{
			string sql = $@"
							select 
								favorite.Id,
								favorite.Route,
								favorite.IsHomePage,
							favorite.SortOrder
							from dbo.Favorite favorite
							where favorite.ResourceId = @resourceId
							and ((@homePageOnly = 0) or (favorite.IsHomePage = 1))";

			var results = await QueryComposer.QueryMultipleAsync(sql, new { resourceId, homePageOnly });

			return (await results.ReadListAsync<FavoriteShortModel>());
		}

		public async Task<IReadOnlyList<FavoritesObjectDetailsResponse>> GetFavoriteDetails(
			IEnumerable<FavoritesObjectDetailsRequest> items
		)
		{
			var correctItems = items.Distinct();

			var grid = await QueryComposer.QueryMultipleAsync(@"
																declare @assets table (
																	[FavoriteId] [int] not null,
																	[ObjectType] [varchar](25) not null,
																	[ObjectId] [int] not null,
																	[Uid] [uniqueidentifier] not null,
																	[TypeObjectId] int null,
																	[Name] [varchar](max) not null,
																	[AssetTypeClass] [int] not null
																)
	
																declare @assetTypes table (
																	[FavoriteId] [int] not null,
																	[ObjectType] [varchar](25) not null,
																	[ObjectId] [int] not null,
																	[Uid] [uniqueidentifier] not null,
																	[TypeObjectId] int null,
																	[Name] [varchar](max) not null,
																	[AssetTypeClass] [int] not null
																)

																declare @semanticTypes table (
																	[FavoriteId] [int] not null,
																	[ObjectType] [varchar](25) not null,
																	[ObjectId] [int] not null,
																	[Uid] [uniqueidentifier] not null,
																	[TypeObjectId] int null,
																	[Name] [varchar](max) not null,
																	[AssetTypeClass] [int] not null
																)
																

																insert into @assets
																select 
																distinct 
																fav.FavoriteId,
																fav.ObjectType,
																fav.ObjectId,
																fav.Uid,
																assetType.ObjectID as TypeObjectId,
																AssetName.DisplayValue,
																assetType.Class
																from (
																select 
																	favorite.FavoriteId,
																	asset.Object as ObjectType,
																	asset.ID as ObjectId,
																	asset.uid as Uid,
																	asset.AssetTypeId,
																	asset.id
																from @favorites favorite
																join Asset asset 
																	on 
																	(
																		((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
																		and favorite.ObjectId = asset.ObjectId
																	)
																	union all
																	select 
																		favorite.FavoriteId,
																		asset.Object as ObjectType,
																		asset.ID as ObjectId,
																		asset.uid as Uid,
																		asset.AssetTypeId,
																		asset.id
																	from @favorites favorite
																	join Asset asset 
																	on(
																		((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
																		and favorite.AssetId = asset.Id
																	)
																	union all
																	select 
																		favorite.FavoriteId,
																		asset.Object as ObjectType,
																		asset.ID as ObjectId,
																		asset.uid as Uid,
																		asset.AssetTypeId,
																		asset.id
																	from @favorites favorite
																	join Asset asset 
																	on(
																		((favorite.ObjectType is null) or (favorite.ObjectType = asset.Object))
																		and favorite.Uid = asset.Uid
																	)
																	union all
																	select 
																		favorite.FavoriteId,
																		asset.Object as ObjectType,
																		asset.ID as ObjectId,
																		asset.uid as Uid,
																		asset.AssetTypeId,
																		asset.id
																	from @favorites favorite
																	join Asset asset 
																	on(
																		favorite.ObjectId = asset.ID
																	)
																	union all
																	select 
																		favorite.FavoriteId,
																		asset.Object as ObjectType,
																		asset.ID as ObjectId,
																		asset.uid as Uid,
																		asset.AssetTypeId,
																		asset.id
																	from @favorites favorite
																	join Asset asset 
																	on(
																	favorite.Uid = asset.Uid)
																) fav
																inner join AssetType assetType on fav.AssetTypeId = assetType.Id
																inner join AssetDisplayValue AssetName on AssetName.AssetID = fav.ID

																insert into @assetTypes
																select 
																	favorite.FavoriteId,
																	assetType.Object as ObjectType,
																	assetType.ObjectID as ObjectId,
																	assetType.uid as Uid,
																	null as TypeObjectId,
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
																	or (favorite.Uid = assetType.Uid)

																drop table if exists #semsummy;
																select favorite.Uid,
																max(s.ID) as SemanticId
																into #semsummy
																from @favorites favorite
																inner join Semantic s on s.[Uid]=favorite.Uid
																group by favorite.Uid;


																create index idx_semsummy on #semsummy(Uid);

																insert into @semanticTypes
																select 
																	favorite.FavoriteId,
																	isnull(favorite.ObjectType, 'SemanticType') as ObjectType,
																	s.ID as ObjectId,
																	favorite.Uid as Uid,
																	null as TypeObjectId,
																	s.Name,
																	18
																from @favorites favorite
																inner join #semsummy sy on sy.[Uid]=favorite.Uid
																inner join Semantic s on s.[Uid]=sy.[Uid] and s.[id] = sy.[SemanticId]

																select favorite.*
																from @assets favorite

																union

																select favorite.*
																from @assetTypes favorite

																union

																select favorite.*
																from @semanticTypes favorite;

																select favorite.FavoriteId, breadcrumbs.Level, breadcrumbs.Name
																from @assets favorite
																cross apply dbo.GetAssetBreadcrumbs(favorite.uid) as breadcrumbs
	
																union
	
																select favorite.FavoriteId, breadcrumbs.Level, breadcrumbs.TypeName as Name
																from @assetTypes favorite
																cross apply dbo.GetAssetTypeBreadcrumbs(favorite.ObjectType, favorite.ObjectId) as breadcrumbs	
																union	
																select favorite.FavoriteId, 0, favorite.Name as Name
																from @semanticTypes favorite",
			new { favorites = correctItems.AsUDTParameter() });
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
									  Uid = favorite.Uid,
									  TypeObjectId = favorite.TypeObjectId,
									  AssetTypeClass = favorite.AssetTypeClass,
									  Breadcrumbs = breadcrumbsGroup.Select(b => new BreadcrumbsInfo
									  {
										  Level = b.Level,
										  Name = b.Name
									  }).ToList()
								  };

			return favoritesMapped.ToList();
		}

		public async Task<int?> GetFavoriteIdByRoute(int resourceId, string route)
		{
			string sql = $@"
							select 
								favorite.Id
							from dbo.Favorite favorite
							where favorite.ResourceId = @resourceId
							and favorite.Route = @route
							";

			return await QueryComposer.QuerySingleOrDefaultAsync<int?>(sql, new { resourceId, route });
		}

		public class FavoriteItem
		{
			public int FavoriteId { get; set; }

			public SystemObjects ObjectType { get; set; }

			public int ObjectId { get; set; }

			public Guid Uid { get; set; }

			public int? TypeObjectId { get; set; }

			public string Name { get; set; }

			public AssetTypeClass AssetTypeClass { get; set; }
		}

		public class FavoriteBreadcrumbItem
		{
			public int FavoriteId { get; set; }

			public int Level { get; set; }

			public string Name { get; set; }
		}
	}
}
