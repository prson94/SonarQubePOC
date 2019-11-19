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

        [IgnoreDataMember]
        protected string AssetTypeVersions { get; set; }

        [DataMember, NotMapped]
        public List<Guid> AssetTypeVersions_Items
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Guid>>(string.IsNullOrEmpty(AssetTypeVersions) ? "[]" : AssetTypeVersions);
            }
            set
            {
                AssetTypeVersions = JsonConvert.SerializeObject(value);
            }
        }

        [IgnoreDataMember]
        protected string IntersectTypes { get; set; }

        [DataMember, NotMapped]
        public List<Guid> IntersectTypes_Items
        {
            get
            {
                return JsonConvert.DeserializeObject<List<Guid>>(string.IsNullOrEmpty(IntersectTypes) ? "[]" : IntersectTypes);
            }
            set
            {
                IntersectTypes = JsonConvert.SerializeObject(value);
            }
        }

        [DataMember]
        public AllocationState State { get; set; }
    }
}
