using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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

        [IgnoreDataMember, Column("Fields")]
        protected string FieldsJson { get; set; }

        [DataMember, NotMapped]
        public List<Field> Fields
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Field>>(string.IsNullOrEmpty(FieldsJson) ? "[]" : FieldsJson);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<Field>);
            }
        }
    }
}
