using d360.core.enums;
using System;

namespace d360.core.entities.Transitive
{
	public class FavoriteItem
	{
		public int FavoriteId { get; set; }

		public SystemObjects ObjectType { get; set; }

		public int ObjectId { get; set; }

		public Guid Uid { get; set; }

		public int? TypeObjectId { get; set; }

		public string Name { get; set; }

		public AssetTypeClass AssetTypeClass { get; set; }
	}
}
