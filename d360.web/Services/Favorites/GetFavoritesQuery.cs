using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities.Membership;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    public class GetFavoritesQuery : IRequestHandler<GetFavoritesQuery.Request, IEnumerable<FavoriteExtendedApiViewModel>>
    {
        private readonly IFavoritesRepository favoritesRepository;

        public GetFavoritesQuery(IFavoritesRepository favoritesRepository)
        {
            this.favoritesRepository = favoritesRepository;
        }

        public async Task<IEnumerable<FavoriteExtendedApiViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            // TODO: cleanup non-existing favorites & homepages
            var favorites = await favoritesRepository.GetFavorites(request.ResourceId);
            var objectIds = favorites.Select(GetObjectId).ToList();
            var favoritesDetails = await favoritesRepository.GetFavoriteDetails(objectIds);
            var relatedMatchers = favorites.Select(f => new { f.Id, Matcher = GetCorrespondingMatcher(f) }).ToList();

            var mappedFavorites = from favorite in favorites
                                  join objectId in objectIds on favorite.Id equals objectId.FavoriteId
                                  join favoriteDetails in favoritesDetails on favorite.Id equals favoriteDetails.FavoriteId
                                  join relatedMatcher in relatedMatchers on favorite.Id equals relatedMatcher.Id
                                  select new FavoriteExtendedApiViewModel
                                  {
                                      Id = favorite.Id,
                                      Route = favorite.Route,
                                      Type = relatedMatcher.Matcher.Type,
                                      // TODO: get rid of first item in breadcrumbs
                                      Breadcrumbs = favoriteDetails.Breadcrumbs.OrderBy(p => p.Level).Select(p => p.Name).ToList(),
                                      ObjectType = favoriteDetails.ObjectType,
                                      ObjectId = favoriteDetails.ObjectId,
                                      Name = favoriteDetails.Name,
                                      TabName = relatedMatcher.Matcher.TabName,
                                  };

            return mappedFavorites.ToList();
        }

        public class Request : IRequest<IEnumerable<FavoriteExtendedApiViewModel>>
        {
            public int ResourceId { get; set; }
        }

        private FavoriteRouteMatcher GetCorrespondingMatcher(FavoriteShortModel f)
        {
            foreach (var matcher in matchers)
            {
                var mapped = TryMatchRoute(matcher, f.Route);
                if (mapped == null)
                {
                    continue;
                }

                return matcher;
            }

            throw new InvalidOperationException($"Failed to match favorite with route {f.Route}");
        }

        private FavoritesObjectDetailsRequest GetObjectId(FavoriteShortModel f)
        {
            foreach (var matcher in matchers)
            {
                var mapped = TryGetObjectId(f, matcher);
                if (mapped != null)
                {
                    return mapped;
                }
            }

            throw new InvalidOperationException($"Failed to match favorite with route {f.Route}");
        }

        private FavoritesObjectDetailsRequest TryGetObjectId(FavoriteShortModel f, FavoriteRouteMatcher matcher)
        {
            var match = TryMatchRoute(matcher, f.Route);
            if (match == null)
            {
                return null;
            }

            var request = new FavoritesObjectDetailsRequest();
            request.FavoriteId = f.Id;
            request.ObjectType = matcher.ObjectType;

            if (match.ContainsKey("assetId"))
            {
                request.AssetId = int.Parse(match["assetId"]);
            }

            if (match.ContainsKey("objectId"))
            {
                request.ObjectId = int.Parse(match["objectId"]);
            }

            if (match.ContainsKey("uid"))
            {
                request.Uid = Guid.Parse(match["uid"]);
            }

            return request;
        }

        private Dictionary<string, string> TryMatchRoute(FavoriteRouteMatcher matcher, string route)
        {
            route = SanitizeRoute(route);
            var routePatternRegex = RoutePatternToRegex(matcher.RoutePattern);
            var match = routePatternRegex.Match(route);
            if (!match.Success)
            {
                return null;
            }

            return match.Groups
                .OfType<Group>()
                .ToDictionary(g => g.Name, g => g.Value);
        }

        private static string SanitizeRoute(string route)
        {
            return route.TrimEnd('/');
        }

        private static Regex RoutePatternToRegex(string routePattern)
        {
            routePattern = routePattern.Replace(@"\", @"\\");
            routePattern = routePattern + "$";

            var parameterNames = new Regex(@":(\w+)")
                .Matches(routePattern)
                .OfType<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            foreach (var parameterName in parameterNames)
            {
                routePattern = routePattern.Replace(
                    $":{parameterName}",
                    $@"(?<{parameterName}>[\w\d\-]+?)");
            }

            return new Regex(routePattern, RegexOptions.IgnoreCase);
        }

        private static IEnumerable<FavoriteRouteMatcher> matchers = new[]
        {
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/:any/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Definition", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Impact Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Lineage",
                Type = FavoriteExtendedType.Asset,
                TabName = "Lineage Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/relationships/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Relationships", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ownership/:assetId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Responsibilities", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/actions/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Actions", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/comments/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Comments", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/followers/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Followers", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/audit/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Change Log", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            }
        };

        class FavoriteRouteMatcher
        {
            public string RoutePattern { get; set; }

            public FavoriteExtendedType Type { get; set; }

            public string TabName { get; set; }

            public SystemObjects ObjectType { get; set; }
        }
    }
}