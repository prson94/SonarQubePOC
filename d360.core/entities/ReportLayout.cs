using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.ReportLayout, "ReportLayout")]
    public class ReportLayout : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description"), StringLength(1000)]
        public string Description { get; set; }

        [DataMember, StringLength(1000)]
        public string Template { get; set; }

        [DataMember]
        public int NumberOfContentAreas { get; set; }
    }
}
