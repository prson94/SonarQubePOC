using d360.core.enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace d360.core.entities
{
	public class ClaimMapping
	{
		public int Id { get; set; }
		public int CompanyId { get; set; }
		public int ClientId { get; set; }
		public int DomainSettingId { get; set; }
		public AuthenticationType AuthenticationType { get; set; }
		public ClaimType ClaimType { get; set; }
		public string Path { get; set; }
		public bool IsArray { get; set; }
		public ClaimAction Action { get; set; }

		[NotMapped]
		public ClaimLocation Location
		{
			get
			{
				if (ClientId != 0)
				{
					if (CompanyId != 0)
					{
						if (DomainSettingId != 0)
						{
							return ClaimLocation.Idp;
						}
						else
						{
							return ClaimLocation.Environment;
						}
					}
					else
					{
						return ClaimLocation.Client;
					}
				}
				else
				{
					return ClaimLocation.Default;
				}
			}
		}

		[NotMapped]
		public string PathHash
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Path))
				{
					return null;
				}

				using (SHA1 sha1 = SHA1Managed.Create())
				{
					var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(Path));
					var hashString = string.Concat(Convert.ToBase64String(hash).ToCharArray().Where(x => char.IsLetterOrDigit(x)).Take(6));
					return hashString;
				}
			}
		}
	}
}