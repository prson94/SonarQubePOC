using System;

namespace d360.core.entities
{
    public partial class AllowedIntersectionType
    {
        public int IntersectTypeID { get; set; }

        public string TargetType { get; set; }

        public int? TargetTypeID { get; set; }

        public string TargetName { get; set; }

        public int? ParentIntersectID { get; set; }

        public string PredicateName { get; set; }

        public Guid Uid { get; set; }
    }
}
