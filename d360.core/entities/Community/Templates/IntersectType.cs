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
    [Table("IntersectType", Schema = "community")]
    public class IntersectType : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public Guid PredicateUid { get; set; }

        [DataMember]
        public Guid SubjectVersionUid { get; set; }

        [DataMember]
        public Guid ObjectVersionUid { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }
        
        [DataMember]
        public State State { get; set; }

        [IgnoreDataMember, Column("Fields")]
        protected string FieldsJson { get; set; }

        [DataMember, NotMapped]
        public List<FieldTypeApiViewModel> Fields
        {
            get
            {
                return JsonConvert.DeserializeObject<List<FieldTypeApiViewModel>>(string.IsNullOrEmpty(FieldsJson) ? "[]" : FieldsJson);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<FieldTypeApiViewModel>);
            }
        }
    }
}
