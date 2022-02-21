using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using d360.core.entities.Contracts;

using Newtonsoft.Json;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class QuestionTypeOption : BaseIntObject, IIntObject
    {
        [DataMember]
        public int QuestionTypeID { get; set; }

        [DataMember, StringLength(500)]
        public string Name { get; set; }

        [DataMember]
        public int Value { get; set; }

        [JsonIgnore, XmlIgnore()]
        public virtual QuestionType QuestionType { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
    }
}
