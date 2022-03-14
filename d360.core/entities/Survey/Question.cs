using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Question : BaseIntObject, IIntObject
    {
        [DataMember]
        public int SurveyID { get; set; }

        [DataMember]
        public string Comment { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        public virtual Survey Survey { get; set; }

        public virtual ICollection<QuestionTypeOption> QuestionTypeOptions { get; set; }
    }
}
