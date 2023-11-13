using d360.core.enums;
using System;
using System.Collections.Generic;

namespace d360.core.entities
{
	public class FavoritesObjectDetailsResponse
	{
		public int FavoriteId { get; set; }

		public string Name { get; set; }

		public List<BreadcrumbsInfo> Breadcrumbs { get; set; }

		public SystemObjects ObjectType { get; set; }

		public int ObjectId { get; set; }

		public Guid Uid { get; set; }

		public int? TypeObjectId { get; set; }

		public AssetTypeClass AssetTypeClass { get; set; }
	}
}
