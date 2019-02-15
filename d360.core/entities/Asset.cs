using d360.core.entities.Contracts;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Asset : BaseCreatedAndUpdatedLongObject
    {
        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember]
        public string SourceID { get; set; }
        
        [IgnoreDataMember, ReadOnly(true), Column(TypeName = "varchar"), StringLength(50)]
        public string KeyHash { get; set; }

        [IgnoreDataMember, ReadOnly(true), Column(TypeName = "varchar"), StringLength(50)]
        public string FieldHash { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }

        [DataMember]
        public virtual ICollection<Field> Fields { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<Fusion> OwnedFusions { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class AssetApiModel: BaseObject
    {
        public long ID { get; set; }

        public int AssetTypeID { get; set; }

        public string SourceID { get; set; }
                
        [DataMember, ForeignKey("AssetID")]
        public virtual ICollection<FieldApiModel> Fields { get; set; }
    }

    public class AssetsApiViewModel
    {
        [DataMember]
        public int pageSize { get; set; } = 25000;
        [DataMember]
        public int pageNum { get; set; } = 1;
        [DataMember]
        public int total { get; set; } = 0;
        [DataMember]
        public IEnumerable<dynamic> items { get; set; }
    }
}
