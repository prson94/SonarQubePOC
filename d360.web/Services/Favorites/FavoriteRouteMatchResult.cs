using System.Collections.Generic;

using d360.model.DataAccessLayer;

namespace d360.web.Services.Favorites
{
    public class FavoriteRouteMatchResult
    {
        public Dictionary<string, string> RouteParams { get; set; }

        public FavoritesObjectDetailsRequest ObjectId { get; set; }

        public FavoriteRouteMatcher Matcher { get; set; }
    }
}
