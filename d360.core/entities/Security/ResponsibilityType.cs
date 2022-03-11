using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityType : BaseIntObject, IIntObject, IUpdatedMetadata, ICreatedMetadata
    {
        #region Properties

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public Guid UID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("ResponsibilityTypeID")]
        public virtual ICollection<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeViewModel : BaseObject
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }
    }

    public class ClaimsViewModel
    {

        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string Description { get; set; }

    }
}
