using System;
using System.Collections.Generic;

namespace d360.core.entities
{
    public class TagDetail
    {
        public string DisplayValue { get; set; }

        public string DisplayPath { get; set; }

        public int AssetID { get; set; }

        public Guid AssetUid { get; set; }

        public Guid AssetTypeUid { get; set; }

        public string AssetType { get; set; }

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public List<TagDetailItem> Tags { get; set; } = new List<TagDetailItem>();
    }
}
