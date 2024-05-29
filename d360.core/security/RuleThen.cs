using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace d360.core.security
{
	/// <summary>
	/// The database class
	/// </summary>
	public class RuleThen
	{
		public long Id { get; set; }
		public int Position { get; set; }
		public int? FieldTypeId { get; set; }
		public string Operator { get; set; }
		public string? Value { get; set; }
		public int? SecurityId { get; set; }
	}

	/// <summary>
	/// The public facing model to create a rule then conditions.
	/// </summary>
	public class CreateRuleThen
	{
		[JsonProperty("fieldName")]
		public string? FieldName { get; set; }
		
		[JsonProperty("operator")]
		public string Operator { get; set; }

		[JsonProperty("value")]
		public string? Value { get; set; }

		[JsonProperty("securityUid")]
		public Guid? SecurityUid { get; set; }
	}

	/// <summary>
	/// The public facing model to create a rule then override.
	/// </summary>
	public class CreateRuleThenOverride
	{
		[JsonProperty("securityUid")]
		public Guid? SecurityUid { get; set; }
	}
}
