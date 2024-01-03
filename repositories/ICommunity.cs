using d360.core.entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ICommunity
	{
		Task<bool> ChangePasswordAsync(int resourceId, string newPassword);

		Task<Group> CreateGroupInTenantAsync(int companyId, Group group);

		Task<bool> CreateOpenIdRequestAsync(OpenIdRequest request);

		/// <summary>
		/// Used to generate a state or nonce value.
		/// </summary>
		string GenerateOpenIdRequestValue(int length = 5);

		Task<string> GetConnectionStringForTenantAsync(int companyId);

		Task<IEnumerable<Group>> GetGroupsByTenantAsync(int companyId);

		Task<OpenIdRequest> GetOpenIdRequestAsync(string state);

		Task<bool> RemoveOpenIdRequestAsync(OpenIdRequest request);

		Task<Resource> ValidateResourceAsync(string username, string password, int? companyId);
	}
}
