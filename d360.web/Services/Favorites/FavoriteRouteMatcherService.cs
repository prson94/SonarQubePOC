using d360.core;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.resources;
using d360.model.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace d360.web.Services.Favorites
{
    public class FavoriteRouteMatcherService
    {
        public FavoriteRouteMatchResult GetRouteMatch(FavoriteShortModel f)
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

        public FavoriteRouteMatchResult TryGetRouteMatch(FavoriteShortModel f, FavoriteRouteMatcher matcher)
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

            return new FavoriteRouteMatchResult
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
                GetName = WithTabName(PageNames.AssetsTab),
                ObjectType = SystemObjects.ArtifactType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard/ArtifactType/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName =WithTabName(PageNames.DashboardsTab),
                ObjectType = SystemObjects.ArtifactType
            },

            // asset
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/:parentId/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.DefinitionTab),
                ObjectType = SystemObjects.Artifact
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/followers/Artifact/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.FollowersTab),
                ObjectType = SystemObjects.Artifact
            },

            // users
            new FavoriteRouteMatcher
            {
                RoutePattern = "resource/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ProfileTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/membergroup/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.GroupsTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemown/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ResponsibilitiesTab),
                ObjectType = SystemObjects.Resource
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/itemfollow/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.FollowingTab),
                ObjectType = SystemObjects.Resource
            },

            // policy type
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/:objectId/structure",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.PolicyTab),
                ObjectType = SystemObjects.PolicyType
            },

            // policy
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/:parentId/id/:objectid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.PolicyTab),
                ObjectType = SystemObjects.Policy
            },
            new FavoriteRouteMatcher
            {
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "policy/:parentId;hierarchyId=:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.PolicyTab),
                ObjectType = SystemObjects.Policy
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/:parentId/id/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.DefinitionTab),
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
                GetName = WithTabName(PageNames.DefinitionTab),
                ObjectType = SystemObjects.Rule
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ruleResults/:any/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.RuleResultsTab),
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
                RoutePattern = "model/:objectId/structure",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ModelTab),
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
                RoutePattern = "model/:parentId;hierarchyId=:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.DefinitionTab),
                ObjectType = SystemObjects.Taxonomy
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "model/:parentId/id/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.DefinitionTab),
                ObjectType = SystemObjects.Taxonomy
            },

            // reference list pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "reference;referenceListId=:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ReferenceTypesTab),
                ObjectType = SystemObjects.ReferenceItemType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/fields/ReferenceItemType/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.FieldDefinitionsTab),
                ObjectType = SystemObjects.ReferenceItemType
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/responsibilities/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ResponsibilitiesTab),
                ObjectType = SystemObjects.ReferenceItemType
            },


            // shared
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/comments/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.CommentsTab),
            },
            new FavoriteRouteMatcher
            { 
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "sidebar/comments/:uid/true",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.CommentsTab)
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/audit/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ChangeLogTab)
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ImpactDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                // TODO: should support several route patterns as part of one matcher
                RoutePattern = "sidebar/visualization/browser/:uid/Impact",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ImpactDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Lineage",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.LineageDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/visualization/browser/:uid/Proces",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ProcessDiagramTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/score/:uid",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ScoringTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/score/:uid/:any",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ScoringTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/ownership/:assetId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ResponsibilitiesTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/relationships/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.RelationshipsTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/actions/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.ActionsTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/workflowmonitor/:type/:objectId",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.WorkflowTab),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "sidebar/workflowmonitor/:type/:objectId;isAdminPage=false",
                PageType = FavoritePageType.Artifact,
                GetName = WithTabName(PageNames.WorkflowTab),
            },

            // resource list pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/assets/TechnicalAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.TechnicalAsset,
                GetName = FixedName(PageNames.TechnicalAssetsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "artifact/assets/BusinessAsset",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.BusinessAsset,
                GetName = FixedName(PageNames.BusinessAssetsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "model/classification",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Model,
                GetName = FixedName(PageNames.ModelsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "policy/classification",
                PageType = FavoritePageType.ResourceListPage,
                ForcedAssetClass = AssetTypeClass.Policy,
                GetName = FixedName(PageNames.PoliciesPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "reference",
                PageType = FavoritePageType.ResourceListPage,
                GetName = FixedName(PageNames.ReferenceTypesPage),
                ForcedAssetClass = AssetTypeClass.Reference
            },

            // special pages
            new FavoriteRouteMatcher
            {
                RoutePattern = "dashboard",
                PageType = FavoritePageType.DashboardPage,
                GetName = FixedName(PageNames.DashboardsPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "community",
                PageType = FavoritePageType.CommunityPage,
                GetName = FixedName(PageNames.CommunityPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "home",
                PageType = FavoritePageType.HomePage,
                GetName = FixedName(PageNames.HomePage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "monitor",
                PageType = FavoritePageType.WorkflowPage,
                GetName = FixedName(PageNames.WorkflowPage),
            },
            new FavoriteRouteMatcher
            {
                RoutePattern = "cart",
                PageType = FavoritePageType.CartPage,
                GetName = FixedName(PageNames.CartPage),
            },

            // search results page
            new FavoriteRouteMatcher
            {
                RoutePattern = "search?query=:query",
                PageType = FavoritePageType.SearchResultsPage,
                GetName = (_, p) => $"“{p["query"]}”",
            }
        };

        private static Func<string, Dictionary<string, string>, string> WithTabName(string tabName)
        {
            return (pageName, p) => pageName + " - " + tabName;
        }

        private static Func<string, Dictionary<string, string>, string> FixedName(string name)
        {
            return (_, p) => name;
        }

    }
}