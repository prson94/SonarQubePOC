using d360.core.enums;
using d360.core.security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISecurity
	{
		Platform Platform { get; }

		Task<RepositoryResponse<ReadSecurityPolicy>> CreatePolicyAsync(CreateSecurityPolicy model);

		Task<RepositoryResponse<ReadSecurityPolicyOverride>> CreatePolicyOverrideAsync(CreateSecurityPolicyOverride model);

		Task<RepositoryResponse<ReadRole>> CreateRoleAsync(CreateRole model);

		Task<RepositoryResponse<IEnumerable<PermissionInfo>>> ReadPermissionsByAssetAsync(Guid assetUid);

		Task<RepositoryResponse<IEnumerable<PermissionInfo>>> ReadPermissionsByAssetTypeAsync(Guid assetTypeUid);

		Task<RepositoryResponse<IEnumerable<AssetOwnerModel>>> ReadVisibleOwnersByAssetAsync(Guid assetUid);

		Task<RepositoryResponse<IEnumerable<ReadSecurityPolicy>>> ReadPoliciesAsync();

		Task<RepositoryResponse<dynamic>> ReadPolicyEditOptionsAsync();

		Task<RepositoryResponse<dynamic>> ReadPolicyEditAssetTypeOptionsAsync(Guid assetTypeUid);

		Task<RepositoryResponse<dynamic>> ReadPolicyEditGroupOptionsAsync();

		Task<RepositoryResponse<dynamic>> ReadPolicyEditUserOptionsAsync();

		Task<RepositoryResponse<dynamic>> ReadPolicyEditFieldLookupOptionsAsync(Guid assetTypeUid, string fieldName);

		Task<RepositoryResponse<dynamic>> ReadPolicyEditRelationLookupOptionsAsync(Guid intersectTypeUid, Guid startingAssetTypeUid);

		Task<RepositoryResponse<IEnumerable<ReadRole>>> ReadRolesAsync();

		Task<RepositoryResponse<bool>> RemovePolicyAsync(Guid uid);

		Task<RepositoryResponse<bool>> RemovePolicyOverrideAsync(Guid uid);

		Task<RepositoryResponse<bool>> RemoveRoleAsync(Guid uid);

		Task<RepositoryResponse<ReadSecurityPolicy>> UpdatePolicyAsync(Guid uid, ReadSecurityPolicy model);

		Task<RepositoryResponse<ReadSecurityPolicyOverride>> UpdatePolicyOverrideAsync(Guid uid, CreateSecurityPolicyOverride model);

		Task<RepositoryResponse<ReadRole>> UpdateRoleAsync(Guid uid, CreateRole model);
	}
}
