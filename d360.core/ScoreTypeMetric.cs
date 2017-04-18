using d360.core.entities.Contracts;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ScoreTypeMetric : BaseCreatedAndUpdatedIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int ScoreTypeID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public StatisticCheckType CheckType { get; set; }
        
        [DataMember]
        public string Configuration { get; set; }

        [DataMember]
        public int MaximumScore { get; set; }

        [DataMember]
        public bool Deleted { get; set; }


        [XmlIgnore()]
        [ForeignKey("ScoreTypeID")]
        public virtual ScoreType ScoreType { get; set; }
    }
}
