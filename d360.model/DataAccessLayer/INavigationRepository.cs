using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public interface INavigationRepository
	{
		Task<IReadOnlyList<AdminConfigurationItem>> GetAdminConfigurationItems();
	}

	public class AdminConfigurationItem
	{
		public AssetTypeClass Class { get; set; }

		public string Name { get; set; }

		public Guid Uid { get; set; }

		public Guid? ParentUid { get; set; }
	}
}
