using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("FusionAttributeSynchronizationError", Schema = "utility")]
    public class FusionAttributeSynchronizationError : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int FusionID { get; set; }

        [Key, Column(Order = 2), DataMember]
        public int FusionAttributeTypeID { get; set; }

        [Key, Column(Order = 3), DataMember]
        public string SourceID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public DateTime Date { get; set; }


    }
}
