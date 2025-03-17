namespace d360.web.Services
{
	public static class FlagList
	{
		// Permanent Feature Flags
		public static readonly string BRANDING_CUSTOM_CSS = "branding-custom-css";
		public static readonly string DASHBOARDING_ENABLED = "govern-dashboarding";
		public static readonly string DATA_PROFILING_UI = "data-profiles-ui";
		public static readonly string SEMANTIC_TYPES_API = "semantic-types-api";
		public static readonly string SEMANTIC_TYPES_UI = "semantic-types-ui";
		public static readonly string TAG_TYPES_ENABLED = "tag-types";

		// Temporary Feature Flags
		public static readonly string CONTAINS_SEARCH_ENABLED = "contains-search-default-ui";
		public static readonly string CUSTOM_SYNONYMS = "custom-synonyms";
		public static readonly string RELATIONSHIP_CARDINALITY = "relationship-cardinality";
		public static readonly string SECURITY_POLICY_CONVERSION_ENABLED = "security-policy-conversion";
		public static readonly string TAGGING_NEW_UI_ENABLED = "tagging-new-administration-ui";
		public static readonly string TAGGING_VALUE_LIMiTATION_ENABLED = "tagging-value-limitation";
		public static readonly string USE_ELASTIC_SEARCH = "search-use-elastic";
        public static readonly string USE_ASSET_FIELD = "asset-field-table";
		public static readonly string USE_INTERSECT_FIELD = "intersect-field-table";
	}
}
