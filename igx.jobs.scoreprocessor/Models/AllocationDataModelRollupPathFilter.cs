using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModelRollupPathFilter
    {
        public Guid AssetVersionRollupPathFilterUid { get; set; }
        public int AssetTypeID { get; set; }
        public int FieldTypeID { get; set; }
        public string Operator { get; set; }
        public List<AllocationDataModelRollupPathFilterValue> Values { get; set; }
    }
}
