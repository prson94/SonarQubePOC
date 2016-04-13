using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Survey : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public int SurveyTypeID { get; set; }

        #region Related Objects

        [ForeignKey("SurveyTypeID")]
        public virtual SurveyType SurveyType { get; set; }

        #endregion

        #endregion

        #region Collections

        [ForeignKey("SurveyID")]
        public virtual ICollection<Question> Questions { get; set; }

        #endregion
    }
}
