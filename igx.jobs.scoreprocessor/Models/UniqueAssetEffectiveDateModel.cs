using System;

namespace igx.jobs.scoreprocessor.Models
{
    public class UniqueAssetEffectiveDateModel
    {
        public Guid AllocationUid { get; set; }
        public int? AssetTypeId { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}
