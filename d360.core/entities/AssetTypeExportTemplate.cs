using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeExportTemplate : BaseCreatedAndUpdatedObject
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string UsageNotes { get; set; }

        [DataMember]
        public string[] IncludeFieldTypes { get; set; }

        [DataMember]
        public bool IncludeUrl { get; set; }

        [DataMember]
        public bool IncludeParent { get; set; }

        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExportView ExportViewType { get; set; }

        [IgnoreDataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public byte[] TemplateFile { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        #endregion

        [NotMapped, DataMember]
        public Guid AssetTypeUID { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }

        [NotMapped, IgnoreDataMember]
        public string AssetTypeExportTemplateStyleJson { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeExportTemplateID")]
        public virtual ICollection<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }

        [
        IgnoreDataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity),
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ID_Name", Description = "ID_Description")
        ]
        public int ID { get; set; }
    }
}
