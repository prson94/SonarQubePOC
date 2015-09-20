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
    public class ResponseTypeOption : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int ResponseTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Value_Name", Description = "Value_Description")]
        public int Value { get; set; }

        #region Related Objects

        [ScriptIgnore]
        [XmlIgnore()]
        public virtual ResponseType ResponseType { get; set; }

        #endregion

        #endregion

        #region Collections

        [ForeignKey("ResponseTypeOptionID")]
        public virtual ICollection<Question> Questions { get; set; }

        #endregion
    }
}
