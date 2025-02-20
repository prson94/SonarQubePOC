namespace d360.core.queue
{
	public enum ApiExecutionAction
    {
        DeleteAssets = 0,
        PostAssets = 1,
        PutAssets = 2,
        DeleteRelationships = 3,
        PostRelationships = 4,
        PutRelationships = 5,
        DeleteAssetTypes = 6,
        PostAssetTypes = 7,
        PutAssetTypes = 8,
        PostCrossReferences = 9,
        PostDataQualityResults = 10,
        PostDataProfile = 11,
        PutDataProfile = 12,
        DeleteDataProfile = 13,
        PostResponsibilityOverride = 14,
        DeleteFieldTypes = 15,
        UpsertUsers = 16,
		PatchCatalog = 17,
		DeleteGroups = 18,
		PostGroups = 19,
		PutGroups = 20,
		DeleteDataQualityResults = 21,
		PostResponsibilityTypes = 22,
		PutResponsibilityTypes = 23,
		PutDataQualityResults = 24,
		DeleteUsers = 25,
		UpsertPredicates = 26,
		DeletePredicates = 27,
		PostScoreAllocation = 28,
		PutScoreAllocation = 29,
		DeleteScoreAllocation = 30,
		PostSemantic = 31,
		PutSemantic = 32,
		DeleteSemantic = 33,

		Miscellaneous = 100
	}
}
