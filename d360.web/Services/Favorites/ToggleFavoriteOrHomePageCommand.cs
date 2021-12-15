using d360.core.entities;
using d360.core.entities.Membership;
using d360.model;
using d360.model.DataAccessLayer;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace d360.web.Services.Favorites
{
    public class ToggleFavoriteOrHomePageCommand : IRequestHandler<ToggleFavoriteOrHomePageCommand.Argument, Unit>
    {
        private readonly IMembershipRepository membershipRepository;
        private readonly ICompanyContext companyContext;
        private readonly FavoriteRouteMatcherService matcherService;
        private readonly IFavoritesRepository favoritesRepository;

        public ToggleFavoriteOrHomePageCommand(
            IMembershipRepository membershipRepository,
            ICompanyContext companyContext,
            FavoriteRouteMatcherService matcherService,
            IFavoritesRepository favoritesRepository)
        {
            this.membershipRepository = membershipRepository;
            this.companyContext = companyContext;
            this.matcherService = matcherService;
            this.favoritesRepository = favoritesRepository;
        }

        public async Task<Unit> Handle(Argument request, CancellationToken cancellationToken)
        {
            request.Route = request.Route.Trim();

            await CheckIsValidFavorite(request);

            var isNewHomePage = await GetIsNewHomePage(request);

            await membershipRepository.ToggleFavorite(
                request.ResourceId,
                new FavoriteApiModel
                {
                    Route = request.Route
                },
                isNewHomePage
                );

            return Unit.Value;
        }

        private async Task CheckIsValidFavorite(Argument request)
        {
            var routeMatch = matcherService.MatchRoute(new FavoriteShortModel
            {
                Route = request.Route,
            });

            var favoriteDetails = (await favoritesRepository.GetFavoriteDetails(new[] { routeMatch.ObjectId })).SingleOrDefault();
            var isCorrect = IsCorrectFavorite(routeMatch, favoriteDetails);
            if (!isCorrect)
            {
                throw new InvalidOperationException($"" +
                    $"Failed to find object {JsonConvert.SerializeObject(routeMatch.ObjectId)} " +
                    $"in order to match favorite route {request.Route}");
            }
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

        public class Argument : IRequest<Unit>
        {
            public int ResourceId { get; set; }

            public string Route { get; set; }

            public bool IsHomePage { get; set; }
        }
    }
}