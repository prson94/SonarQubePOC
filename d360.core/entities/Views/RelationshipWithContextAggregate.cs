using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class RelationshipWithContextAggregate : BaseObject
    {
        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 1)]
        public int IntersectID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public int TypeID { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }

        [DataMember]
        public string IconText { get; set; }

        [DataMember]
        public int ContextCount { get; set; }

        [DataMember]
        public int CriticalContextCount { get; set; }
    }
}
