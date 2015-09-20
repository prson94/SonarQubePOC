using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Survey : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
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
