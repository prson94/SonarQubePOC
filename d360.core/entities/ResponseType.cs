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
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.ResponseType, "ResponseType")]
    public class ResponseType : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AllowOptions_Name", Description = "AllowOptions_Description")]
        public bool AllowOptions { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AllowValueOverride_Name", Description = "AllowValueOverride_Description")]
        public bool AllowValueOverride { get; set; }

        #endregion

        #region Collections

        [ForeignKey("ResponseTypeID")]
        public virtual ICollection<QuestionType> QuestionTypes { get; set; }

        [ForeignKey("ResponseTypeID")]
        public virtual ICollection<ResponseTypeOption> ResponseTypeOptions { get; set; }

        #endregion
    }
}
