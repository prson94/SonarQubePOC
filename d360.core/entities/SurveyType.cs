using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SurveyType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public string ObjectType { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SurveyTypeObject_Name", Description = "SurveyTypeObject_Description")]
        public int ObjectID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #region Navigation Properties

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<QuestionType> QuestionTypes { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<Survey> Surveys { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<SurveyObjectCache> SurveyObjectCaches { get; set; }

        #endregion
    }
}
