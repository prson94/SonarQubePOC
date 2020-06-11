using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class GetRelationshipApiModel
    {
        public Guid Uid { get; set; }
        public Guid RelationshipTypeUid { get; set; }
        public State State { get; set; }
        public GetRelationshipEdgeApiModel Subject { get; set; }
        public GetRelationshipEdgeApiModel Object { get; set; }
        public GetRelationshipPredicateApiModel Predicate { get; set; }
    }
    public class GetRelationshipsApiModel
    {
        public int pageNum { get; set; }
        public int pageSize { get; set; }
        public int total { get; set; }
        public List<GetRelationshipApiModel> items { get; set; }
    }
    public class GetRelationshipEdgeApiModel
    {
        public Guid Uid { get; set; }
        public Guid AssetTypeUid { get; set; }

    }
    public class GetRelationshipPredicateApiModel
    {
        public Guid Uid { get; set; }
        public PredicateType Type { get; set; }
        public string Name { get; set; }
        public string Inverse { get; set; }

    }
}
