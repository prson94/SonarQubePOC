using d360.core;
using d360.core.entities.Membership;
using d360.core.enums;
using System;
using System.Collections.Generic;

namespace d360.web.Services.Favorites
{
    public class FavoriteRouteMatcher
    {
        public string RoutePattern { get; set; }

        public FavoritePageType PageType { get; set; }

        public string TabName { get; set; }

        public SystemObjects? ObjectType { get; set; }

        public Func<string, Dictionary<string, string>, string> GetName { get; set; }

        public AssetTypeClass? ForcedAssetClass { get; set; }
    }
}