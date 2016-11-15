using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Favorite : BaseIntObject, IIntObject
    {
        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public bool IsOverride { get; set; } = false;

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }        
    }
}
