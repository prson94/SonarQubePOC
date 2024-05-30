using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace d360.core.security
{
	/// <summary>
	/// The database class
	/// </summary>
	public class Rule
	{
		public long Id { get; set; }
		public Guid Uid { get; set; }
		public string Name { get; set; }
		public int RoleId { get; set; }
		public char SecurityType { get; set; } // G, U
		public int AssetTypeId { get; set; }
		public bool ApplyToType { get; set; }
		public bool IsVisible { get; set; }
		public bool IsOverride { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
		public int UpdatedBy { get; set; }
		public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
	}

	/// <summary>
	/// The public facing model to create a rule.
	/// </summary>
	public class CreateRule
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("assetTypeUid")]
		public Guid AssetTypeUid { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("securityType")]
		public RuleSecurityType SecurityType { get; set; }

		[JsonProperty("applyToType")]
		public bool ApplyToType { get; set; }

		[JsonProperty("visible")]
		public bool IsVisible { get; set; }

		[JsonProperty("when")]
		public List<CreateRuleWhen> When { get; set; }

		[JsonProperty("then")]
		public List<CreateRuleThen> Then { get; set; }
	}


	/// <summary>
	/// The public facing model to read a rule.
	/// </summary>
	public class CreateRuleOverride
	{
		[JsonProperty("assetUid")]
		public Guid AssetUid { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("securityType")]
		public RuleSecurityType SecurityType { get; set; }

		[JsonProperty("securityUid")]
		public Guid SecurityUid { get; set; }
	}

	/// <summary>
	/// The public facing model to read a rule.
	/// </summary>
	public class ReadRule
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("assetTypeUid")]
		public Guid AssetTypeUid { get; set; }

		[JsonProperty("assetTypeName")]
		public string AssetTypeName { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("roleName")]
		public string RoleName { get; set; }

		[JsonProperty("securityType")]
		public RuleSecurityType SecurityType { get; set; }

		[JsonProperty("applyToType")]
		public bool ApplyToType { get; set; }

		[JsonProperty("visible")]
		public bool IsVisible { get; set; }
	}

	/// <summary>
	/// The public facing model to read a rule override.
	/// </summary>
	public class ReadRuleOverride : CreateRuleOverride
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }
	}
}
