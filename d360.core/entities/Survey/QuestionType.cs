using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class QuestionType : BaseIntObject, ICreatedObject, IIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember, StringLength(500)]
        public string Name { get; set; }

        [DataMember, StringLength(2000)]
        public string Description { get; set; }

        [DataMember]
        public QuestionDisplayStyle DisplayStyle { get; set; }

        [DataMember]
        public int SurveyTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }


        [ScriptIgnore, XmlIgnore()]
        public virtual SurveyType SurveyType { get; set; }


        [ForeignKey("QuestionTypeID")]
        public virtual ICollection<QuestionTypeOption> QuestionTypeOptions { get; set; }
    }
}
