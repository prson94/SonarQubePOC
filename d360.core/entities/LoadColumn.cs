using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadColumn : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int LoadID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ColumnIndex { get; set; }

        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public bool IsDynamic { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual Load Load { get; set; }
    }
}
