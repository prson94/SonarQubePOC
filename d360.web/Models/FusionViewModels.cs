using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    public class FusionAdministrationViewModel
    {
        public List<FusionType> Types { get; set; }
        public List<FusionAttributeType> AttributeTypes { get; set; }
    }

    public class FusionIndexModel
    {
        public List<FusionType> Types { get; set; }
    }

    public class FusionDetailModel
    {
        public Fusion Fusion { get; set; }
        public List<Field> Fields { get; set; }
        public string SelectedTab { get; set; }
        public int? SelectedFusionAttributeID { get; set; }
    }

    public class FusionAttributeNode
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Expanded { get; set; }
        public bool LoadOnDemand { get; set; }
        public List<FusionAttributeNode> Items { get; set; }
    }

    public class FusionEditModel
    {
        public Fusion Fusion { get; set; }
        public Fields Fields { get; set; }
    }

    public class FusionOwnerEditListModel
    {
        public string RelationshipOwnerObjectType { get; set; }

        public int RelationshipOwnerObjectID { get; set; }

        public int FusionID { get; set; }

        public List<FusionOwnerEditModel> Items { get; set; }
    }

    public class FusionOwnerEditModel
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public string ParentObjectType { get; set; }

        public int? ParentObjectID { get; set; }
    }

    public class FusionPromotionEditListModel
    {
        public int FusionID { get; set; }

        public string PromotionObjectType { get; set; }

        public int PromotionObjectID { get; set; }

        public string PromotionParentObjectType { get; set; }

        public int PromotionParentObjectID { get; set; }

        public bool Enabled { get; set; }

        public List<FusionPromotionEditModel> Items { get; set; }
    }

    public class FusionPromotionEditModel
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public string ParentObjectType { get; set; }

        public int? ParentObjectID { get; set; }
    }
}