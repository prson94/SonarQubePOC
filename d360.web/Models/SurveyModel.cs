using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Name = "Survey", Namespace = constants.NAMESPACE)]
    public class SurveyModel
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int ResourceID { get; set; }
        [DataMember]
        public string ResourceName { get; set; }
        [DataMember]
        public int PercentComplete { get; set; }
    }
}