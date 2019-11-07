using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Community.Templates
{
    [Table("Asset", Schema = "community")]
    public class Asset : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public Guid AssetTypeVersionUid { get; set; }
        
        [DataMember]
        public State State { get; set; }

        [DataMember]
        public string Object { get; set; }

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
