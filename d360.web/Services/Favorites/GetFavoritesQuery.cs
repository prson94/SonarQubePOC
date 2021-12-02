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
        private readonly IMembershipRepository membershipRepository;

        public GetFavoritesQuery(IMembershipRepository membershipRepository)
        {
            this.membershipRepository = membershipRepository;
        }

        public async Task<IEnumerable<FavoriteExtendedApiViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            // TODO: cleanup non-existing favorites & homepages
            var favorites = await membershipRepository.GetFavorites(request.ResourceId);
            return favorites.Select(f => Map(f)).ToList();
        }

        private FavoriteExtendedApiViewModel Map(FavoriteApiViewModel f)
        {
            foreach (var matcher in matchers)
            {
                var mapped = TryMap(f, matcher);
                if (mapped != null)
                {
                    return mapped;
                }
            }

            throw new InvalidOperationException($"Failed to match favorite with route {f.Route}");
        }

        private FavoriteExtendedApiViewModel TryMap(FavoriteApiViewModel f, Matcher matcher)
        {
            var match = TryMatch(matcher, f.Route);
            if (match == null)
            {
                return null;

            }
            var response = new FavoriteExtendedApiViewModel()
            {
                Id = f.Id,
                Route = f.Route,
            };

            response.ObjectType = matcher.ObjectType;
            response.Type = FavoriteExtendedType.Asset;

            if (match.ContainsKey("objectId"))
            {
                response.ObjectId = match["objectId"];
            }

            if (match.ContainsKey("uid"))
            {
                response.Uid = Guid.Parse(match["uid"]);
            }

            // TODO: read actual name when we have correct object type & object id & uid
            response.Name = f.Name + " - " + matcher.TabName;

            return response;
        }

        private Dictionary<string, string> TryMatch(Matcher matcher, string route)
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

        private static IEnumerable<Matcher> matchers = new[]
        {
            new Matcher
            {
                RoutePattern = "artifact/:any/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Definition", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Impact Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Lineage",
                Type = FavoriteExtendedType.Asset,
                TabName = "Lineage Diagram", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/relationships/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Relationships", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/ownership/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Responsibilities", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/actions/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Actions", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/comments/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Comments", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/followers/Artifact/:objectId",
                Type = FavoriteExtendedType.Asset,
                TabName = "Followers", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new Matcher
            {
                RoutePattern = "sidebar/audit/:uid",
                Type = FavoriteExtendedType.Asset,
                TabName = "Change Log", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            }
        };

        class Matcher
        {
            public string RoutePattern { get; set; }

            public FavoriteExtendedType Type { get; set; }

            public string TabName { get; set; }

            public SystemObjects ObjectType { get; set; }
        }

        public class Request : IRequest<IEnumerable<FavoriteExtendedApiViewModel>>
        {
            public int ResourceId { get; set; }
        }
    }
}