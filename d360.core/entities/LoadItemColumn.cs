using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadItemColumn : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int LoadID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int RowIndex { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ColumnIndex { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string LookupObject { get; set; }

        [DataMember]
        public int? LookupObjectID { get; set; }

        [DataMember]
        public bool? Success { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual LoadColumn LoadColumn { get; set; }

        [IgnoreDataMember]
        public virtual LoadItem LoadItem { get; set; }
    }
}
