using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Community.Templates
{
    [Table("IntersectType", Schema = "community")]
    public class IntersectType : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public Guid PredicateUid { get; set; }

        [DataMember]
        public Guid SubjectVersionUid { get; set; }

        [DataMember]
        public Cardinality SubjectCardinality { get; set; }

        [DataMember]
        public Guid ObjectVersionUid { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }
        
        [DataMember]
        public State State { get; set; }

        [IgnoreDataMember]
        protected string Fields { get; set; }

        [DataMember, NotMapped]
        public List<FieldTypeApiEditModel> Field_Items
        {
            get
            {
                return JsonConvert.DeserializeObject<List<FieldTypeApiEditModel>>(string.IsNullOrEmpty(Fields) ? "[]" : Fields);
            }
            set
            {
                Fields = JsonConvert.SerializeObject(value);
            }
        }
    }
}
