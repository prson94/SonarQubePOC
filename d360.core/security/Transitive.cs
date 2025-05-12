using Newtonsoft.Json;
using System;

namespace d360.core.security
{
	/// <summary>
	/// The public facing model when viewing asset owners (via roles and security policies).
	/// </summary>
	public class AssetOwnerModel
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("roleName")]
		public string RoleName { get; set; }

		[JsonProperty("groupUid")]
		public Guid GroupUid { get; set; }

		[JsonProperty("groupName")]
		public string GroupName { get; set; }

		[JsonProperty("resourceUid")]
		public Guid ResourceUid { get; set; }

		[JsonProperty("resourceName")]
		public string ResourceName { get; set; }

        [JsonProperty("context")]
        public string Context { get; set; }

		[JsonProperty("ruleName")]
		public string RuleName { get; set; }

		[JsonProperty("isOverride")]
		public bool IsOverride { get; set; }
	}
}
