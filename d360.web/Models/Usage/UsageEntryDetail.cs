using Newtonsoft.Json;
using System;

namespace d360.web.Models.Usage
{
	public class UsageEntryDetail: UsageEntry
	{
		[JsonProperty("resourceUid")]
		public Guid ResourceUid { get; set; }

		[JsonProperty("firstName")]
		public string FirstName { get; set; }

		[JsonProperty("lastName")]
		public string LastName { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("timestamp")]
		public DateTime Timestamp { get; set; }


		/// <summary>
		/// Deprecated property. Defer to Timestamp property instead.
		/// </summary>
		[JsonProperty("eventDate"), Obsolete]
		public DateTime EventDate { get { return Timestamp; } }

		/// <summary>
		/// Deprecated property. Defer to Browser property instead.
		/// </summary>
		[JsonProperty("userAgent"), Obsolete]
		public string UserAgent { get; set; } = "";

		/// <summary>
		/// Deprecated property.
		/// </summary>
		[JsonProperty("host"), Obsolete]
		public string Host { get; set; } = "";

		/// <summary>
		/// Deprecated property.
		/// </summary>
		[JsonProperty("assetTypeName"), Obsolete]
		public string AssetTypeName { get; set; } = "";

		/// <summary>
		/// Deprecated property.
		/// </summary>
		[JsonProperty("assetDisplayValue"), Obsolete]
		public string AssetDisplayValue { get; set; } = "";

		/// <summary>
		/// Deprecated property.
		/// </summary>
		[JsonProperty("assetClass"), Obsolete]
		public string AssetClass { get; set; } = "";
	}
}