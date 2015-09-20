using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeRelation : BaseObject
    {
        [Column(Order = 1), DataMember, Key, ReadOnly(true)]
        public int FieldTypeID { get; set; }

        [Column(Order = 2), DataMember, Key, ReadOnly(true)]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public string ObjectType { get; set; }

        [Column(Order = 3), DataMember, Key, ReadOnly(true)]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int ObjectID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public bool IsListable { get; set; }

        [DataMember]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public bool IsRequired { get; set; }

        [DataMember]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int SortOrder { get; set; }

        [IgnoreDataMember]
        public FieldType FieldType { get; set; }
    }
}
