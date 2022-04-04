using System;

using d360.core.entities;

namespace d360.model.DataAccessLayer.repositories
{
    public class ResponsibilityBreakdownByResourceAggregate
    {
        public Guid AssetTypeUid { get; set; }

        public Guid ResponsibilityTypeUid { get; set; }

        public int AssetCount { get; set; }

        // nested entities 

        public AssetType AssetType { get; set; }

        public ResponsibilityType ResponsibilityType { get; set; }
    }
}
