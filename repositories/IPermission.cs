using d360.core.enums;
using System;
using System.Threading.Tasks;

namespace repositories
{
	public interface IPermission
	{
		/// <summary>
		/// The int-based permissions mask can be sent in as a permission itself, then we bit-wise compare.
		/// </summary>
		bool PermissionInMask(Permission p, int mask);

		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset, based on its Id.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetId(long id);
		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset, based on its legacy object/objectId.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetLegacy(string @object, int id);
		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset, based on its Uid.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetUid(Guid uid);

		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset type, based on its Id.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetTypeId(int id);
		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset type, based on its legacy object/objectId.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetTypeLegacy(string @object, int id);
		/// <summary>
		/// Provides the combined permissions mask that the user has on a specific asset type, based on its Uid.
		/// </summary>
		Task<int> ReadCombinedPermissionByAssetTypeUid(Guid uid);
	}
}
