using d360.core.enums;
using Newtonsoft.Json;
using System;

namespace d360.core.security
{
	/// <summary>
	/// The database class
	/// </summary>
	public class RuleWhen
	{
		public long Id { get; set; }
		public int Position { get; set; }
		public char CheckType { get; set; } // F, R
		public int? FieldTypeId { get; set; }
		public int? IntersectTypeId { get; set; }
		public Operator Operator { get; set; }
		public string? Value { get; set; }
		public long? AssetId { get; set; }
	}

	/// <summary>
	/// The public facing model to create a rule when conditions.
	/// </summary>
	public class SecurityPolicyWhen
	{
		[JsonProperty("checkType")]
		public string CheckType { get; set; } = "F";

		[JsonProperty("fieldName")]
		public string FieldName { get; set; }

		[JsonProperty("intersectTypeUid")]
		public Guid? IntersectTypeUid { get; set; }

		[JsonProperty("operator")]
		public Operator Operator { get; set; }

		[JsonProperty("value")]
		public string Value { get; set; }

		[JsonProperty("assetUid")]
		public Guid? AssetUid { get; set; }
	}

	/// <summary>
	/// The public facing model to create a rule when override.
	/// </summary>
	public class SecurityPolicyWhenOverride
	{
		[JsonProperty("assetUid")]
		public Guid? AssetUid { get; set; }
	}
}
