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
            var relatedMatchers = favorites.Select(f =>
            {
                var matcher = GetCorrespondingMatcher(f);
                return new { f.Id, Matcher = matcher.Item1, Matched = matcher.Item2 };
            }).ToList();

            var mappedFavorites = from favorite in favorites
                                  join objectId in objectIds on favorite.Id equals objectId.FavoriteId
                                  join favoriteDetails in favoritesDetails on favorite.Id equals favoriteDetails.FavoriteId into joinedFavoriteDetails
                                  from favoriteDetails in joinedFavoriteDetails.DefaultIfEmpty()
                                  join relatedMatcher in relatedMatchers on favorite.Id equals relatedMatcher.Id
                                  select new FavoriteExtendedApiViewModel
                                  {
                                      Id = favorite.Id,
                                      Route = favorite.Route,
                                      Type = relatedMatcher.Matcher.Type,
                                      // TODO: get rid of first item in breadcrumbs
                                      Breadcrumbs = (favoriteDetails?.Breadcrumbs ?? new List<BreadcrumbsInfo>())
                                          .OrderBy(p => p.Level)
                                          .Select(p => p.Name)
                                          .ToList(),
                                      ObjectType = favoriteDetails?.ObjectType,
                                      ObjectId = favoriteDetails?.ObjectId,
                                      Name = relatedMatcher.Matcher.GetName(favoriteDetails?.Name, relatedMatcher.Matched)
                                  };

            return mappedFavorites.ToList();
        }

        public class Request : IRequest<IEnumerable<FavoriteExtendedApiViewModel>>
        {
            public int ResourceId { get; set; }
        }

        private (FavoriteRouteMatcher, Dictionary<string, string>) GetCorrespondingMatcher(FavoriteShortModel f)
        {
            foreach (var matcher in matchers)
            {
                var mapped = TryMatchRoute(matcher, f.Route);
                if (mapped == null)
                {
                    continue;
                }

                return (matcher, mapped);
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
            // please, note, that we have very limited support of query strings here
            // we don't support matching "?a=1&b=2" by pattern "?b=:b&a=:a" (i.e. when order is changed)

            routePattern = routePattern.Replace(@"\", @"\\");
            routePattern = routePattern.Replace(@"?", @"\?");

            if (routePattern.Contains("?"))
            {
                routePattern = routePattern + @"(?:&.*|$)";
            }
            else
            {
                routePattern = routePattern + "$";
            }

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
                GetName = (name, p) => name + " - "  + "Definition", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Impact Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Lineage",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Lineage Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/relationships/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Relationships", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ownership/:assetId",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Responsibilities", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/actions/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Actions", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/comments/:uid",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Comments", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/followers/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Followers", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/audit/:uid",
                Type = FavoriteExtendedType.Asset,
                GetName = (name, p) => name + " - "  + "Change Log", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "search?query=:query",
                Type = FavoriteExtendedType.SearchResultsPage,
                GetName = (_, p) => $"\"{p["query"]}\"", // TODO: resources,
            }
        };

        class FavoriteRouteMatcher
        {
            public string RoutePattern { get; set; }

            public FavoriteExtendedType Type { get; set; }

            public string TabName { get; set; }

            public SystemObjects? ObjectType { get; set; }

            public Func<string, Dictionary<string, string>, string> GetName { get; set; }
        }
    }
}