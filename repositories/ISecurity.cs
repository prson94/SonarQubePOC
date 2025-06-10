using d360.core.entities;
using d360.core.enums;
using d360.core.security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISecurity: IPermission
	{
		Platform Platform { get; }

		Task<RepositoryResponse<ReadSecurityPolicy>> CreatePolicyAsync(CreateSecurityPolicy model);
		Task<RepositoryResponse<ReadSecurityPolicyOverride>> CreateOverrideAsync(CreateSecurityPolicyOverride model);
		Task<RepositoryResponse<ReadRole>> CreateRoleAsync(CreateRole model);
		Task<RepositoryResponse<IEnumerable<ResponsibilityGetBreakdownByResourceModel>>> ReadAssetCountsByResourceAndRoleAsync(Guid resourceUid, Guid? roleUid);
		Task<RepositoryResponse<IEnumerable<ResponsibilityBreakdownResponse>>> ReadAssetCountsByRoleAsync(Guid? roleUid);
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
		Task<RepositoryResponse<IEnumerable<dynamic>>> ReadGroupsAndUsersAsSecurityAsync(Guid assetUid, bool includeInternalUsers = false);
		Task<RepositoryResponse<Role>> ReadRawRoleAsync(Guid uid);
		Task<RepositoryResponse<bool>> RemovePolicyAsync(Guid uid, bool softDelete = true);
		Task<RepositoryResponse<bool>> RemoveOverrideAsync(Guid uid);
		Task<RepositoryResponse<bool>> RemoveOverridesByGroupAsync(int groupId);
		Task<RepositoryResponse<bool>> RemoveOverridesByUserAsync(int userId);
		Task<RepositoryResponse<bool>> RemoveOverridesByAssetRoleAndUsersAsync(long assetId, int roleId, List<Guid> users);
		Task<RepositoryResponse<RoleDeleteResult>> RemoveRoleAsync(Guid uid);
		Task RunPolicyAsync(Guid? assetUid = null, Guid? executionUid = null, Guid? policyUid = null);
		Task<RepositoryResponse<ReadSecurityPolicy>> UpdatePolicyAsync(Guid uid, ReadSecurityPolicy model);
		Task<RepositoryResponse<bool>> UpdateOverrideAsync(Guid uid, UpdateSecurityPolicyOverride model);
		Task<RepositoryResponse<ReadRole>> UpdateRoleAsync(Guid uid, CreateRole model);
		Task<RepositoryResponse<bool>> UpsertOverridesByAssetRoleAndUsersAsync(long assetId, int roleId, List<Guid> users);
		Task<bool> DoesPolicyExists(string policyName);

		Task<IEnumerable<Guid>> FindRolesByUidAsync(IEnumerable<Guid> roles);
	}
}
