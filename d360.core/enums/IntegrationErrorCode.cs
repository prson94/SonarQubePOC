using System.ComponentModel;

namespace d360.core.enums
{
    public enum IntegrationErrorCode
    {
        [Description("The reported asset count from the source system did not match what we were able to pull from the API. Will not proceed with deletion.")]
        GenericFieldSectionError = 1,
        [Description("The reported asset count from the source system did not match what we were able to pull from the API. Will not proceed with deletion.")]
        GenericRelationshipSectionError = 2,
        [Description("The reported asset count from the source system did not match what we were able to pull from the API. Will not proceed with deletion.")]
        GenericResponsibilitySectionError = 3,
        [Description("The reported asset count from the source system did not match what we were able to pull from the API. Will not proceed with deletion.")]
        SourceCountPullCountNotSameError = 4,
        [Description("The parent/child association could not be resolved based on identifiers.")]
        ParentChildRelationNotResolvedError = 5,
        [Description("The association could not be resolved based on identifier.")]
        RelationNotResolvedError = 6,
        [Description("The user could not be resolved based on identifier.")]
        UserNotResolvedError = 7,
        [Description("Unable to clear out hashes for section specified.")]
        UnableToClearSectionHashesError = 8,
        [Description("Unable to calculate the execution count since last hash clearance.")]
        UnableToDetermineLastHashClearError = 9,
        [Description("Task cancellation requested.")]
        TaskCanceledGeneralError = 10,
        [Description("General exception.")]
        GeneralError = 11,
        [Description("IGC HTML Error recieved.")]
        IgcPageHtmlError = 12,
        [Description("Asset count mismatch between IGC and Govern.")]
        AssetCountMismatchError = 13,
        [Description("IGC asset count does not match Govern asset count.")]
        AssetSourceCountGovernCountMismatchError = 14,
        [Description("Assets with missing definition.")]
        AssetsMissingDefinitionError = 15,
        [Description("Assets with missing relationships.")]
        AssetsMissingRelationshipsError = 16,
        [Description("Assets with missing responsibilities.")]
        AssetsMissingResponsibilitiesError = 17,
        [Description("Relationship count mismatch between IGC and Govern.")]
        RelationshipCountMismatchError = 18,
        [Description("IGC relationship count does not match Govern relationship count.")]
        RelationshipSourceCountGovernCountMismatchError = 19,
        [Description("Responsibility count mismatch between IGC and Govern.")]
        ResponsibilityCountMismatchError = 20,
        [Description("IGC responsibility count does not match Govern responsibility count.")]
        ResponsibilitySourceCountGovernCountMismatchError = 21,
        [Description("More assets deleted than expected.")]
        AssetUnexpectedDeletedCount = 22,
        [Description("More asset relationships deleted than expected.")]
        RelationshipsUnexpectedDeletedCount = 23,
        [Description("Unable to connect to source system.")]
        ConnectivityError = 24,
        [Description("Source system paging error.")]
        PagingError = 25
    }
}
