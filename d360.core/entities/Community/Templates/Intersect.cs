using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Community.Templates
{
    [Table("Intersect", Schema = "community")]
    public class Intersect : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public Guid IntersectTypeUid { get; set; }

        [DataMember]
        public Guid SubjectUid { get; set; }

        [DataMember]
        public Guid ObjectUid { get; set; }

        [DataMember]
        public State State { get; set; }

        [IgnoreDataMember]
        protected string Fields { get; set; }

        [DataMember, NotMapped]
        public List<Field> Field_Items
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Field>>(string.IsNullOrEmpty(Fields) ? "[]" : Fields);
            }
            set
            {
                Fields = JsonConvert.SerializeObject(value);
            }
        }
    }
}
