using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadItem : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int LoadID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int RowIndex { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public string KeyHash { get; set; }

        [DataMember]
        public string FieldHash { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        public bool? Status { get; set; }

        [DataMember]
        public string StatusMessage { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid? AssetUid { get; set; }

        [DataMember]
        public Guid? ParentAssetUid { get; set; }

        [DataMember]
        public Guid? IntersectUid { get; set; }


        [IgnoreDataMember, NotMapped]
        public int Level { get; set; }
        #endregion

        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual Load Load { get; set; }

        [IgnoreDataMember, ForeignKey("LoadID, RowIndex")]
        public virtual ICollection<LoadItemColumn> LoadItemColumns { get; set; }
    }
}
