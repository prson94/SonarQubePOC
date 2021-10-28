using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Authentication;

namespace d360.core.entities
{
    public interface ICompanyAuthenticationSettings
    {
        string type { get; set; }
    }

    public interface IAuthenticationExtraParameters
    {

    }

    public class NoneAuthenticationExtraParameters : IAuthenticationExtraParameters
    {
    }

    public class OktaAuthenticationExtraParameters : IAuthenticationExtraParameters
    {
        public string idp { get; set; }
    }

    public class CompanyOpenIdAuthenticationSettings: ICompanyAuthenticationSettings
    {
        public string type { get; set; }
        public string baseUri { get; set; }
        public string discoveryUri { get; set; }
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string audience { get; set; }
        public string nameClaimType { get; set; }
        public JObject extraParameters { get; set; }

        public IAuthenticationExtraParameters GetStructuredExtraParameters()
        {
            IAuthenticationExtraParameters p;

            switch (type)
            {
                case "Okta":
                    p = extraParameters.ToObject<OktaAuthenticationExtraParameters>();
                    break;
                default:
                    p = new NoneAuthenticationExtraParameters();
                    break;
            }

            return p;
        }
    }

    public class CompanySsoModel
    {
        public bool AllowNewUserLogin { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public byte[] IdpCertificateFile { get; set; }
        public string IdpCertificatePassword { get; set; }
        public string IdpSloEndpoint { get; set; }
        public string IdpSsoEndpoint { get; set; }
        public byte[] SpCertificateFile { get; set; }
        public string SpCertificatePassword { get; set; }
        public d360.core.enums.HashAlgorithmType HashAlgorithmType { get; set; }

        public bool SignInitialSSORequest { get; set; }
        public bool IsCompanyActive { get; set; }

        public string AuthenticationSettings { get; set; }

        public CompanyOpenIdAuthenticationSettings StructuredAuthenticationSettings
        {
            get
            {
                return JsonConvert.DeserializeObject<CompanyOpenIdAuthenticationSettings>(
                    AuthenticationSettings ?? "{}"
                    );
            }
        }
    }
}
