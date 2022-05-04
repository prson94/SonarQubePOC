using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SiteNav : BaseIntObject, IIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Route { get; set; }

        [DataMember]
        public int? SortOrder { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(100)]
        public string Icon { get; set; }

        [DataMember]
        public string Title { get; set; }

        [NotMapped, DataMember]
        public List<SiteNavPermission> Permissions { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(100)]
        public string ImageIconUrl { get; set; }

        [NotMapped, DataMember]
        public string IconPayload { get; set; }

        [NotMapped, DataMember]
        public string Type { get { return "Folder1"; } }

        [NotMapped, DataMember]
        public string FullURL
        {
            get
            {
                if (string.IsNullOrEmpty(ImageIconUrl))
                {
                    return null;
                }
                else
                {
                    return constants.COMPANY_RESOURCES_URL + ImageIconUrl;
                }
            }
        }
    }

    public class SiteNavPermission : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int SiteNavID { get; set; }
        
        [Key, Column(Order = 2), DataMember]
        public string Object { get; set; }
        
        [Key, Column(Order = 3), DataMember]
        public int ObjectID { get; set; }

        [NotMapped, DataMember]
        public string Name { get; set; }
    }
}