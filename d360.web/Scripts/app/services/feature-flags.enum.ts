export enum FeatureFlags {
    DistributedCacheFlag = "GovernDistributedCachePerm",
    BrandingThemeCustomCss = "GovernBrandingCustomCssPerm",
    DataProfilingUiFlag = "GovernDataProfileUiPerm",
    SemanticTypesUiFlag = "GovernSemanticTypesUiPerm",
	ContainsSearchDefaultUiFlag = "GovernContainsSearchDefaultUiPerm",
	DashboardingEnabled = "govern-dashboarding-functionality-permanent",

	//temp flags
	AssignmentsFlag = "GovernAssignmentsTemp20230815",
	AssignmentDetailsFlag = "govern-workflow-assignments-requests-detail",
	RelationshipCardinalityTempFlag = "GovernRelationshipCardinalityTemp20230901",
	ReferenceListV2Flag = "GovernReferenceTemp20230901",
	ScoringEngineUpdate = "GovernScoringEngineUpdateTemp"
}
