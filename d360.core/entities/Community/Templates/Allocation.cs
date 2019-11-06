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
    public enum AllocationState
    {
        Active = 1,
        Inactive = 2
    }

    [Table("Allocation", Schema = "community")]
    public class Allocation : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public Guid PackageVersionUid { get; set; }

        [IgnoreDataMember, Column("AssetTypeVersions")]
        protected string AssetTypeVersionsJson { get; set; }

        [DataMember, NotMapped]
        public List<Guid> AssetTypeVersions
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Guid>>(string.IsNullOrEmpty(AssetTypeVersionsJson) ? "[]" : AssetTypeVersionsJson);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<Guid>);
            }
        }

        [IgnoreDataMember, Column("IntersectTypes")]
        protected string IntersectTypesJson { get; set; }

        [DataMember, NotMapped]
        public List<Guid> IntersectTypes
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Guid>>(string.IsNullOrEmpty(IntersectTypesJson) ? "[]" : IntersectTypesJson);
            }
            set
            {
                JsonConvert.SerializeObject(value as List<Guid>);
            }
        }

        [DataMember]
        public AllocationState State { get; set; }
    }
}
