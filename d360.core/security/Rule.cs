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
		public RuleSecurityType SecurityType { get; set; } // 1 = G, 2 = U
		public int AssetTypeId { get; set; }
		public bool ApplyToType { get; set; }
		public bool IsVisible { get; set; }
		public bool IsOverride { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
		public int UpdatedBy { get; set; }
		public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
	}

	// Maps to the security.Override table.
	public class SecurityPolicyOverride
	{
		public Guid Id { get; set; }
		public int RoleId { get; set; }
		public RuleSecurityType SecurityType { get; set; }
		public int SecurityId { get; set; }
		public long AssetId { get; set; }
		public string? Context { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedOn { get; set; }
		public int UpdatedBy { get; set; }
		public DateTime UpdatedOn { get; set; }
	}

	public interface ISecurityPolicy
	{
		string Name { get; set; }
		Guid AssetTypeUid { get; set; }
		Guid RoleUid { get; set; }
		RuleSecurityType SecurityType { get; set; }
		bool ApplyToType { get; set; }
		bool IsVisible { get; set; }
		List<SecurityPolicyWhen> When { get; set; }
		List<SecurityPolicyThen> Then { get; set; }
	}

	/// <summary>
	/// The public facing model to create a rule.
	/// </summary>
	public class CreateSecurityPolicy: ISecurityPolicy
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

		[JsonProperty("whenConditions")]
		public List<SecurityPolicyWhen> When { get; set; }

		[JsonProperty("thenConditions")]
		public List<SecurityPolicyThen> Then { get; set; }
	}

	/// <summary>
	/// The public facing model to read a rule.
	/// </summary>
	public class CreateSecurityPolicyOverride
	{
		[JsonProperty("assetUid")]
		public Guid AssetUid { get; set; }

		[JsonProperty("roleUid")]
		public Guid RoleUid { get; set; }

		[JsonProperty("securityType")]
		public RuleSecurityType SecurityType { get; set; }

		[JsonProperty("securityUid")]
		public Guid SecurityUid { get; set; }

		[JsonProperty("context")]
		public string Context { get; set; }
	}

	/// <summary>
	/// The public facing model to read a rule.
	/// </summary>
	public class ReadSecurityPolicy: CreateSecurityPolicy, ISecurityPolicy
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }

		[JsonProperty("assetTypeName")]
		public string AssetTypeName { get; set; }

		[JsonProperty("roleName")]
		public string RoleName { get; set; }
	}

	/// <summary>
	/// The public facing model to read a rule override.
	/// </summary>
	public class ReadSecurityPolicyOverride : CreateSecurityPolicyOverride
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }
	}

	/// <summary>
	/// The public facing model to update a policy override.
	/// </summary>
	public class UpdateSecurityPolicyOverride
	{
		[JsonProperty("context")]
		public string Context { get; set; }
	}
}
