using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ICommunity
	{
		Task<RepositoryResponse<bool>> ChangePasswordAsync(int resourceId, string newPassword);

		Task<RepositoryResponse<int>> CreateClaimAsync(ClaimMapping claim);

		Task<bool> CreateOpenIdRequestAsync(OpenIdRequest request);

		Task<RepositoryResponse<int>> CreateUserAsync(Resource user);

		Task<RepositoryResponse<bool>> CreateUserInTenantAsync(int companyId, int resourceId, bool isAdministrator, DateTime loggedInOn, AuthenticationMethod authMethod);

		Task<List<UserApiModel>> GetUsersInTenantAsync(int companyId, List<UserApiModel> users);

		Task<List<UserUpsertValidateModel>> CreateUsersInTenantAsync(int companyId, List<UserUpsertValidateModel> users);

		/// <summary>
		/// Used to generate a state or nonce value.
		/// </summary>
		string GenerateOpenIdRequestValue(int length = 5);

		string GetConnectionStringForTenant(int companyId);

		Task<OpenIdRequest> GetOpenIdRequestAsync(string state);

		Task<RepositoryResponse<ClaimMapping>> ReadClaimMappingById(int id);

		Task<OidcAuthenticationSettings> ReadIdpOidcSettingsByTenantPrefix(string prefix);

		Task<SamlAuthenticationSettings> ReadIdpSamlSettingsByTenantPrefix(string prefix);

		Task<RepositoryResponse<AuthenticationType>> ReadAuthenticationTypeByTenantUrlAsync(int companyId, string urlPrefix);

		Task<RepositoryResponse<IEnumerable<ClaimMapping>>> ReadClaimsByTenantAsync(int clientId, int companyId, int domainSettingId);

		Task<RepositoryResponse<IEnumerable<CompanyDomainSetting>>> ReadDomainSettingsByTenantAsync(int companyId);

		Task<IEnumerable<CompanyDigestExecution>> ReadMostRecentWorkflowDigestStatusBySlotAsync(EnvironmentLevel slot, string region = null);
		
		Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync(int companyId);

		Task<SettingInfo> ReadSettingAsync(int companyId, Setting setting);

		Task<List<SettingInfo>> ReadSettingsAsync(int companyId);

		Task<T> ReadSettingValueAsync<T>(int companyId, Setting setting);

		Task<bool> ReadShouldUserBeAutoAdminByGroupMembershipAsync(int companyId, int domainSettingId, List<string> groups);

		Task<IEnumerable<CompanyWithDatabaseServerSettings>> ReadTenantConnectionSettingsByCurrentSlotAsync(EnvironmentLevel slot, string region = null);

		Task<CompanyWithDatabaseServerSettings> ReadTenantConnectionSettingsByIdAsync(int companyId);

		Task<CompanyResource> ReadTenantUserAsync(int companyId, int resourceId);

		Task<RepositoryResponse<Resource>> ReadUserByEmailAsync(string email);

		Task<RepositoryResponse<Resource>> ReadUserByIdAsync(int userId);

		Task<RepositoryResponse<Resource>> ReadUserByUidAsync(Guid userId);

		Task<RepositoryResponse<Resource>> ReadUserByUsernameAsync(string username);

		Task<RepositoryResponse<IEnumerable<Resource>>> ReadUsersByTenantAsync(int companyId, List<int> userIds = null);

		Task<ClientUserModel> ReadUserFeatureFlagContext(int companyId, int userId);

		Task<RepositoryResponse<bool>> RemoveClaimAsync(int claimId, int clientId, int companyId, int domainSettingId);

		Task<bool> RemoveOldOpenIdRequestsAsync();

		Task<bool> RemoveOpenIdRequestAsync(OpenIdRequest request);

		Task<RepositoryResponse<int>> RemoveUsersFromTenantAsync(int companyId, List<Guid> resourceUids);

		Task<RepositoryResponse<bool>> ResetUserPassword(int resourceId, string currentPassword, string newPassword);

		Task<bool> UpdateClaimAsync(int claimId, ClaimAction action, string path, bool isArray);

		Task<RepositoryResponse<Resource>> UpdateUserApiCredentialsAsync(int userId);

		Task<RepositoryResponse<int>> UpdateUserAsync(Resource user);

		Task<RepositoryResponse<bool>> UpdateUserInTenantAsync(int companyId, int resourceId, bool isAdministrator, DateTime loggedInOn, AuthenticationMethod authMethod);

		Task UpsertWorkflowDigestStatusAsync(int companyId, Guid invocationId, int? existingId);

		Task<Resource> ValidateResourceAsync(string username, string password, int? companyId);



		Task<RepositoryResponse<bool>> RemoveSettingAsync(int companyId, Setting setting);

		Task<RepositoryResponse<bool>> UpsertSettingAsync(int companyId, Setting setting, string value);

	}
}
