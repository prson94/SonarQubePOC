using System;

using d360.core.enums;

namespace d360.core.entities.Transitive
{
    public class PredicateIntersect
    {
        public int IntersectID { get; set; }
        
        public int IntersectTypeID { get; set; }
        
        public Guid IntersectTypeUid { get; set; }
        
        public string Subject { get; set; }
        
        public int SubjectID { get; set; }
        
        public string Object { get; set; }
        
        public int ObjectID { get; set; }
        
        public State State { get; set; }
        
        public int PredicateID { get; set; }
        
        public Guid PredicateUid { get; set; }
        
        public string PredicateName { get; set; }
        
        public string PredicateInverse { get; set; }
        
        public PredicateType PredicateType { get; set; }
    }
}
