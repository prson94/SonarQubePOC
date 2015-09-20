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
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.QuestionType, "QuestionType")]
    public class QuestionType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(500)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        public int ResponseTypeID { get; set; }

        [DataMember]
        public int SurveyTypeID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }


        #region Related Objects

        [ScriptIgnore]
        [XmlIgnore()]
        public virtual ResponseType ResponseType { get; set; }

        [ScriptIgnore]
        [XmlIgnore()]
        public virtual SurveyType SurveyType { get; set; }

        #endregion

        #endregion

        #region Collections

        [ForeignKey("QuestionTypeID")]
        public virtual ICollection<Question> Questions { get; set; }

        #endregion
    }
}
