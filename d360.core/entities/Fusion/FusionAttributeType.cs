using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributeType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        public int? ParentID { get; set; }

        [DataMember, NotMapped]
        public int? AssetTypeID { get; set; }

        [DataMember, NotMapped]
        public Guid uid { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FusionType_Name", Description = "FusionType_Description")]
        public int FusionTypeID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        [DataMember]
        public bool ScanEnabled { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FusionType_Name", Description = "FusionType_Description")]
        public virtual FusionType FusionType { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttributeType Parent { get; set; }

        [IgnoreDataMember, ForeignKey("ParentID")]
        public virtual ICollection<FusionAttributeType> Children { get; set; }

        [IgnoreDataMember, ForeignKey("FusionAttributeTypeID")]
        public virtual ICollection<FusionAttribute> FusionAttributes { get; set; }
    }
}
