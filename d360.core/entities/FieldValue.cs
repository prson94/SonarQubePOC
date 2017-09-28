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
    public class FieldValue : BaseIntObject
    {
        [Column(Order = 1, TypeName = "varchar"), DataMember, StringLength(25)]
        public string ObjectType { get; set; }

        [Column(Order = 2), DataMember]
        public int ObjectID { get; set; }

        [Column(Order = 3), DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public string Value { get; set; }
                
        [IgnoreDataMember]
        public FieldType FieldType { get; set; }
    }
}