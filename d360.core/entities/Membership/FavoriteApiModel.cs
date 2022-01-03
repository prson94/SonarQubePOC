using d360.core.enums;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities.Membership
{
    public enum FavoritePageType
    {
        Artifact,
        SearchResultsPage,
        ResourceListPage,
        HomePage,
        DashboardPage,
        CommunityPage,
        WorkflowPage,
        CartPage,
        Unknown
    }

    public interface IFavoriteUpsert
    {
        string Route { get; set; }
    }

    public class FavoriteApiModel : IFavoriteUpsert
    {

        [DataMember]
        public string Route { get; set; }
    }

    public class FavoriteApiViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Route { get; set; }

        public FavoritePageType PageType { get; set; }

        public AssetTypeClass? AssetTypeClass { get; set; }

        public List<string> Breadcrumbs { get; set; }
    }
}
