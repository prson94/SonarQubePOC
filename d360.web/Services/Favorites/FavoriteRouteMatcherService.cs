using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.resources;
using d360.model.DataAccessLayer;

using SmartFormat;

namespace d360.web.Services.Favorites
{
    public class FavoriteRouteMatcherService
    {
        public FavoriteRouteMatchResult MatchRoute(FavoriteShortModel f)
        {
            var matchResults = matchers.Select(matcher => TryMatchRoute(f, matcher)).Where(r => r != null).ToList();
            
            if (!matchResults.Any())
            {
                return TryMatchRoute(f, UnknownRouteMatcher);
            }

            if (matchResults.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Route {f.Route} was matched with several matchers: \n" +
                     string.Join("\n", matchResults.Select(m => " •" + m.Matcher.RoutePattern)));
            }

            return matchResults.Single();
        }

        public FavoriteRouteMatchResult TryMatchRoute(FavoriteShortModel f, FavoriteRouteMatcher matcher)
        {
            var routeParams = TryMatchRoute(matcher, f.Route);

            if (routeParams == null)
            {
                return null;
            }

            var req = new FavoritesObjectDetailsRequest
            {
                FavoriteId = f.Id,
                ObjectType = matcher.ObjectType
            };

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

            return new FavoriteRouteMatchResult
            {
                ObjectId = req,
                Matcher = matcher,
                RouteParams = routeParams
            };
        }

        public string GetNormalizedRoute(string route, FavoriteRouteMatcher matcher, FavoritesObjectDetailsResponse favoriteDetails)
        {
            if (TryMatchRoute(matcher.RoutePattern, route) != null)
            {
                return route;
            }

            return Smart.Format(ToFormattableString(matcher.RoutePattern), favoriteDetails);
        }

        public IEnumerable<string> GetAllPossibleRoutes(string route, FavoriteRouteMatcher matcher, FavoritesObjectDetailsResponse favoriteDetails)
        {
            if (!matcher.OtherRoutePatterns.Any())
            {
                return new[] { route };
            }

            var routes = matcher.RoutePatterns.Select(pattern => Smart.Format(ToFormattableString(pattern), favoriteDetails));

            return routes;
        }

        private static string ToFormattableString(string routePattern)
        {
            var parameterNames = new Regex(@":((?:\w)+)")
                .Matches(routePattern)
                .OfType<Match>()
                .Select(x => x.Groups[1].Value)
                .ToList();

            foreach (var parameterName in parameterNames)
            {
                routePattern = routePattern.Replace(
                    $":{parameterName}",
                    $"{{{CapitalizeFirstLetter(parameterName)}}}");
            }

            return routePattern;

            string CapitalizeFirstLetter(string str)
            {
                if (str == null && str.Length == 0)
                {
                    return str;
                }

                return str.Substring(0, 1).ToUpper() + str.Substring(1);
            }
        }

        private Dictionary<string, string> TryMatchRoute(FavoriteRouteMatcher matcher, string route)
        {
            return matcher.RoutePatterns
                .Select(pattern => TryMatchRoute(pattern, route))
                .Where(x => x != null)
                .SingleOrDefault();
        }

        private Dictionary<string, string> TryMatchRoute(string routePattern, string route)
        {
            route = SanitizeRoute(route);
            var routePatternRegex = RoutePatternToRegex(routePattern);
            var match = routePatternRegex.Match(route);

            if (!match.Success)
            {
                return null;
            }

            var routeParams = match.Groups
                .OfType<System.Text.RegularExpressions.Group>()
                .ToDictionary(g => g.Name, g => g.Value);

            routeParams.Add("route", route);

            return routeParams;
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
                    $@"(?<{parameterName}>[\w\d\-{(parameterName == "query" ? "%" : "")}]+?)");
            }

            return new Regex(routePattern, RegexOptions.IgnoreCase);
        }

        private static readonly FavoriteRouteMatcher UnknownRouteMatcher = new FavoriteRouteMatcher
        {
            RoutePattern = ".*",
            PageType = FavoritePageType.Unknown,
            GetName = (name, routeParams) => "/" + routeParams["route"]
        };

        private static readonly IEnumerable<FavoriteRouteMatcher> matchers = new[]
        {
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.DashboardsTab),
                ObjectType = SystemObjects.ArtifactType
            },
            // users
            new FavoriteRouteMatcher
            {
                RoutePattern = "users/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ProfileTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "users/:uid/groups",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.GroupsTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemown/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ResponsibilitiesTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemfollow/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.FollowingTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/results",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.RuleResultsTab),
                ObjectType = SystemObjects.Rule
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/:uid/diagrams",
                PageType = FavoritePageType.Artifact,
                GetName = (detail, p) => detail?.Name,
                ObjectType = SystemObjects.TaxonomyType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/:uid/fields",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.FieldDefinitionsTab),
                ObjectType = SystemObjects.ReferenceItemType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/:uid/owners",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ResponsibilitiesTab),
                ObjectType = SystemObjects.ReferenceItemType
            },

            // shared
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/comments",
                OtherRoutePatterns = { "sidebar/comments/:uid/true" },
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.CommentsTab),
            },
			new FavoriteRouteMatcher
			{
				RoutePattern = "asset/:uid/followers",
				PageType = FavoritePageType.Artifact,
				GetName = WithTabName(() => Label.FollowersTab),
				ObjectType = SystemObjects.Artifact
			},
			new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/log",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ChangeLogTab)
            },
			new FavoriteRouteMatcher
			{
				RoutePattern = "semantics/:uid/log",
				PageType = FavoritePageType.Artifact,
				GetName = WithTabName(() => Label.ChangeLogTab)
			},
			new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/diagrams",
                OtherRoutePatterns = {
					"asset/:uid/diagrams/Impact"
				},
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ImpactDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/diagrams/Lineage",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.LineageDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/diagrams/Proces",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ProcessDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/score",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ScoringTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/score/:any",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ScoringTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/owners",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ResponsibilitiesTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/relationships",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.RelationshipsTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/children",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ChildrenTab)
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/actions",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.ActionsTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "asset/:uid/workflowmonitor",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.WorkflowTab),
            },
			new FavoriteRouteMatcher
			{
				RoutePattern = "asset/:uid",
				PageType = FavoritePageType.Artifact,
				GetName = WithTabName(() => Label.DefinitionTab),
				ObjectType = SystemObjects.Artifact
			},
			new FavoriteRouteMatcher
            {
                RoutePattern = "assets/:uid/workflowmonitor;isAdminPage=false",
				OtherRoutePatterns = {
					"assets/:uid/workflowmonitor"
				},
				PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.WorkflowTab),
            },
			new FavoriteRouteMatcher
			{
				RoutePattern = "assets/:uid",
				PageType = FavoritePageType.Artifact,
				GetName = FromClassName(),
				ObjectType = SystemObjects.ReferenceItemType
			},
            // resource list pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/class/TechnicalAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.TechnicalAsset,
                GetName = FixedName(() => Label.TechnicalAssetsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/class/BusinessAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.BusinessAsset,
                GetName = FixedName(() => Label.BusinessAssetsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/class/Model",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Model,
                GetName = FixedName(() => Label.ModelsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/class/Policy",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Policy,
                GetName = FixedName(() => Label.PoliciesPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "assets/class/Reference",
                PageType = FavoritePageType.ResourceListPage,
                GetName = FixedName(() => Label.ReferenceTypesPage),
                ForcedAssetClass = AssetTypeClass.Reference
            },

            // special pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard",
                PageType = FavoritePageType.DashboardPage,
                GetName = FixedName(() => Label.DashboardsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "community",
                PageType = FavoritePageType.CommunityPage,
                GetName = FixedName(() => Label.CommunityPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "home",
                PageType = FavoritePageType.HomePage,
                GetName = FixedName(() => Label.HomePage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "monitor",
                PageType = FavoritePageType.WorkflowPage,
                GetName = FixedName(() => Label.WorkflowPage),
            },

            // search results page
            new FavoriteRouteMatcher
            {
                RoutePattern = "search?query=:query",
                PageType = FavoritePageType.SearchResultsPage,
                GetName = (_, p) => $"“{HttpUtility.UrlDecode(p["query"])}”",
            },

            //Semantic Types
            new FavoriteRouteMatcher
            {
                RoutePattern = "semantics",
                PageType = FavoritePageType.SemanticTypePage,
                GetName = FixedName(() => Label.SemanticTypePage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "semantics/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.DefinitionTab),
                ObjectType = SystemObjects.SemanticType,
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "semantics/:uid/assets",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(() => Label.AssetsTab),
                ObjectType = SystemObjects.SemanticType,
            }
        };

        private static Func<FavoritesObjectDetailsResponse, Dictionary<string, string>, string> WithTabName(Func<string> tabName)
        {
            return (pageName, p) => pageName?.Name + " - " + tabName();
        }
		private static Func<FavoritesObjectDetailsResponse, Dictionary<string, string>, string> FromClassName()
		{
			return (detail, _) =>
			{
				string tabName = "";
				switch (detail.AssetTypeClass)
				{
					case AssetTypeClass.BusinessAsset:
					case AssetTypeClass.TechnicalAsset:
						tabName = Label.AssetsTab;
						break;
					case AssetTypeClass.Model:
						tabName = Label.ModelsPage;
						break;
					case AssetTypeClass.Policy:
						tabName = Label.PoliciesPage;
						break;
					case AssetTypeClass.Rule:
						tabName = Label.RulesPage;
						break;
					case AssetTypeClass.ReferenceItemType:
					case AssetTypeClass.Reference:
						tabName = Label.ReferenceTypesTab;
						break;
					default:
						tabName = detail.AssetTypeClass.ToString();
						break;
				}
				return detail?.Name + " - " + tabName;
			};
		}

		private static Func<FavoritesObjectDetailsResponse, Dictionary<string, string>, string> FixedName(Func<string> name)
        {
            return (_, p) => name();
        }
    }
}
