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
        PutAssetTypes
    }
    public class ApiExecutionInfo
    {
        public int CompanyID { get; set; }

        public int? ResourceID { get; set; }

        public string CompanyDomainPrefix { get; set; }

        public Guid ExecutionID { get; set; }

        public ApiExecutionAction Action { get; set; }

        [JsonIgnore]
        public string StorageFolder { get { return $"api-execution"; } }

        [JsonIgnore]
        public string RequestFileName { get { return $"{CompanyID}/{ExecutionID}_request.json"; } }

        [JsonIgnore]
        public string ResponseFileName { get { return $"{CompanyID}/{ExecutionID}_response.json"; } }
    }
}
