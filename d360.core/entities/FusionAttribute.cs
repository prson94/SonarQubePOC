using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.FusionAttribute, "FusionAttribute")]
    public class FusionAttribute : BaseIntObject, IIntObject, IFieldsObject, ISearchable
    {
        [DataMember]
        public int? ParentID { get; set; }

        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"),
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public int FusionAttributeTypeID { get; set; }

        [ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string Path { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        [DataMember]
        public bool Deleted { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttributeType FusionAttributeType { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttribute Parent { get; set; }

        [IgnoreDataMember, ForeignKey("ParentID")]
        public virtual List<FusionAttribute> Children { get; set; }
    }
}
