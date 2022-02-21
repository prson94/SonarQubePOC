using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetResponsibilitiesApiModel : BaseObject
    {
        [DataMember]
        public int pageSize { get; set; }

        [DataMember]
        public int pageNum { get; set; }

        [DataMember]
        public int total { get; set; }

        [DataMember]
        public List<AssetResponsibilityItemModel> items { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityApiModel : BaseObject
    {
        public long AssetID { get; set; }

        [DataMember]
        public bool AssignedToType { get; set; }

        [DataMember]
        public Guid ResponsibilityTypeUid { get; set; }

        [DataMember]
        public string ResponsibilityTypeName { get; set; }

        [DataMember]
        public string AssigneeMethod { get; set; }

        [DataMember]
        public Guid AssigneeUid { get; set; }

        [DataMember]
        public string AssigneeName { get; set; }

        public int SecurityAssetID { get; set; }

        public string SecurityAsset { get; set; }

        [DataMember]
        public string AssigneeType
        {
            get
            {
                switch ((SecurityAsset ?? "").ToUpper())
                {
                    case "R":
                        return "resource";
                    case "G":
                        return "group";
                    case "O":
                        return "organization";
                    default:
                        return null;
                }
            }
        }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class OwnershipApiModel : BaseObject
    {
        [DataMember]
        public string Responsibility { get; set; }

        [DataMember]
        public Guid ResponsibilityUid { get; set; }

        [DataMember]
        public string Resource { get; set; }

        [DataMember]
        public Guid ResourceUid { get; set; }

        [DataMember]
        public Guid? GroupResourceUid { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string AssignedBy { get; set; }

        [DataMember]
        public string ResourceType { get; set; }

        [DataMember]
        public bool IsVisible { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class AssetResponsibilityItemModel : BaseObject
    {
        public long AssetID { get; set; }

        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string AssetTypeName { get; set; }

        [DataMember]
        public List<ResponsibilityApiModel> Responsibilities { get; set; }
    }
}
