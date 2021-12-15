using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d360.core.entities.Membership;
using d360.model.DataAccessLayer;
using d360.web.Services.Favorites;
using MediatR;

namespace d360.web.Services
{
    public class GetFavoritesQuery : IRequestHandler<GetFavoritesQuery.Request, IEnumerable<FavoriteExtendedApiViewModel>>
    {
        private readonly IFavoritesRepository favoritesRepository;
        private readonly FavoriteRouteMatcherService matcherService;

        public GetFavoritesQuery(IFavoritesRepository favoritesRepository, FavoriteRouteMatcherService matcherService)
        {
            this.favoritesRepository = favoritesRepository;
            this.matcherService = matcherService;
        }

        public async Task<IEnumerable<FavoriteExtendedApiViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            var favorites = await favoritesRepository.GetFavorites(request.ResourceId, request.HomePageOnly);
            var routeMatchers = favorites.Select(matcherService.GetRouteMatch).ToList();
            var favoritesDetails = await favoritesRepository.GetFavoriteDetails(routeMatchers.Select(r => r.ObjectId));

            var mappedFavorites = from favorite in favorites
                                  join routeMatch in routeMatchers
                                    on favorite.Id equals routeMatch.ObjectId.FavoriteId
                                  join favoriteDetails in favoritesDetails
                                    on favorite.Id equals favoriteDetails.FavoriteId into joinedFavoriteDetails
                                  from favoriteDetails in joinedFavoriteDetails.DefaultIfEmpty()
                                  where IsCorrectFavorite(routeMatch, favoriteDetails)
                                  orderby favorite.SortOrder
                                  select new FavoriteExtendedApiViewModel
                                  {
                                      Id = favorite.Id,
                                      Route = favorite.Route,
                                      PageType = routeMatch.Matcher.PageType,
                                      Breadcrumbs = (favoriteDetails?.Breadcrumbs ?? new List<BreadcrumbsInfo>())
                                          .OrderBy(p => p.Level)
                                          .Select(p => p.Name)
                                          .ToList(),
                                      ObjectType = favoriteDetails?.ObjectType,
                                      ObjectId = favoriteDetails?.ObjectId,
                                      AssetTypeClass = routeMatch.Matcher.ForcedAssetClass ?? favoriteDetails?.AssetTypeClass,
                                      Name = routeMatch.Matcher.GetName(favoriteDetails?.Name, routeMatch.RouteParams)
                                  };

            return mappedFavorites.ToList();
        }

        public class Request : IRequest<IEnumerable<FavoriteExtendedApiViewModel>>
        {
            public int ResourceId { get; set; }

            public bool HomePageOnly { get; set; }
        }

        private bool IsCorrectFavorite(FavoriteRouteMatchResult routeMatch, FavoritesObjectDetailsResponse favoriteDetails)
        {
            var isNonArtifact = routeMatch.Matcher.PageType != FavoritePageType.Artifact;
            var favoriteExists = favoriteDetails != null;
            return isNonArtifact || favoriteExists;
        }

    }
}