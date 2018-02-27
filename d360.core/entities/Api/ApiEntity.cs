using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Entity", Schema = "api")]
    public class ApiEntity : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EndpointVersionID { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [ForeignKey("EndpointVersionID"), IgnoreDataMember]
        public virtual ApiEndpointVersion Version { get; set; }

        [ForeignKey("AssetTypeID"), IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }

        [ForeignKey("EntityID"), IgnoreDataMember]
        public virtual ICollection<ApiEntityFieldType> FieldTypes { get; set; }

        [ForeignKey("EntityID"), IgnoreDataMember]
        public virtual ICollection<ApiEntityUri> Uris { get; set; }
    }
}
