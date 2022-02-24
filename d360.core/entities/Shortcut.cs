using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Shortcut : BaseIntObject
    {
        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Name { get; set; }
        
        [DataMember]
        [Column(TypeName = "VARCHAR")]
        [StringLength(50)]
        public string Icon { get; set; }
        
        [DataMember]
        [Column(TypeName = "VARCHAR")]
        [StringLength(250)]
        public string IconUrl { get; set; }
        
        [DataMember]
        [Column(TypeName = "VARCHAR")]
        [StringLength(250)]
        public string Url { get; set; }

        [DataMember]
        [StringLength(500)]
        public string Description { get; set; }

        [DataMember]
        [StringLength(100)]
        public string IconColor { get; set; }

        [DataMember]
        [StringLength(100)]
        public string TitleColor { get; set; }

        [DataMember]
        [StringLength(100)]
        public string BackgroundColor { get; set; }

        [DataMember]
        public int DisplayOrder { get; set; }

        [DataMember]
        public LinkTarget LinkTarget { get; set; }

        [NotMapped, DataMember]
        public string IconPayload { get; set; }

        [NotMapped, DataMember]
        public string FullURL
        {
            get
            {
                if (string.IsNullOrEmpty(IconUrl))
                {
                    return null;
                }
                else
                {
                    return constants.COMPANY_RESOURCES_URL + IconUrl;
                }
            }
        }
    }
}
