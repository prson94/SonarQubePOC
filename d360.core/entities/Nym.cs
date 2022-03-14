using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class Nym : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        public Nym()
        {
            Visible = true;
        }

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
                return createdon.HasValue
                   ? createdon.Value
                   : DateTime.UtcNow;
            }

            set { createdon = value; }
        }

        private DateTime? createdon = null;

        public int CreatedBy { get; set; }

        public int PredicateID { get; set; }

        public bool Visible { get; set; }

        [IgnoreDataMember]
        public virtual Predicate Predicate { get; set; }
    }
}
