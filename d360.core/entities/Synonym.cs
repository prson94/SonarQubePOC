using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.entities.Contracts;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Synonym, "Synonym")]
    public partial class Synonym : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SynonymType_Name", Description = "SynonymType_Description")]
        public int SynonymTypeID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual SynonymType SynonymType { get; set; }
    }
}
