using d360.core.entities;
using d360.core.entities.Membership;
using d360.model;
using d360.model.DataAccessLayer;
using MediatR;
using Resources;
using SmartFormat;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace d360.web.Services.Favorites
{
    public class ToggleFavoriteOrHomePageCommand : IRequestHandler<ToggleFavoriteOrHomePageCommand.Argument, Unit>
    {
        private readonly ICompanyContext companyContext;
        private readonly FavoriteRouteMatcherService matcherService;
        private readonly IFavoritesRepository favoritesRepository;

        public ToggleFavoriteOrHomePageCommand(
            ICompanyContext companyContext,
            FavoriteRouteMatcherService matcherService,
            IFavoritesRepository favoritesRepository)
        {
            this.companyContext = companyContext;
            this.matcherService = matcherService;
            this.favoritesRepository = favoritesRepository;
        }

        public async Task<Unit> Handle(Argument request, CancellationToken cancellationToken)
        {
            request.Route = request.Route.Trim();

            var routeMatch = matcherService.MatchRoute(new FavoriteShortModel
            {
                Route = request.Route,
            });

            if (routeMatch.Matcher.PageType == FavoritePageType.Unknown)
            {
                throw new InvalidRequestException(Smart.Format(ApiMessages.FavoriteUnknownRoute, new { request.Route }));
            }

            var favoriteDetails = (await favoritesRepository.GetFavoriteDetails(new[] { routeMatch.ObjectId })).SingleOrDefault();
            var isCorrect = IsCorrectFavorite(routeMatch, favoriteDetails);
            if (!isCorrect)
            {
                throw new InvalidRequestException(Smart.Format(ApiMessages.FavoriteUnknownObject, new { request.Route }));
            }

            request.Route = matcherService.GetNormalizedRoute(request.Route, routeMatch.Matcher, favoriteDetails);

            var isNewHomePage = await GetIsNewHomePage(request);

            await ToggleFavorite(
                request,
                routeMatch,
                favoriteDetails,
                isNewHomePage
                );

            return Unit.Value;
        }

        private bool IsCorrectFavorite(FavoriteRouteMatchResult routeMatch, FavoritesObjectDetailsResponse favoriteDetails)
        {
            var isNonArtifact = routeMatch.Matcher.PageType != FavoritePageType.Artifact;
            var favoriteExists = favoriteDetails != null;
            return isNonArtifact || favoriteExists;
        }

        private async Task<bool> GetIsNewHomePage(Argument request)
        {
            if (!request.IsHomePage)
            {
                return false;
            }

            var currentHome = await companyContext.Filter<Favorite>(x => x.ResourceID == request.ResourceId && x.IsHomePage).FirstOrDefaultAsync();
            bool isNewHomePage = true;
            if (currentHome != null)
            {
                if (request.Route == currentHome.Route)
                {
                    isNewHomePage = false;
                }
            }

            return isNewHomePage;
        }

        private async Task ToggleFavorite(
            Argument request,
            FavoriteRouteMatchResult routeMatch,
            FavoritesObjectDetailsResponse @object,
            bool isHomepage = false)
        {
            var newFavorite = new Favorite()
            {
                ResourceID = request.ResourceId,
                Route = request.Route,

                IsHomePage = isHomepage,
                Type = routeMatch.Matcher.PageType.ToString(),
                Name = routeMatch.Matcher.GetName(@object?.Name, routeMatch.RouteParams),
                Object = @object?.ObjectType.ToString(),
                ObjectID = @object?.ObjectId
            };

            // only 1 home page allowed at once, remove old one(s)
            if (newFavorite.IsHomePage)
            {
                var favorites = await companyContext.Filter<Favorite>(f => f.ResourceID == request.ResourceId && f.IsHomePage).ToListAsync();
                companyContext.Favorites.RemoveRange(favorites);
                await companyContext.SaveChangesAsync();
            }

            var routes = matcherService.GetAllPossibleRoutes(request.Route, routeMatch.Matcher, @object);
            var existing = await companyContext.Favorites.FirstOrDefaultAsync(f => f.ResourceID == newFavorite.ResourceID && routes.Contains(f.Route));
            if (existing == null)
            {
                companyContext.Add(newFavorite);
            }
            else
            {
                if (existing.IsHomePage != newFavorite.IsHomePage)
                {
                    existing.IsHomePage = newFavorite.IsHomePage;
                    existing.Type = newFavorite.Type;
                    existing.Name = newFavorite.Name;
                    existing.Object = newFavorite.Object;
                    existing.ObjectID = newFavorite.ObjectID;

                    companyContext.Update(existing);
                }
                else
                {
                    companyContext.Delete(existing);
                }
            }
        }

        public class Argument : IRequest<Unit>
        {
            public int ResourceId { get; set; }

            public string Route { get; set; }

            public bool IsHomePage { get; set; }
        }
    }
}