using System.Collections.Generic;

using d360.core.enums;

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
        Unknown,
        SemanticTypePage
    }

	public class FavoriteApiModel
	{
		public int Id { get; set; }

		public string Type { get; set; }

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
