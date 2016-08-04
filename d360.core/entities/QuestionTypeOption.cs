using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

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

        [ScriptIgnore, XmlIgnore()]
        public virtual QuestionType QuestionType { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
    }
}
