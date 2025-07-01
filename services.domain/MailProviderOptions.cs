using System;

namespace services.domain
{
	public class MailProviderOptions
	{
		public string ApiKey { get; set; } = "";
		public string SubAccount { get; set; } = string.Empty;
		public string ReplyAddress { get; set; } = "no-reply@data3sixty.com";
	}
}
