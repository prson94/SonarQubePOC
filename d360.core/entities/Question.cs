using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Question : BaseIntObject, IIntObject
    {
        [DataMember]
        public int SurveyID { get; set; }

        [DataMember]
        public string Comment { get; set; }

        public virtual Survey Survey { get; set; }


        public virtual ICollection<QuestionTypeOption> QuestionTypeOptions { get; set; }
    }
}
