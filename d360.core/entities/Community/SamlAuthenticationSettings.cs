using d360.core.enums;

namespace d360.core.entities
{
	public class SamlAuthenticationSettings
	{
		public byte[] IdpCertificateFile { get; set; }

		public string IdpCertificatePassword { get; set; }

		public string IdpSloEndpoint { get; set; }

		public string IdpSsoEndpoint { get; set; }

		public byte[] SpCertificateFile { get; set; }

		public string SpCertificatePassword { get; set; }

		public HashAlgorithmType HashAlgorithmType { get; set; }

		public bool SignInitialSSORequest { get; set; }
	}
}
