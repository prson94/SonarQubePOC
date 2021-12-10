using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities.Membership;
using d360.core.enums;
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
            // TODO: cleanup non-existing favorites & homepages by contion
            // TODO: should remove non-unique favorites
            // TODO: breadcrumbs are empty for Asset Types, Policy Types and so on
            var favorites = await favoritesRepository.GetFavorites(request.ResourceId);
            var routeMatchers = favorites.Select(GetRouteMatch).ToList();
            var favoritesDetails = await favoritesRepository.GetFavoriteDetails(routeMatchers.Select(r => r.ObjectId));

            var mappedFavorites = from favorite in favorites
                                  join routeMatch in routeMatchers
                                    on favorite.Id equals routeMatch.ObjectId.FavoriteId
                                  join favoriteDetails in favoritesDetails
                                    on favorite.Id equals favoriteDetails.FavoriteId into joinedFavoriteDetails
                                  from favoriteDetails in joinedFavoriteDetails.DefaultIfEmpty()
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
        }

        private RouteMatchResult GetRouteMatch(FavoriteShortModel f)
        {
            var matchResults = matchers.Select(matcher => TryGetRouteMatch(f, matcher)).Where(r => r != null).ToList();
            if (!matchResults.Any())
            {
                throw new InvalidOperationException($"Failed to match favorite with route {f.Route}");
            }

            if (matchResults.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Route {f.Route} was matched with several matchers: \n" +
                     string.Join("\n", matchResults.Select(m => " •" + m.Matcher.RoutePattern)));
            }

            return matchResults.Single();
        }

        class RouteMatchResult
        {
            public Dictionary<string, string> RouteParams { get; set; }

            public FavoritesObjectDetailsRequest ObjectId { get; set; }

            public FavoriteRouteMatcher Matcher { get; set; }
        }

        private RouteMatchResult TryGetRouteMatch(FavoriteShortModel f, FavoriteRouteMatcher matcher)
        {
            var routeParams = TryMatchRoute(matcher, f.Route);
            if (routeParams == null)
            {
                return null;
            }

            var req = new FavoritesObjectDetailsRequest();
            req.FavoriteId = f.Id;
            req.ObjectType = matcher.ObjectType;

            if (routeParams.ContainsKey("assetId"))
            {
                if (int.TryParse(routeParams["assetId"], out var assetId))
                {
                    req.AssetId = assetId;
                }
                else
                {
                    return null;
                }
            }

            if (routeParams.ContainsKey("objectId"))
            {
                if (int.TryParse(routeParams["objectId"], out var objectId))
                {
                    req.ObjectId = objectId;
                }
                else
                {
                    return null;
                }
            }

            if (routeParams.ContainsKey("uid"))
            {
                if (Guid.TryParse(routeParams["uid"], out var uid))
                {
                    req.Uid = uid;
                }
                else
                {
                    return null;
                }
            }

            if (routeParams.ContainsKey("type"))
            {
                var systemObjectTypes = ((SystemObjects[])Enum.GetValues(typeof(SystemObjects)))
                    .Where(o => string.Equals(o.ToString(), routeParams["type"], StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (systemObjectTypes.Any())
                {
                    req.ObjectType = systemObjectTypes.Single();
                }
                else
                {
                    return null;
                }
            }

            return new RouteMatchResult
            {
                ObjectId = req,
                Matcher = matcher,
                RouteParams = routeParams
            };
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
            return route.Trim('/');
        }

        private static Regex RoutePatternToRegex(string routePattern)
        {
            // please, note, that we have very limited support of query strings here
            // we don't support matching "?a=1&b=2" by pattern "?b=:b&a=:a" (i.e. when order is changed)

            routePattern = "^" + routePattern;
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
            // asset type
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Assets", // TODO: resources,
                ObjectType = SystemObjects.ArtifactType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard/ArtifactType/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Dashboards", // TODO: resources,
                ObjectType = SystemObjects.ArtifactType
            },

            // asset
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/:parentId/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Definition", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/followers/Artifact/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Followers", // TODO: resources,
                ObjectType = SystemObjects.Artifact
            },

            // users
            new FavoriteRouteMatcher
            {
                RoutePattern = "resource/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Profile", // TODO: resources,                
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/membergroup/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Groups", // TODO: resources,                
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemown/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Responsibilities", // TODO: resources,                
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemfollow/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Following", // TODO: resources,                
                ObjectType = SystemObjects.Resource
            },

            // policy type
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/:objectId/structure",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Policy", // TODO: resources,                
                ObjectType = SystemObjects.PolicyType
            },

            // policy
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/:parentId/id/:objectid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Policy", // TODO: resources,                
                ObjectType = SystemObjects.Policy
            },
            new FavoriteRouteMatcher
            {
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "policy/:parentId;hierarchyId=:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Policy", // TODO: resources,                
                ObjectType = SystemObjects.Policy
            },

            // rule type
            new FavoriteRouteMatcher
            {
                RoutePattern = "quality/rule/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name,
                ObjectType = SystemObjects.RuleType
            },

            // rule
            new FavoriteRouteMatcher
            {
                RoutePattern = "quality/rule/:any/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Definition", // TODO: resources,                
                ObjectType = SystemObjects.Rule
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ruleResults/:any/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Rule Results", // TODO: resources,                
                ObjectType = SystemObjects.Rule
            },

            // model type            
            new FavoriteRouteMatcher
            {
                RoutePattern = "model/structure/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name,
                ObjectType = SystemObjects.TaxonomyType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/diagram/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name,
                ObjectType = SystemObjects.TaxonomyType
            },

            // model
            new FavoriteRouteMatcher
            {
                RoutePattern = "model/:any;hierarchyId=:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Definition", // TODO: resources,                
                ObjectType = SystemObjects.Taxonomy
            },

            // reference list pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "reference;referenceListId=:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - Reference Types", // TODO: resources,
                ObjectType = SystemObjects.ReferenceItemType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/fields/ReferenceItemType/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - Field Definitions", // TODO: resources,
                ObjectType = SystemObjects.ReferenceItemType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/responsibilities/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - Responsibilities", // TODO: resources,
                ObjectType = SystemObjects.ReferenceItemType
            },


            // shared
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/comments/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Comments", // TODO: resources,
                ObjectType = null // TODO: objectType should be null here
            },
            new FavoriteRouteMatcher
            { 
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "sidebar/comments/:uid/true",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Comments", // TODO: resources,
                ObjectType = null
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/audit/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Change Log", // TODO: resources,
                ObjectType = null
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Impact Diagram", // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "sidebar/visualization/browser/:uid/Impact",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Impact Diagram", // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Lineage",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Lineage Diagram", // TODO: resources
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Proces",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Process Diagram", // TODO: resources
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/score/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Scoring", // TODO: resources
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ownership/:assetId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Responsibilities", // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/relationships/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Relationships", // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/actions/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Actions", // TODO: resources
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/workflowmonitor/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Workflow", // TODO: resources
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/workflowmonitor/:type/:objectId;isAdminPage=false",
                PageType = FavoritePageType.Artifact,
                GetName = (name, p) => name + " - "  + "Workflow", // TODO: resources
            },

            // resource list pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/assets/TechnicalAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.TechnicalAsset,
                GetName = FixedName("Technical Assets"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/assets/BusinessAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.BusinessAsset,
                GetName = FixedName("Business Assets"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "model/classification",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Model,
                GetName = FixedName("Models"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/classification",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Policy,
                GetName = FixedName("Policies"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "reference",
                PageType = FavoritePageType.ResourceListPage,
                GetName = FixedName("Reference Types"), // TODO: resources,
                ForcedAssetClass = AssetTypeClass.Reference
            },

            // special pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard",
                PageType = FavoritePageType.DashboardPage,
                GetName = FixedName("Dashboards"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "community",
                PageType = FavoritePageType.CommunityPage,
                GetName = FixedName("Community"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "home",
                PageType = FavoritePageType.HomePage,
                GetName = FixedName("Home"), // TODO: resources,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "monitor",
                PageType = FavoritePageType.WorkflowPage,
                GetName = FixedName("Workflow"), // TODO: resources,
            },

            // search results page
            new FavoriteRouteMatcher
            {
                RoutePattern = "search?query=:query",
                PageType = FavoritePageType.SearchResultsPage,
                GetName = (_, p) => $"“{p["query"]}”", // TODO: resources,
            }
        };

        private static Func<string, Dictionary<string, string>, string> FixedName(string name)
        {
            return (_, p) => name;
        }

        class FavoriteRouteMatcher
        {
            public string RoutePattern { get; set; }

            public FavoritePageType PageType { get; set; }

            public string TabName { get; set; }

            public SystemObjects? ObjectType { get; set; }

            public Func<string, Dictionary<string, string>, string> GetName { get; set; }

            public AssetTypeClass? ForcedAssetClass { get; set; }
        }
    }
}