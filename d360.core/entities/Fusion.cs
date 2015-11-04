using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Fusion, "Fusion")]
    public class Fusion : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata
    {
        public int FusionTypeID { get; set; }

        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"),
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Enabled_Name", Description = "Enabled_Description")]
        public bool Enabled { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LockPromotedItems_Name", Description = "LockPromotedItems_Description")]
        public bool LockPromotedItems { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IsManual_Name", Description = "IsManual_Description")]
        public bool Manual { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Interval_Name", Description = "Interval_Description")]
        public int? Interval { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IntervalType_Name", Description = "IntervalType_Description")]
        public JobIntervalType? IntervalType { get; set; }

        [DataMember]
        public bool? ForceRefresh { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual FusionType FusionType { get; set; }

        [IgnoreDataMember, ForeignKey("FusionID")]
        public virtual ICollection<FusionAttribute> FusionAttributes { get; set; }

        [IgnoreDataMember, ForeignKey("FusionID")]
        public virtual ICollection<FusionFilter> FusionFilters { get; set; }
    }
}
