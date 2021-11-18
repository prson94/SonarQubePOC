using Newtonsoft.Json;
using System;

namespace d360.core.queue
{
    public enum ApiExecutionAction
    {
        DeleteAssets,
        PostAssets,
        PutAssets,
        DeleteRelationships,
        PostRelationships,
        PutRelationships,
        DeleteAssetTypes,
        PostAssetTypes,
        PutAssetTypes,
        PostCrossReferences,
        PostDataQualityResults,
        PostDataProfile,
        PutDataProfile,
        DeleteDataProfile,
        PostResponsibilityOverride,
        DeleteFieldTypes
    }
    public class ApiExecutionInfo: IServiceBusMessageType
    {
        public int CompanyID { get; set; }

        public int? ResourceID { get; set; }

        public string CompanyDomainPrefix { get; set; }

        public Guid ExecutionID { get; set; }

        public ApiExecutionAction Action { get; set; }

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
