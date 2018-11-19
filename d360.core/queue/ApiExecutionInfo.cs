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
    }
    public class ApiExecutionInfo
    {
        public int CompanyID { get; set; }

        public string CompanyDomainPrefix { get; set; }

        public Guid ExecutionID { get; set; }

        public ApiExecutionAction Action { get; set; }

        [JsonIgnore]
        public string StorageFolder { get { return $"api-execution-{CompanyID}"; } }

        [JsonIgnore]
        public string RequestFileName { get { return $"{ExecutionID}_request.json"; } }

        [JsonIgnore]
        public string ResponseFileName { get { return $"{ExecutionID}_response.json"; } }
    }
}
