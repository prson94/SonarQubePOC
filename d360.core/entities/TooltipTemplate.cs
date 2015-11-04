using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.TooltipTemplate, "TooltipTemplate")]
    public class TooltipTemplate : BaseIntObject, IIntObject
    {
        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Body_Name", Description = "Body_Description"), 
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Body_ErrorRequired")
        ]
        public string TemplateBody { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Action_Name", Description = "Action_Description")]
        public string Action { get; set; }
    }
}
