using System;
using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Net;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionExternal", Schema = "api")]
    public class ApiExecutionsExternal : BaseIntObject, IIntObject
    {
        [DataMember]
        public Guid ExternalId { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Status { get; set; }

        [DataMember]
        public string Detail { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Component { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public string Configuration { get; set; }
    }


    public class ApiExecutionExternalRequestModel
    {
        public string Status { get; set; }
        public Guid? ExternalId { get; set; }
        public string Detail { get; set; }
        public string Component { get; set; }
        public List<Dictionary<string, object>> Configuration { get; set; }
    }

    public class ApiExecutionExternalViewModel
    {
        public string Status { get; set; }
        public Guid ExternalId { get; set; }
        public string Detail { get; set; }
        public string Component { get; set; }
        public DateTime? CreatedOn { get; set; }
        public List<Dictionary<string, object>> Configuration { get; set; }
    }

    public class APIExecutionExternalAPIModelResult : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<APIExecutionExternalAPIModel> items { get; set; }
        [IgnoreDataMember]
        public HttpStatusCode StatusCode { get; set; }
        [IgnoreDataMember]
        public string Message { get; set; }
    }

    public class APIExecutionExternalAPIModel
    {
        public string status { get; set; }
        public Guid externalId { get; set; }
        public string detail { get; set; }
        public string component { get; set; }
        public DateTime createdOn { get; set; }
        public List<Dictionary<string, object>> Configuration { get; set; }
        [IgnoreDataMember]
        public string ConfigurationJSON { get; set; }
    }

    public class APIExecutionBulkLoadModel
    {
        public int total { get; set; }
        public int pageNum { get; set; }
        public int pageSize { get; set; }
        public IEnumerable<LoadDetailV2> items { get; set; }
    }

    public class APIExecutionBulkLoadItemDetailsModel
    {
        public int total { get; set; }
        public int pageNum { get; set; }
        public int pageSize { get; set; }
        public List<LoadItemDetail> items { get; set; }
    }
}
