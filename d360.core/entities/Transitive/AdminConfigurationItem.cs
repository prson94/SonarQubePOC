using d360.core.enums;
using System;

namespace d360.core.entities
{
	public class AdminConfigurationItem
	{
		public AssetTypeClass Class { get; set; }

		public string Name { get; set; }

		public Guid Uid { get; set; }

		public Guid? ParentUid { get; set; }
	}
}
