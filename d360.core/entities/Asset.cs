using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Asset : BaseObject, IDisplayValueObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public string DisplayValue { get; set; }
        
        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public State State { get; set; }

        [IgnoreDataMember]
        public string KeyHash { get; set; }

        [IgnoreDataMember]
        public string FieldHash { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }
    }
}
