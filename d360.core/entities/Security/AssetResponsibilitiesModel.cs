using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string ResponsibilityName { get; set; }

        [DataMember]
        public string AssigneeMethod { get; set; }

        [DataMember]
        public Guid AssigneeUid { get; set; }

        public int SecurityAssetID { get; set; }

        public string SecurityAsset { get; set; }

        [DataMember]
        public string AssigneeType {
            get
            {
                switch ((SecurityAsset ??"").ToUpper())
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
