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

    #region Asset Browser

    public class AssetBrowserLineageApiRelationshipModel
    {
        public Guid intersectUid { get; set; }
        public Guid subjectUid { get; set; }
        public string subjectKey { get; set; }
        public Guid objectUid { get; set; }
        public string objectKey { get; set; }
        public string predicate { get; set; }
        public Guid predicateUid { get; set; }
        public PredicateType predicateType { get; set; }
        public string backColor { get; set; }
        public string foreColor { get; set; }
    }

    public interface IAssetBrowserLineageApiItemModel
    {
        long ID { get; set; }
        Guid assetUid { get; set; }
        string displayValue { get; set; }
        string key { get; set; }
        List<AssetBrowserLineageApiItemModel> items { get; set; }
    }

    [DataContract]
    public class AssetBrowserLineageApiItemModel : IAssetBrowserLineageApiItemModel
    {
        [IgnoreDataMember]
        public long ID { get; set; }
        [DataMember]
        public Guid assetUid { get; set; }
        [DataMember]
        public string key { get; set; }
        [DataMember]
        public string displayValue { get; set; }
        [DataMember]
        public List<AssetBrowserLineageApiItemModel> items { get; set; }
    }

    [DataContract]
    public class AssetBrowserLineageApiTopItemModel : AssetBrowserLineageApiItemModel, IAssetBrowserLineageApiItemModel
    {
        [DataMember]
        public string backColor { get; set; }
        [DataMember]
        public string foreColor { get; set; }
    }

    public class AssetBrowserLineageApiResponseModel
    {
        public Guid focalAssetUid { get; set; }
        public List<AssetBrowserLineageApiTopItemModel> assets { get; set; } = new List<AssetBrowserLineageApiTopItemModel>();
        public List<AssetBrowserLineageApiRelationshipModel> intersects { get; set; } = new List<AssetBrowserLineageApiRelationshipModel>();
    }

    #endregion
}