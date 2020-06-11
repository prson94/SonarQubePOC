using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Community.Templates
{
    [Table("AssetTypeVersion", Schema = "community")]
    public class AssetTypeVersion : BaseTemplateCreatedAndUpdatedGuidObject
    {
        public Guid AssetTypeUid { get; set; }
        public string Description { get; set; }
        [StringLength(250)]
        public string DisplayFormat { get; set; }
        [StringLength(7)] 
        public string BackColor { get; set; }
        [StringLength(7)] 
        public string ForeColor { get; set; }
        [StringLength(50)] 
        public string Icon { get; set; }
        public int HierarchyMaximumDepth { get; set; }
        public State State { get; set; }
        public bool CanOwnFusion { get; set; }
        public bool AutoDisplayDescription { get; set; }
        public bool UseAsTransformation { get; set; }

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

        [IgnoreDataMember]
        protected string Levels { get; set; }

        [DataMember, NotMapped]
        public List<AssetTypeVersionLevel> Level_Items
        {
            get
            {
                return JsonConvert.DeserializeObject<List<AssetTypeVersionLevel>>(string.IsNullOrEmpty(Levels) ? "[]" : Levels);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<AssetTypeVersionLevel>);
            }
        }
    }
}
