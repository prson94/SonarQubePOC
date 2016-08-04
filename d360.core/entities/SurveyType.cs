using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SurveyType : BaseIntObject, ICreatedObject, IIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int ValidForDays { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<QuestionType> QuestionTypes { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<Survey> Surveys { get; set; }

    }
}
