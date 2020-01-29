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

    #region Asset

    [DataContract]
    public class AssetBrowserAssetModel
    {
        [DataMember]
        public int hop { get; set; }
        [DataMember]
        public int assetTypeId { get; set; }
        [DataMember]
        public Guid assetTypeUid { get; set; }
        [DataMember]
        public Guid assetUid { get; set; }
        [DataMember]
        public string key { get; set; }
        [DataMember]
        public string parentKey { get; set; }
        [DataMember]
        public string displayValue { get; set; }
        [DataMember]
        public string backColor { get; set; }
        [DataMember]
        public double backAmount { get; set; }
        [DataMember]
        public string foreColor { get; set; }
        [DataMember]
        public double foreAmount { get; set; }
        [DataMember]
        public string icon { get; set; }
        [DataMember]
        public bool useAsTransformation { get; set; }
        [DataMember]
        public bool hasAssetReadAccess { get; set; }
        [DataMember]
        public bool isSubjectInTransformation { get; set; }
        [DataMember]
        public AssetTypeClass @class { get; set; }
        [DataMember]
        public AssetBrowserApiHopDirection reveal { get; set; }
        [DataMember]
        public List<AssetBrowserOwnerCountModel> ownerCounts { get; set; }
        [DataMember]
        public List<AssetBrowserAssetRelationCountModel> relationCounts { get; set; }
        [DataMember]
        public List<AssetBrowserAssetModel> items { get; set; }
    }

    public class AssetBrowserAssetsModel
    {
        public List<AssetBrowserAssetModel> assets { get; set; } = new List<AssetBrowserAssetModel>();
        public List<AssetBrowserAssetRelationModel> assetRelations { get; set; } = new List<AssetBrowserAssetRelationModel>();
    }

    public class AssetBrowserAssetRelationCountModel
    {
        public string Predicate { get; set; }
        public int PredicateID { get; set; }
        public Guid PredicateUid { get; set; }
        public AssetBrowserApiHopDirection Direction { get; set; }
        public int Count { get; set; }
    }

    public class AssetBrowserAssetRelationModel
    {
        public Guid intersectUid { get; set; }
        public Guid subjectUid { get; set; }
        public string subjectKey { get; set; }
        public Guid objectUid { get; set; }
        public string objectKey { get; set; }
        public string predicate { get; set; }
        public int predicateId { get; set; }
        public Guid predicateUid { get; set; }
        public PredicateType predicateType { get; set; }
        public string backColor { get; set; }
        public string foreColor { get; set; }
        public string icon { get; set; }
    }

    #endregion

    #region Owner

    [DataContract]
    public class AssetBrowserOwnerModel
    {
        [DataMember]
        public string key { get; set; }
        [DataMember]
        public string displayValue { get; set; }
        [DataMember]
        public string backColor { get; set; }
        [DataMember]
        public string foreColor { get; set; }
        [DataMember]
        public string icon { get; set; }
        
        //[DataMember]
        public Guid assetUid { get; set; }
        
        [DataMember]
        public Guid resourceUid { get; set; }
    }

    public class AssetBrowserOwnersModel
    {
        public List<AssetBrowserOwnerModel> owners { get; set; } = new List<AssetBrowserOwnerModel>();
        public List<AssetBrowserAssetRelationModel> ownerRelations { get; set; } = new List<AssetBrowserAssetRelationModel>();
    }

    public class AssetBrowserOwnerCountModel
    {
        public string ResponsibilityType { get; set; }
        public int ResponsibilityTypeID { get; set; }
        public int Count { get; set; }
    }

    public class AssetBrowserOwnerRelationModel
    {
        public Guid assetUid { get; set; }
        public Guid ownerUid { get; set; }
        public string assetKey { get; set; }
        public string ownerKey { get; set; }
        public string backColor { get; set; }
        public string foreColor { get; set; }
    }

    #endregion

    #endregion
}