using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ScoreType : BaseCreatedAndUpdatedIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        #endregion

        #region Collection Properties

        [XmlIgnore()]
        [ForeignKey("ScoreTypeID")]
        public virtual ICollection<Score> Scores { get; set; }

        #endregion
    }
}
