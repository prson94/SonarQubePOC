using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Name = "QuestionResponse", Namespace = constants.NAMESPACE)]
    public class QuestionResponseModel
    {
        [DataMember]
        public int QuestionTypeID { get; set; }
        [DataMember]
        public int SurveyTypeID { get; set; }
        [DataMember]
        public SystemObjects ObjectType { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public int Value { get; set; }
        [DataMember]
        public string Comment { get; set; }
    }
}