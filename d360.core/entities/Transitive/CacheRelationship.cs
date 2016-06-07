using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [Table("Relationship", Schema = "cache")]
    public partial class CacheRelationship
    {
        [Key, Column(Order = 1)]
        public int IntersectID { get; set; }


        public int SourceIntersectTypeNodeID { get; set; }

        public int SourceIntersectNodeID { get; set; }

        [Key, Column(Order = 2)]
        public string SourceObject { get; set; }

        [Key, Column(Order = 3)]
        public int SourceObjectID { get; set; }


        public int TargetIntersectTypeNodeID { get; set; }

        public int TargetIntersectNodeID { get; set; }

        public string TargetObject { get; set; }

        public int TargetObjectID { get; set; }
    }
}
