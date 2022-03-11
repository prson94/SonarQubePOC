using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeExportTemplateStyle : BaseCreatedAndUpdatedIntObject
    {
        #region Properties

        [DataMember]
        public int Row { get; set; }

        [DataMember]
        public int Column { get; set; }

        [DataMember]
        public int? Color { get; set; }

        [DataMember]
        public bool IsBold { get; set; }

        [DataMember]
        public int? BackgroundColor { get; set; }

        [NotMapped, DataMember]
        public string TextColor { get; set; }

        [NotMapped, DataMember]
        public string BgColor { get; set; }

        [DataMember]
        public int AssetTypeExportTemplateID { get; set; }

        public int BackgroundColorValueFieldTypeID { get; set; }

        public int ColorValueFieldTypeID { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual AssetTypeExportTemplate AssetTypeExportTemplate { get; set; }
    }
}
