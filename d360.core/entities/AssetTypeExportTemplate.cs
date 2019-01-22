using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeExportTemplate : BaseCreatedAndUpdatedIntObject
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string UsageNotes { get; set; }
        
        [DataMember]
        public string IncludeFields { get; set; }

        [DataMember]
        public bool IncludeUrl { get; set; }

        [DataMember]
        public bool IncludeParent { get; set; }

        [DataMember]
        public ExportView ExportViewType { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public byte[] TemplateFile { get; set; }

        #endregion
        [NotMapped, DataMember]
        public Guid AssetTypeUID { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }
        
        [IgnoreDataMember, ForeignKey("AssetTypeExportTemplateID")]
        public virtual ICollection<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }
    }
}