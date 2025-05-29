using d360.core.entities;
using d360.core.enums;
using Dapper;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
	{
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
	}
}
