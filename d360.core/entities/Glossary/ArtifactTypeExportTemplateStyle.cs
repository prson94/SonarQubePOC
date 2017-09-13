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
    public class ArtifactTypeExportTemplateStyle : BaseIntObject, IIntObject, IUpdatedMetadata
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

        public int ArtifactTypeExportTemplateID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual ArtifactTypeExportTemplate ArtifactTypeExportTemplate { get; set; }

    }
}
