using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeStyle : BaseObject
    {
        [DataMember, Key]
        public int ID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconBackColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconForeColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string IconText { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Icon { get; set; }

        #region Navigation Properties

        [IgnoreDataMember, ForeignKey("ID")]
        public virtual AssetType AssetType { get; set; }

        #endregion
    }
}
