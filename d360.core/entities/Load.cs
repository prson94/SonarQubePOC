using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Load : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(2)]
        public string Action { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(10)]
        public string Extension { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public byte[] File { get; set; }

        [DataMember]
        public string Notes { get; set; }

        [DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [DataMember]
        public Guid? AssetTypeUid { get; set; }

        [DataMember]
        public Guid? IntersectTypeUid { get; set; }

        [DataMember]
        public Guid? PutExecutionID { get; set; }

        [DataMember]
        public Guid? PostExecutionID { get; set; }

        [DataMember]
        public Guid? uid { get; set; }

        #endregion


        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual ICollection<LoadColumn> LoadColumns { get; set; }

        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual ICollection<LoadItem> LoadItems { get; set; }

        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual ICollection<LoadItemColumn> LoadItemColumns { get; set; }
    }
}
