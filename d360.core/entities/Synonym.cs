using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.entities.Contracts;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Synonym, "Synonym")]
    public partial class Synonym : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember, StringLength(25), Column(TypeName = "varchar")]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        [StringLength(250)]
        public string Name { get; set; }
        
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTime CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;

        public int CreatedBy { get; set; }

        public int PredicateID { get; set; }

        [IgnoreDataMember]
        public virtual Predicate Predicate { get; set; }
    }
}
