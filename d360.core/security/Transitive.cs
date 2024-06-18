using Newtonsoft.Json;
using System;

namespace d360.core.security
{
	/// <summary>
	/// The public facing model when viewing asset owners (via roles and security policies).
	/// </summary>
	public class AssetOwnerModel
	{
		[JsonProperty("ruleUid")]
		public Guid RuleUid { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("roleName")]
		public string RoleName { get; set; }

		[JsonProperty("securityType")]
		public RuleSecurityType SecurityType { get; set; }

		[JsonProperty("securityUid")]
		public Guid SecurityUid { get; set; }

		[JsonProperty("securityName")]
		public string SecurityName { get; set; }

		[JsonProperty("isOverride")]
		public bool IsOverride { get; set; }

		[JsonIgnore]
		public bool IsVisible { get; set; }
	}
}
