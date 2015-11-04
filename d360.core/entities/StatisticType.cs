using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.StatisticType, "StatisticType")]
    public class StatisticType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "CheckType_Name", Description = "CheckType_Description")]
        public StatisticCheckType CheckType { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "PartOfScore_Name", Description = "PartOfScore_Description")]
        public bool PartOfScore { get; set; }

        [DataMember]
        public string Configuration { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Collection Properties

        [XmlIgnore()]
        [ForeignKey("StatisticTypeID")]
        public virtual ICollection<Statistic> Statistics { get; set; }

        [XmlIgnore()]
        [ForeignKey("StatisticTypeID")]
        public virtual ICollection<StatisticTypeRelation> Relations { get; set; }

        #endregion

    }
}
