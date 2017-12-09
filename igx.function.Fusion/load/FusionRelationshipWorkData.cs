using System.Collections.Generic;

namespace igx.function.fusion.load
{
    internal class FusionRelationshipWorkData
    {
        public FusionRelationshipWorkData()
        {
            UnresolvedRelationshipData = new List<FusionRelationshipTableData>();
            ResolvedRelationshipData = new List<FusionRelationshipTableData>();
        }
        public List<FusionRelationshipTableData> UnresolvedRelationshipData { get; set; }
        public List<FusionRelationshipTableData> ResolvedRelationshipData { get; set; }
        public IEnumerable<FusionIntersectMapping> IntersectTypeMapping { get; set; }
    }
}