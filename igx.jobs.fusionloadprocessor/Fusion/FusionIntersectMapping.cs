using d360.core.enums;

namespace igx.jobs.fusionloadprocessor
{
    internal class FusionIntersectMapping
    {
        public int ID { get; set; }
        public int SubjectID { get; set; }
        public int ObjectID { get; set; }
        public PredicateType PredicateType { get; set; } = PredicateType.Simple;
    }
}