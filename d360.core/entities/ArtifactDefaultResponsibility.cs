using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ArtifactDefaultResponsibility : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int ArtifactTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int VocabularyID { get; set; }

        [DataMember]
        public int? TaxonomyTypeID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ResponsibilityTypeID { get; set; }

        #endregion

        public virtual ArtifactType ArtifactType { get; set; }

        public virtual Vocabulary Vocabulary { get; set; }

        public virtual TaxonomyType TaxonomyType { get; set; }

        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
