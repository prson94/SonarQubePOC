using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Survey : BaseIntObject, IIntObject
    {
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public int SurveyTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual SurveyType SurveyType { get; set; }

        [ForeignKey("SurveyID")]
        public virtual ICollection<Question> Questions { get; set; }
    }
}
