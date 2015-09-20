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
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Question, "Question")]
    public class Question : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int QuestionTypeID { get; set; }

        [DataMember]
        public int? ResponseTypeOptionID { get; set; }

        [DataMember]
        public string ResponseValue { get; set; }

        [DataMember]
        public int SurveyID { get; set; }

        [DataMember]
        public string Comment { get; set; }

        #region Related Objects

        public virtual QuestionType QuestionType { get; set; }

        public virtual ResponseTypeOption ResponseTypeOption { get; set; }

        public virtual Survey Survey { get; set; }

        #endregion

        #endregion
    }
}
