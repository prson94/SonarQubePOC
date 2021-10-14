using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetType : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public AssetTypeClass Class { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        [DataMember]
        public FlowObjectType? FlowObjectType { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember]
        public bool Hierarchical { get; set; }

        [DataMember]
        public int? HierarchyIntersectTypeID { get; set; }

        [DataMember]
        public int? HierarchyPredicateID { get; set; }

        [DataMember]
        public int HierarchyMaximumDepth { get; set; }

        [DataMember]
        public string Notes { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ObjectID { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeID")]
        public virtual ICollection<Asset> Assets { get; set; }

        [DataMember]
        public bool AutoDisplayDescription { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeID")]
        public virtual ICollection<AssetTypeLevel> AssetTypeLevels { get; set; }

        [IgnoreDataMember, ForeignKey("ID")]
        public virtual AssetTypeStyle AssetTypeStyle { get; set; }

        [DataMember]
        public bool UseAsTransformation { get; set; }
        
        [NotMapped]
        public AssetType Parent { get; set; }
        
        [DataMember]
        public bool? AutoDisplayParent { get; set; }

        [DataMember]
        public bool? CanEditParent { get; set; }

    }

    public class AssetTypeBrowserApiViewModel
    {
        public Guid uid { get; set; }

        public string Name { get; set; }

        int _ClassID;
        public int ClassID
        {
            get { return _ClassID; }
            set
            {
                _ClassID = value;
                this.Class = ((AssetTypeClass)_ClassID).AsInfoModel();
            }
        }
        public AssetTypeClassInfo Class { get; set; }
        public string Path { get; set; }
    }


    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeApiViewModel : BaseObject
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        AssetTypeClass _ClassID;
        public AssetTypeClass ClassID
        {
            get { return _ClassID; }
            set
            {
                _ClassID = value;
                this.Class = _ClassID.AsInfoModel();
            }
        }
        [DataMember]
        public AssetTypeClassInfo Class { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool Hierarchical { get; set; }
        [DataMember]
        public int HierarchyMaximumDepth { get; set; }
        [DataMember]
        public string DisplayFormat { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [DataMember]
        public bool AutoDisplayDescription { get; set; }
        [DataMember]
        public bool UseAsTransformation { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public IconStyleInsert IconStyle { get; set; }
        [DataMember]
        public bool? AutoDisplayParent { get; set; }
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public FlowObjectType? FlowObjectType { get; set; }
        [DataMember]
        public bool? CanEditParent { get; set; }

        [IgnoreDataMember]
        public string LevelsJson { get; set; }

        [DataMember]
        public List<AssetTypeLevelApiViewModel> Levels 
        { 
            get 
            {
                if (string.IsNullOrEmpty(LevelsJson))
                {
                    return null;
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<AssetTypeLevelApiViewModel>>(LevelsJson);
                }
            }
        }
    }
}
