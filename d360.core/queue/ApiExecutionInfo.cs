using System;

using Newtonsoft.Json;

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

		Miscellaneous = 100
	}
    public class ApiExecutionInfo : IServiceBusMessageType
    {
        public int CompanyID { get; set; }

        public int? ResourceID { get; set; }

        public string CompanyDomainPrefix { get; set; }

        public Guid ExecutionID { get; set; }

        public ApiExecutionAction? Action { get; set; }

        public bool SendWorkflowEvents { get; set; } = true;

        [JsonIgnore]
        public string StorageFolder { get { return $"api-execution"; } }

        [JsonIgnore]
        public string RequestFileName { get { return $"{CompanyID}/{ExecutionID}_request.json"; } }

        [JsonIgnore]
        public string ResponseFileName { get { return $"{CompanyID}/{ExecutionID}_response.json"; } }

        [JsonIgnore]
        public int MessageType { get { return (int)Action; } }
    }
}
