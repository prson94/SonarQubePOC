//These flags are stored within the Community database. Not LaunchDarkly.
export enum FeatureFlags {
	BrandingThemeCustomCss = "branding-custom-css",
	DataProfilingUiFlag = "data-profiles-ui",
	SemanticTypesUiFlag = "semantic-types-ui",
	DashboardingEnabled = "govern-dashboarding",
	TagTypesEnabled="tag-types",
	ContainsSearchDefaultUi = "contains-search-default-ui",

	// temporary flags.
	CustomSynonymsFlag = "custom-synonyms",
	TagsLimitedValuesFlag = "tagging-new-administration-ui",
	TagsAdminUIV2Flag = "tagging-value-limitation",
	RelationshipCardinality = "relationship-cardinality",
	NewSecurityModel = "security-policy-conversion",
	UseElasticSearch = "search-use-elastic",
	NewReferenceListField = "fieldtype-referencelist-conversion"
}
