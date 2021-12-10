using d360.core.enums;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities.Membership
{
    public enum FavoriteType { Asset, AssetType, Page };

    public interface IFavoriteUpsert
    {
        string Route { get; set; }
        FavoriteType Type { get; set; }
    }

    public class FavoriteApiModel : IFavoriteUpsert
    {

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public FavoriteType Type { get; set; }
    }

    public class FavoriteApiViewModel : IFavoriteUpsert
    {
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public FavoriteType Type { get; set; }
    }

    public enum FavoritePageType
    {
        Artifact,
        SearchResultsPage,
        ResourceListPage,
        HomePage,
        DashboardPage,
        CommunityPage,
        WorkflowPage
    }
    public class FavoriteExtendedApiViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Route { get; set; }

        public FavoritePageType PageType { get; set; }

        public AssetTypeClass? AssetTypeClass { get; set; }

        public List<string> Breadcrumbs { get; set; }

        // TODO: remove this
        public SystemObjects? ObjectType { get; set; }

        // TODO: remove this
        public int? ObjectId { get; set; }
    }
}
