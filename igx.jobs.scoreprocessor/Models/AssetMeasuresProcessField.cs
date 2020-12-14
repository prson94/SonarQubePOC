using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AssetMeasuresProcessField
    {
        public Guid Assetuid { get; set; }
        public int FieldTypeID { get; set; }
        public string FieldTypeName { get; set; }
        public string Values { get; set; }
    }
}
