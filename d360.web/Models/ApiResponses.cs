using d360.core;
using d360.core.entities;
using d360.core.enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace d360.web.Models
{
    public class ApplicationSetting
    {
        public string Name { get; set; }

        public dynamic Value { get; set; }
    }

    public class ApiExecutionRecievedResponse
    {
        public Guid ExecutionID { get; set; }

        public string Message { get; set; }

        public string Uri { get; set; }
    }

    public class ApiStatusResponse
    {
        public Guid Uid { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }

    public class ApiExecutionStatusModel
    {
        public int Total { get; set; }

        public int Processed { get; set; }

        public int Error { get; set; }

        public JObject Fields { get; set; }

        public DateTime StartedOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public List<DatabaseBulkAssetResult> Results { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ConfirmResponse
    {
        [DataMember]
        public string type { get; set; } = "confirm";

        [DataMember]
        public string title { get; set; } = "Success";

        [DataMember]
        public string message { get; set; } = "success";
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class CreateResponse
    {
        [DataMember]
        public string Message { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ErrorResponse
    {
        [DataMember]
        public string type { get; set; } = "error";

        [DataMember]
        public string title { get; set; } = "An error occured";

        [DataMember]
        public string message { get; set; } = "error";
    }

    public class SelectListInfoItem : System.Web.Mvc.SelectListItem
    {
        public string Info { get; set; }
    }
}