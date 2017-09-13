using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ArtifactTypeExportTemplate : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties
                
        [DataMember]        
        public string Name { get; set; }

        [DataMember]        
        public string Description { get; set; }

        [DataMember]
        public string UsageNotes { get; set; }

        public string IncludeFields { get; set; }

        public bool IncludeUrl { get; set; }
        public bool IncludeParent { get; set; }

        public ExportView ExportViewType { get; set; }

        public int ArtifactTypeID { get; set; }

        [DataMember]
        public byte[] TemplateFile { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual ArtifactType ArtifactType { get; set; }

        [IgnoreDataMember, ForeignKey("ArtifactTypeExportTemplateID")]
        public virtual ICollection<ArtifactTypeExportTemplateStyle> ArtifactTypeExportTemplateStyles { get; set; }
    }
}
