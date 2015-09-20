using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class SynonymTypeRelationDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int SynonymTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string TypeName { get; set; }
    }
}
