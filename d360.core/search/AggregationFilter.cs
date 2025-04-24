using d360.core.enums;
using System;

namespace d360.core.search
{
	public class AggregationFilter
    {
        public Guid? Uid { get; set; }
        
        public AssetTypeClass? Class { get; set; }
    }
}
