using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Asset : BaseCreatedAndUpdatedLongObject, IEventTrackedEntity
    {
        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ObjectID { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember, StringLength(500)]
        public string SourceID { get; set; }

        [DataMember, StringLength(250)]
        public string Code { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string Color { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Icon { get; set; }

        [IgnoreDataMember, ReadOnly(true), Column(TypeName = "varchar"), StringLength(50)]
        public string KeyHash { get; set; }

        [IgnoreDataMember, ReadOnly(true), Column(TypeName = "varchar"), StringLength(50)]
        public string FieldHash { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }

        [DataMember]
        public virtual ICollection<Field> Fields { get; set; }
                
        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), Object),
                ObjectID = ObjectID,
                AssetTypeID = AssetTypeID,
                ObjectType = SystemObjects.Unknown,
                ObjectTypeID = -1
            };
        }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class AssetApiModel : BaseObject
    {
        public long ID { get; set; }

        public int AssetTypeID { get; set; }

        public string SourceID { get; set; }

        [DataMember, ForeignKey("AssetID")]
        public virtual ICollection<FieldApiModel> Fields { get; set; }
    }

    public class AssetsApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<dynamic> items { get; set; }
    }

    public class AssetAuditApiItemModel
    {
        public Guid uid { get; set; }
        public string name { get; set; }
        public Guid resourceUid { get; set; }
        public string resourceName { get; set; }
        public DateTime date { get; set; }
        public string action { get; set; }
        public Guid? actionAssetUid { get; set; }
        public Guid? actionAssetTypeUid { get; set; }
        public string actionObject { get; set; }
        public string actionObjectTypeName { get; set; }
        public string actionObjectName { get; set; }
        public string actionDescription { get; set; }
        public string field { get; set; }
        public string fieldType { get; set; }
        public string newValue { get; set; }
        public int @class { get; set; }
        public int version { get; set; }
        public string previousValue { get; set; }
    }

    public class AssetsAuditApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<AssetAuditApiItemModel> items { get; set; }
    }

    public class AssetsApiPermissionViewModel
    {
        [DataMember]
        public bool ReadAsset { get; set; }
        [DataMember]
        public bool ModifyAsset { get; set; }
        [DataMember]
        public bool DeleteAsset { get; set; }
    }



    public class AssetsByPathApiRequestModel : PagedApiBaseRequestModel
    {
        [DataMember]
        public string searchPhrase { get; set; }

        [DataMember]
        public IEnumerable<AssetsByPathItemApiFilterRequestModel> filters { get; set; }
    }

    public class AssetsByPathItemApiFilterRequestModel
    {
        [DataMember]
        public Guid? Uid { get; set; }

        [DataMember]
        public AssetTypeClass? Class { get; set; }

        [DataMember]
        public bool? UseAsTransformation { get; set; }

        [DataMember]
        public AssetsByPathItemApiFilterSideOfRelationshipRequestModel AsSideOfRelationship { get; set; }
    }

    public class AssetsByPathItemApiFilterSideOfRelationshipRequestModel
    {
        [DataMember]
        public PredicateType? PredicateType { get; set; }

        [DataMember]
        public Guid? PredicateUid { get; set; }

        [DataMember]
        public AssetsByPathItemApiFilterSideOfRelationshipRequestEnum Side { get; set; }
    }

    public enum AssetsByPathItemApiFilterSideOfRelationshipRequestEnum
    {
        Subject,
        Object
    }

    #region AssetsController.GetAssetsByPathAsync Return Types

    public class AssetsByPathApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<AssetsByPathItemApiViewModel> items { get; set; }
    }

    public class AssetsByPathItemApiViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string AssetTypeIcon { get; set; }

        [DataMember]
        public string AssetTypeName { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [IgnoreDataMember]
        public string SegmentsXml { get; set; }

        [DataMember]
        public IEnumerable<AssetsByPathItemSegmentApiViewModel> Segments
        {
            get
            {
                try
                {
                    return
                        from s in XElement.Parse(SegmentsXml).Elements("segment")
                        select new AssetsByPathItemSegmentApiViewModel { Value = s.Value };
                }
                catch
                {
                    return new List<AssetsByPathItemSegmentApiViewModel>();
                }
            }
        }
    }

    public class AssetsByPathItemSegmentApiViewModel
    {
        [DataMember]
        public string Value { get; set; }
    }

    #endregion


    public class AssetClassCountModel
    {
        public string @class { get; set; }
        public int numberOfAssets { get; set; }        
    }

    public class AssetsCountModel
    {
        public int totalNumberOfAssets { get; set; }
        public List<AssetClassCountModel> countsByAssetClass { get; set; }
    }

    public class AssetTypeCountModel
    {
        public Guid uid { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public Guid? parentUid { get; set; }
        public string @class { get; set; }
        public string name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string description { get; set; }
        public int? count { get; set; }

    }

    public class UserGetAPIRestrictionModel
    {
        public bool HasAssetRestriction { get; set; }
        public bool HasAssetTypeRestriction { get; set; }
        public bool HasAssetPermission { get; set; }
    }
    public class AssetTypePossibleOwnersModel
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
