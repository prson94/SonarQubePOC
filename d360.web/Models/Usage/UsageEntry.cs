using Newtonsoft.Json;
using System;

namespace d360.web.Models.Usage
{
	public class UsageEntry
	{
		[JsonProperty("assetUid")]
		public Guid? AssetUid { get; set; }

		[JsonProperty("assetTypeUid")]
		public Guid? AssetTypeUid { get; set; }

		[JsonProperty("dashboardUid")]
		public Guid? DashboardUid { get; set; }

		[JsonProperty("issueUid")]
		public Guid? IssueUid { get; set; }

		[JsonProperty("semanticUid")]
		public Guid? SemanticUid { get; set; }

		[JsonProperty("tagUid")]
		public Guid? TagUid { get; set; }

		[JsonProperty("tab")]
		public string Tab { get; set; }

		[JsonProperty("sidebar")]
		public string Sidebar { get; set; }

		[JsonProperty("action")]
		public UsageAction Action { get; set; }

		[JsonProperty("browser")]
		public UsageBrowser Browser { get; set; }

		[JsonProperty("language")]
		public string Language { get; set; }

		[JsonProperty("locale")]
		public string Locale { get; set; }
	}
}