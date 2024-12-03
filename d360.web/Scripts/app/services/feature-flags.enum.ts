export enum FeatureFlags {
    DistributedCacheFlag = "GovernDistributedCachePerm",
    BrandingThemeCustomCss = "GovernBrandingCustomCssPerm",
    DataProfilingUiFlag = "GovernDataProfileUiPerm",
    SemanticTypesUiFlag = "GovernSemanticTypesUiPerm",
	ContainsSearchDefaultUiFlag = "GovernContainsSearchDefaultUiPerm",
	DashboardingEnabled = "govern-dashboarding-functionality-permanent",
	TagTypesEnabled="govern-tag-types",

	//temp flags
	RelationshipCardinalityTempFlag = "GovernRelationshipCardinalityTemp20230901",
	ReferenceListV2Flag = "GovernReferenceTemp20230901",
	CustomSynonymsFlag = "govern-custom-synonyms-temp",
	TagsLimitedValuesFlag = "govern-tagging-new-administration-ui-temp",
	TagsAdminUIV2Flag = "govern-tagging-value-limitation-temp",
	NewSecurityModel = "govern-security-security-policy-conversion-temp"
}
