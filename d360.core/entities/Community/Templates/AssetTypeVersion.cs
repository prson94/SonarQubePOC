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
    [Table("AssetTypeVersion", Schema = "community")]
    public class AssetTypeVersion : BaseTemplateCreatedAndUpdatedGuidObject
    {
        public Guid AssetTypeUid { get; set; }
        public string Description { get; set; }
        public string DisplayFormat { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string Icon { get; set; }
        public int HierarchyMaximumDepth { get; set; }
        public State State { get; set; }
        public bool CanOwnFusion { get; set; }
        public bool AutoDisplayDescription { get; set; }
        public bool UseAsTransformation { get; set; }

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

        [IgnoreDataMember, Column("Levels")]
        protected string LevelsJson { get; set; }

        [DataMember, NotMapped]
        public List<AssetTypeVersionLevel> Levels
        {
            get
            {
                return JsonConvert.DeserializeObject<List<AssetTypeVersionLevel>>(string.IsNullOrEmpty(LevelsJson) ? "[]" : LevelsJson);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<AssetTypeVersionLevel>);
            }
        }
    }
}
