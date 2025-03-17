using d360.core.entities;
using d360.core.enums;
using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
	{
		public async Task<bool> CreateOpenIdRequestAsync(OpenIdRequest request)
		{
			bool success = false;
			using (var connection = Connect())
			{
				int recordsCount = await connection.InsertAsync(request);
				success = recordsCount > 0;
			}
			return success;
		}

		public string GenerateOpenIdRequestValue(int length = 5)
		{
			var builder = new StringBuilder(length);

			// Unicode/ASCII Letters are divided into two blocks (Letters 65–90 / 97–122):
			// The first group containing the uppercase letters and the second group containing the lowercase.
			char offset = 'a';
			const int lettersOffset = 26; // A...Z or a..z: length=26

			Random _random = new Random();

			for (var i = 0; i < length; i++)
			{
				var @char = (char)_random.Next(offset, offset + lettersOffset);
				builder.Append(@char);
			}

			return builder.ToString().ToLower();
		}

		public async Task<OpenIdRequest> GetOpenIdRequestAsync(string state, bool fromSecondary = true)
		{
			OpenIdRequest model = null;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@state", state);
			using (var connection = Connect(fromSecondary))
			{
				model = await connection.QueryFirstOrDefaultAsync<OpenIdRequest>("select * from OpenIdRequest where State = @state", dbArgs);
			}

			return model;
		}

		public async Task<RepositoryResponse<AuthenticationType>> ReadAuthenticationTypeByTenantUrlAsync(int companyId, string urlPrefix)
		{
			var response = new RepositoryResponse<AuthenticationType>(AuthenticationType.Forms, 200, true, "");

			using (var connection = Connect(true))
			{
				response.Data = await connection.QueryFirstAsync<AuthenticationType>(
						$"select AuthenticationType from CompanyDomainSetting where CompanyID = @companyId and UrlPrefix = @urlPrefix",
						new { companyId, urlPrefix }
					);
			}

			return response;
		}

		public async Task<OidcAuthenticationSettings> ReadIdpOidcSettingsByTenantPrefix(string prefix)
		{
			OidcAuthenticationSettings response;

			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleOrDefaultAsync<OidcAuthenticationSettings>($@"
declare @json nvarchar(max)
select	@json = d.AuthenticationSettings
from	CompanyDomainSetting u
		inner join DomainSetting d on d.ID = u.DomainSettingID 
where	u.UrlPrefix = @prefix

select	oidc.* 
from	openjson(@json) with (
	baseUri nvarchar(500), 
	discoveryUri nvarchar(500), 
	jwtAuthorityUri nvarchar(500),
	clientId nvarchar(500), 
	clientSecret nvarchar(500), 
	audience nvarchar(500), 
	nameClaimType nvarchar(500),
	scopesJson nvarchar(max) '$.scopes' as json,
	extraParametersJson nvarchar(max) '$.extraParameters' as json
) oidc", new { prefix }
				);
			}

			return response;
		}

		public async Task<SamlAuthenticationSettings> ReadIdpSamlSettingsByTenantPrefix(string prefix)
		{
			SamlAuthenticationSettings response;

			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleOrDefaultAsync<SamlAuthenticationSettings>($@"
select	d.HashAlgorithmType,
		d.SignInitialSSORequest,
		d.IdpSsoEndpoint,
		d.IdpSloEndpoint,
		idp.[File] as IdpCertificateFile,
		idp.[Password] as IdpCertificatePassword,
		sp.[File] as SpCertificateFile,
		sp.[Password] as SpCertificatePassword
from	CompanyDomainSetting u
		inner join DomainSetting d on d.ID = u.DomainSettingID 
		left join DomainCertificate idp on idp.ID = d.IdpDomainCertificateID
		left join DomainCertificate sp on sp.ID = d.SpDomainCertificateID
where	u.UrlPrefix = @prefix",
					new { prefix }
				);
			}

			return response;
		}

		public async Task<bool> RemoveOldOpenIdRequestsAsync()
		{
			bool success = false;

			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync("delete OpenIdRequest where CreatedOn < @dt", new { dt = DateTime.UtcNow.AddMinutes(-30) });
				success = recordsCount > 0;
			}

			return success;
		}

		public async Task<bool> RemoveOpenIdRequestAsync(OpenIdRequest request)
		{
			bool success = false;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@state", request.State);
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync("delete OpenIdRequest where State = @state", dbArgs);
				success = recordsCount > 0;
			}

			return success;
		}
	}
}
