using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    public class UniqueAssetEffectiveDateModel
    {
        public Guid AllocationUid { get; set; }
        public int? AssetTypeId { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class UniqueAssetEffectiveDateModelComparer : IEqualityComparer<UniqueAssetEffectiveDateModel>
    {
        public bool Equals(UniqueAssetEffectiveDateModel x, UniqueAssetEffectiveDateModel y)
        {
            return (x.AllocationUid == y.AllocationUid && x.AssetTypeId == y.AssetTypeId && x.AssetUid == y.AssetUid && x.EffectiveDate == y.EffectiveDate);
        }

        public int GetHashCode(UniqueAssetEffectiveDateModel obj)
        {
            return base.GetHashCode();
        }
    }
}
