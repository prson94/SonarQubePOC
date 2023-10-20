using d360.core.enums;

namespace igx.CertificateManager.Models
{
	internal class CertificateExportModel
	{
		public HashAlgorithmType HashAlgorithmType { get; set; }
		public bool SignInitialSSORequest { get; set; }
		public string IdpSloEndpoint { get; set; }
		public string IdpSsoEndpoint { get; set; }
		public string IcName { get; set; }
		public int IcId { get; set; }
		public byte[] IcFile { get; set; }
		public string UrlPrefix { get; set; }
	}
}
