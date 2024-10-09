using d360.core.entities;
using d360.core.enums;

namespace d360.extensions.info
{
    public class UriSecurityContextProvider : ISecurityContextProvider
    {
        public UriSecurityContextProvider()
        {

        }

		public AuthenticationType AuthenticationType { get; set; }

		public bool AllowNewUserLogin { get; set; }

		public int ClientID { get; set; }

        public int CompanyID { get; set; }

        public int DomainSettingID { get; set; }

        public int ResourceID { get; set; }

		public bool IsAdministrator { get; set; }

        public string CompanyPrefix { get; set; }

		public string PrimaryCompanyPrefix { get; set; }

		public OidcAuthenticationSettings Oidc { get; set; }

		public SamlAuthenticationSettings Saml { get; set; }
	}
}
