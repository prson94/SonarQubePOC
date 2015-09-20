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
    [DataContract(Namespace = NAMESPACE)]
    public class FieldLookupValue: BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int FieldTypeID { get; set; }
        
        [Column(Order = 2), DataMember, Key]
        public string LookupObjectType { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int? LookupObjectID { get; set; }

        [Column(Order = 4), DataMember, Key]
        public int? Value { get; set; }

        [Column(Order = 5), DataMember, Key]
        public string Text { get; set; }
    }
}
