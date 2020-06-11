using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("SynchedAssetTypeFieldItem", Schema = "integration")]
    public class IntegrationAssetTypeFieldItem : BaseIntObject
    {
        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public bool IncludeInPropertyRequest { get; set; } = true;

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string SourceField { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string TargetField { get; set; }

        [DataMember]
        public int? ParentContextPosition { get; set; } = null; // If this is populated, then we need to grab the value of the _id from the _context collection, based on the position.

        [DataMember]
        public bool IsArray { get; set; } = false;

        [DataMember]
        public string DefaultValue { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(10)]
        public string ArrayValueDelimiter { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string ArrayValueFieldName { get; set; }

        [DataMember]
        public bool Active { get; set; } = true;


        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual ICollection<IntegrationAssetType> IntegrationAssetType { get; set; }
    }
}
