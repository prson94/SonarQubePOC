using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class TaxonomyDetail : BaseIntObject, IIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string DisplayValue { get; set; }

        [DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string TextPath { get; set; }

        [DataMember]
        public int TaxonomyTypeID { get; set; }

        [DataMember]
        public int Level { get; set; }

        [DataMember]
        public bool HasChildren { get; set; }
    }
}
